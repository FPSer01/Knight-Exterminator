using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using static PamukAI.PAI;
using Random = UnityEngine.Random;

public class CentipedeBossBehaviour : EnemyBehaviour
{
    [Header("General")]
    [SerializeField] private float seeDistance;
    [SerializeField] private float turnSpeedToTarget;
    [SerializeField] private float meleeDistance;
    [SerializeField] private float meleeAngle;
    [SerializeField] private float overallCooldown;
    private float currentOverallCooldown;

    private float originalMoveSpeed;
    private float originalAcceleration;
    private float originalAngularSpeed;

    [Header("Bite")]
    [SerializeField] private float biteTime;
    [SerializeField] private float biteExecuteDelay;

    [Header("Many Strikes")]
    [SerializeField] private float manyStrikesTime;
    [SerializeField] private float manyStrikesExecuteDelay;
    [SerializeField] private float manyStrikesDuration;
    private float currentMeleeCooldown;

    [Header("Whip")]
    [SerializeField] private EnemyTriggerZone rightWhipTrigger;
    [SerializeField] private EnemyTriggerZone leftWhipTrigger;
    [Space]
    [SerializeField] private float whipTime;
    [SerializeField] private float whipExecuteDelay;
    private float currentWhipCooldown;

    [Header("Circle Attack")]
    [SerializeField] private float circleAttackDistance;
    [SerializeField] private float circleRadius;
    [SerializeField] private float circleMoveSpeed;
    [SerializeField] private float circleAcceleration;
    [SerializeField] private float circleTime;
    private Vector3 currentCirclePoint;
    [Space]
    [Range(0f, 1f)] [SerializeField] private float circleAttackChance;
    [SerializeField] private float circlePrecision;
    [SerializeField] private float pointSwitchThreshold;

    [Header("Dig In Hole")]
    [SerializeField] private float hideMoveSpeed;
    [SerializeField] private float hideAcceleration;
    [SerializeField] private LayerMask holeMask;
    [SerializeField] private float distanceToBeginDig;
    [SerializeField] private float digCooldown;
    private float currentDigCooldown;

    [Header("Dig Out Of Hole")]
    [SerializeField] private float sitInHoleTime;
    [SerializeField] private float prepareOutOfHoleTime;
    [SerializeField] private float outOfHoleAnimation;
    [SerializeField] private float outOfHoleTime;

    private CentipedeHole foundHole;

    [Header("Model Positions")]
    [SerializeField] private Transform leftTurnPosition;
    [SerializeField] private Transform rightTurnPosition;
    [SerializeField] private float switchModelPosDuration;

    private Vector3 originalModelLocalPos;
    private float currentCircleCooldown;
    private float currentAngle;
    private bool circleRight = true;

    [Header("Phase Control")]
    [SerializeField] private List<CentipedeBossPhase> phaseSettings;

    [Header("Components")]
    [SerializeField] private Transform bossModel;
    [SerializeField] private EnemySFXController sfxController;
    [SerializeField] private BossHealth enemyHealth;
    [SerializeField] private CentipedeBossAttack enemyAttack;
    [SerializeField] private Animator animator;
    [SerializeField] private SplineAnimate splineAnimate;
    [SerializeField] private CentipedeColliderController colliderController;

    private int currentPhaseIndex;
    private Method state;

    protected override void Awake()
    {
        base.Awake();

        enemyHealth.OnPhaseChange += OnPhaseChange;
    }

    private void OnPhaseChange(BossPhaseSettings settings)
    {
        currentPhaseIndex = settings.PhaseIndex;
        var phase = phaseSettings.Find(p => p.Index == currentPhaseIndex);
        SetPhaseStats(phase);       

        Debug.Log($"<color=#FF0000>[Phase] Change to Index {currentPhaseIndex}, Phase {currentPhaseIndex + 1}</color>");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SetPhaseStats(phaseSettings.Find(s => s.Index == 0));

        rightWhipTrigger.OnPlayerTriggerEnter += RightWhipTrigger_OnPlayerTriggerEnter;
        leftWhipTrigger.OnPlayerTriggerEnter += LeftWhipTrigger_OnPlayerTriggerEnter;

        SetDigCooldown();

        SwitchState(Idle, ref state);
        Debug.Log($"<color=#FF0000>[Phase] Change to Index {currentPhaseIndex}, Phase {currentPhaseIndex + 1}</color>");

        originalModelLocalPos = bossModel.localPosition;
    }

    #region Triggers

    private void LeftWhipTrigger_OnPlayerTriggerEnter()
    {
        if (IsWhipAttack())
            SwitchState(WhipLeft, ref state);
    }

    private void RightWhipTrigger_OnPlayerTriggerEnter()
    {
        if (IsWhipAttack())
            SwitchState(WhipRight, ref state);
    }

    #endregion

    protected override void Update()
    {
        base.Update();
        Tick(state);
    }

    #region States

    private bool Idle()
    {
        if (DoOnce())
        {
            Debug.Log("Centipede Idle State");
            animator.SetBool("Move", false);
        }

        if (IsNear(transform, mainTarget, seeDistance))
        {
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool DecideState()
    {
        switch (currentPhaseIndex)
        {
            case 0:
                SwitchState(Move_Phase1, ref state);
                break;
            case 1:
                SwitchState(Move_Phase2, ref state);
                break;
            case 2:
                SwitchState(Move_Phase3, ref state);
                break;
            default:
                Debug.LogWarning("Boss default Phase State was triggered");
                SwitchState(Move_Phase1, ref state);
                break;
        }

        return true;
    }

    private bool Move_Phase1()
    {
        if (DoOnce())
        {
            Debug.Log("[Phase 1] Centipede Move State");
            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(true);
            rightWhipTrigger.SetCollider(true);
        }

        CheckCooldowns();

        if (currentDigCooldown <= 0)
            SwitchState(DigInHole, ref state);

        // Проверка на ближний бой (2 исхода: 50 на 50)
        if (CheckForStrike(meleeDistance, meleeAngle) && IsMeleeAttack())
        {
            int meleeIndex = Random.Range(0, 2);
            if (meleeIndex == 0)
                SwitchState(Bite, ref state);
            else
                SwitchState(ManyStrikes, ref state);
        }

        if (IsNear(transform, mainTarget, meleeDistance))
            PredictMoveTowardsTarget();
        else
            MoveTowardsTarget();

        return true;
    }

    private bool Move_Phase2()
    {
        if (DoOnce())
        {
            Debug.Log("[Phase 2] Centipede Move State");
            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(true);
            rightWhipTrigger.SetCollider(true);
        }

        CheckCooldowns();

        if (currentDigCooldown <= 0)
            SwitchState(DigInHole, ref state);

        if (IsCircleAttack())
        {
            int decideDirectionRight = Random.Range(0, 2);
            circleRight = decideDirectionRight == 1;

            SwitchState(CircleAttack, ref state);
        }

        // Проверка на ближний бой (2 исхода: 50 на 50)
        if (CheckForStrike(meleeDistance, meleeAngle) && IsMeleeAttack())
        {
            int meleeIndex = Random.Range(0, 2);
            if (meleeIndex == 0)
                SwitchState(Bite, ref state);
            else
                SwitchState(ManyStrikes, ref state);
        }

        if (IsNear(transform, mainTarget, meleeDistance))
            PredictMoveTowardsTarget();
        else
            MoveTowardsTarget();

        return true;
    }

    private bool Move_Phase3()
    {
        if (DoOnce())
        {
            Debug.Log("[Phase 3] Centipede Move State");
            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(true);
            rightWhipTrigger.SetCollider(true);
            enemyAttack.ActivatePoisonClouds(true);
        }

        CheckCooldowns();

        if (currentDigCooldown <= 0)
            SwitchState(DigInHole, ref state);

        if (IsCircleAttack())
        {
            int decideDirectionRight = Random.Range(0, 2);
            circleRight = decideDirectionRight == 1;

            SwitchState(CircleAttack, ref state);
        }

        // Проверка на ближний бой (2 исхода: 50 на 50)
        if (CheckForStrike(meleeDistance, meleeAngle) && IsMeleeAttack())
        {
            int meleeIndex = Random.Range(0, 2);
            if (meleeIndex == 0)
                SwitchState(Bite, ref state);
            else
                SwitchState(ManyStrikes, ref state);
        }

        if (IsNear(transform, mainTarget, meleeDistance))
            PredictMoveTowardsTarget();
        else
            MoveTowardsTarget();

        return true;
    }

    private bool Bite()
    {
        if (DoOnce())
        {
            Debug.Log("Centipede Bite State");

            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Bite");

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.enabled = false;
        }

        if (Wait(biteExecuteDelay))
        {
            RotateTowardsTarget(turnSpeedToTarget);
            return true;
        }

        if (DoOnce())
        {
            enemyAttack.Bite();
        }

        if (Wait(biteTime - biteExecuteDelay))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;

            SetMeleeCooldown();
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool ManyStrikes()
    {
        if (DoOnce())
        {
            Debug.Log("Centipede Many Strikes State");

            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Many Strikes");

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.enabled = false;
        }

        if (Wait(manyStrikesExecuteDelay))
        {
            return true;
        }

        if (DoOnce())
        {
            enemyAttack.ManyStrikes(manyStrikesDuration);
        }

        if (Wait(manyStrikesTime - manyStrikesExecuteDelay))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;
            SetMeleeCooldown();
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool WhipLeft()
    {
        if (DoOnce())
        {
            Debug.Log("Centipede Whip State");

            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Whip");
            animator.SetFloat("Whip Right", 0);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.enabled = false;
        }

        if (Wait(whipExecuteDelay))
        {
            return true;
        }

        if (DoOnce())
        {
            enemyAttack.WhipAttack(false);
        }

        if (Wait(whipTime - whipExecuteDelay))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;
            SetWhipCooldown();

            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool WhipRight()
    {
        if (DoOnce())
        {
            Debug.Log("Centipede Whip State");

            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Whip");
            animator.SetFloat("Whip Right", 1);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.enabled = false;
        }

        if (Wait(whipExecuteDelay))
        {
            return true;
        }

        if (DoOnce())
        {
            enemyAttack.WhipAttack(true);
        }

        if (Wait(whipTime - whipExecuteDelay))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;
            SetWhipCooldown();

            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool CircleAttack()
    {
        if (DoOnce())
        {
            Debug.Log("Centipede Circle State");

            animator.SetBool("Move", false);
            animator.SetBool("Circle Attack", true);
            animator.SetFloat("Circle Right", circleRight ? 1 : 0);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.speed = circleMoveSpeed;
            agent.acceleration = circleAcceleration;
            agent.angularSpeed = 999999f;
            agent.autoBraking = false;

            bossModel.DOLocalMove(circleRight ? rightTurnPosition.localPosition : leftTurnPosition.localPosition, switchModelPosDuration);

            enemyAttack.SetSpawnSlime(true);

            currentCirclePoint = mainTarget.position;
            currentAngle = GetInitialAngle(currentCirclePoint);
            agent.SetDestination(GetCurrentCirclePoint(circleRight));
        }

        if (Wait(circleTime))
        {
            if (agent.remainingDistance <= pointSwitchThreshold)
            {
                Vector3 circlePosition = GetCurrentCirclePoint(circleRight);
                agent.SetDestination(circlePosition);
            }

            return true;
        }

        if (Step())
        {
            animator.SetBool("Circle Attack", false);
            enemyAttack.SetSpawnSlime(false);

            agent.autoBraking = true;
            agent.speed = originalMoveSpeed;
            agent.acceleration = originalAcceleration;
            agent.angularSpeed = originalAngularSpeed;

            bossModel.DOLocalMove(originalModelLocalPos, switchModelPosDuration);

            SetCircleAttackCooldown();
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool DigInHole()
    {
        if (DoOnce())
        {
            Debug.Log("Centipede Dig In Hole State");

            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);
            enemyAttack.ActivatePoisonClouds(false);

            foundHole = FindClosestHoleFromTransform(transform);

            if (foundHole == null)
                SwitchState(DecideState, ref state);

            agent.speed = hideMoveSpeed;
            agent.acceleration = hideAcceleration;

            agent.SetDestination(foundHole.transform.position);
        }

        if (agent.remainingDistance <= distanceToBeginDig)
            SwitchState(DigOutOfHole, ref state);

        return true;
    }

    private bool DigOutOfHole()
    {
        if (DoOnce())
        {
            Debug.Log("Centipede Dig Out Of Hole State");

            agent.enabled = false;

            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            colliderController.SetColliders(false);

            if (foundHole != null)
                foundHole.PlaySFX();

            transform.DOMove(transform.position + new Vector3(0, -5f, 0), 1f);

            agent.speed = originalMoveSpeed;
            agent.acceleration = originalAcceleration;
        }

        if (Wait(sitInHoleTime))
            return true;

        if (DoOnce())
        {
            if (foundHole != null)
                foundHole.StopSFX();

            foundHole = FindClosestHoleFromTransform(mainTarget);
            foundHole.PlayBeforeJumpOutVFX(true);
        }

        if (Wait(prepareOutOfHoleTime))
            return true;

        if (DoOnce())
        {
            if (foundHole != null)
                foundHole.PlaySFX();

            foundHole.TurnSplineTowardsTransform(mainTarget);
            foundHole.PlayBeforeJumpOutVFX(false);
            foundHole.DoExplosion();

            animator.SetTrigger("Out Of Hole");
            splineAnimate.Container = foundHole.GetSpline();
            splineAnimate.Duration = outOfHoleAnimation;
            splineAnimate.Restart(true);
        }

        if (Wait(outOfHoleTime))
            return true;

        if (Step())
        {
            if (foundHole != null)
                foundHole.StopSFX();

            agent.enabled = true;
            colliderController.SetColliders(true);
            SetDigCooldown();

            SwitchState(DecideState, ref state);
        }

        return true;
    }

    #endregion

    #region States Support Methods

    private CentipedeHole FindClosestHoleFromTransform(Transform startTransform)
    {
        Collider[] colliders = new Collider[16];

        if (Physics.OverlapSphereNonAlloc(startTransform.position, seeDistance, colliders, holeMask, QueryTriggerInteraction.Collide) > 0)
        {
            CentipedeHole result = null;
            float minDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col == null)
                    continue;

                if (!col.TryGetComponent(out CentipedeHole hole))
                    continue;

                float distance = Vector3.Distance(startTransform.position, col.transform.position);

                if (minDistance > distance)
                {
                    minDistance = distance;
                    result = hole;
                }
            }

            Debug.Log(result.name);
            return result;
        }

        return null;
    }

    private float GetInitialAngle(Vector3 centerPoint)
    {
        Vector3 direction = (transform.position - centerPoint).normalized;
        return Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
    }

    private Vector3 GetCurrentCirclePoint(bool right)
    {
        Vector3 result;

        if (right)
            currentAngle -= circlePrecision * Time.deltaTime;
        else
            currentAngle += circlePrecision * Time.deltaTime;

        // Вычисляем позицию на окружности
        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        result = currentCirclePoint + new Vector3(
            Mathf.Cos(angleInRadians) * circleRadius,
            currentCirclePoint.y,
            Mathf.Sin(angleInRadians) * circleRadius
        );

        return result;
    }

    private bool CheckForStrike(float strikeDistance, float strikeAngle)
    {
        Vector3 targetPlanePosition = new(mainTarget.position.x, transform.position.y, mainTarget.position.z);
        float distance = Vector3.Distance(targetPlanePosition, transform.position);
        float angle = Vector3.Angle(transform.forward, targetPlanePosition - transform.position);

        if (distance <= strikeDistance && Mathf.Abs(angle) <= strikeAngle)
        {
            return true;
        }
        return false;
    }

    #endregion

    #region Attack Checks

    private bool IsCircleAttack()
    {
        float randomValue = Random.value;

        bool chance = randomValue >= 0 && randomValue <= circleAttackChance;
        bool cooldowns = currentCircleCooldown <= 0 && currentOverallCooldown <= 0;

        return chance && cooldowns && IsNear(transform, mainTarget, circleAttackDistance);
    }

    private bool IsMeleeAttack()
    {
        return currentMeleeCooldown <= 0 && currentOverallCooldown <= 0;
    }

    private bool IsWhipAttack()
    {
        return currentWhipCooldown <= 0f && currentOverallCooldown <= 0;
    }

    #endregion

    #region Cooldowns

    private void CheckCooldowns()
    {
        if (currentMeleeCooldown > 0)
            currentMeleeCooldown -= Time.deltaTime;

        if (currentWhipCooldown > 0)
            currentWhipCooldown -= Time.deltaTime;

        if (currentCircleCooldown > 0)
            currentCircleCooldown -= Time.deltaTime;

        if (currentOverallCooldown > 0)
            currentOverallCooldown -= Time.deltaTime;

        if (currentDigCooldown > 0)
            currentDigCooldown -= Time.deltaTime;
    }

    private void SetDigCooldown()
    {
        currentDigCooldown = digCooldown;
    }

    private void SetOverallCooldown()
    {
        currentOverallCooldown = overallCooldown;
    }

    private void SetMeleeCooldown()
    {
        SetOverallCooldown();
        currentMeleeCooldown = enemyAttack.MeleeCooldown;
    }

    private void SetWhipCooldown()
    {
        SetOverallCooldown();
        currentWhipCooldown = enemyAttack.WhipCooldown;
    }

    private void SetCircleAttackCooldown()
    {
        SetOverallCooldown();
        currentCircleCooldown = enemyAttack.CircleAttackCooldown;
    }

    #endregion

    private void SetPhaseStats(CentipedeBossPhase settings)
    {
        originalMoveSpeed = settings.MoveSpeed;
        originalAcceleration = settings.Acceleration;
        originalAngularSpeed = settings.AngularSpeed;

        agent.speed = originalMoveSpeed;
        agent.acceleration = originalAcceleration;
        agent.angularSpeed = originalAngularSpeed;

        enemyAttack.MeleeCooldown = settings.MeleeCooldown;
        enemyAttack.WhipCooldown = settings.WhipCooldown;

        circleAttackDistance = settings.CircleAttackDistance;
        circleRadius = settings.CircleRadius;
        circleMoveSpeed = settings.CircleMoveSpeed;
        circleAcceleration = settings.CircleAcceleration;
        circleTime = settings.CircleTime;
        circleAttackChance = settings.CircleAttackChance;
        enemyAttack.CircleAttackCooldown = settings.CircleAttackCooldown;

        hideMoveSpeed = settings.HoleMoveSpeed;
        hideAcceleration = settings.HoleAcceleration;
        digCooldown = settings.DigCooldown;
        sitInHoleTime = settings.SitInHoleTime;
        prepareOutOfHoleTime = settings.PrepareOutOfHoleTime;
        outOfHoleTime = settings.OutOfHoleTime;
    }

    private void OnDrawGizmosSelected()
    {
        // See
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, seeDistance);

        // Melee Attack
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeDistance);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, meleeAngle / 2, 0) * transform.forward * meleeDistance + transform.position);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, -meleeAngle / 2, 0) * transform.forward * meleeDistance + transform.position);

        // Circle Radius
        Gizmos.color = Color.magenta;
        if (mainTarget == null)
        {
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(mainTarget.position, circleRadius);
        }
    }

    [Serializable]
    public class CentipedeBossPhase
    {
        [Header("Index")]
        public int Index;

        [Header("General")]
        public float MoveSpeed;
        public float Acceleration;
        public float AngularSpeed;

        [Header("Bite and Many Strikes")]
        public float MeleeCooldown;

        [Header("Whip")]
        public float WhipCooldown;

        [Header("Circle Attack")]
        public float CircleAttackDistance;
        public float CircleRadius;
        public float CircleMoveSpeed;
        public float CircleAcceleration;
        public float CircleTime;
        [Range(0f, 1f)] public float CircleAttackChance;
        public float CircleAttackCooldown;

        [Header("Dig")]
        public float HoleMoveSpeed;
        public float HoleAcceleration;
        public float DigCooldown;
        [Space]
        public float SitInHoleTime;
        public float PrepareOutOfHoleTime;
        public float OutOfHoleTime;
    }
}
