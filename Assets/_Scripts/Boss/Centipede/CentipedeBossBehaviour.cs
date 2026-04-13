using DG.Tweening;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Splines;
using static PamukAI.PAI;
using Random = UnityEngine.Random;

public class CentipedeBossBehaviour : BaseEnemyBehaviour
{
    [Serializable]
    public struct CentipedeBossPhase
    {
        [Header("Index")]
        public int Index;

        [Header("Agent Movement")]
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

    private readonly static string DEBUG = $"[{LogTags.BLUE_COLOR}Centipede Behaviour{LogTags.END_COLOR}]";

    [Header("Centipede Boss Behaviour: General")]
    [SerializeField] private float rotateSpeed;
    [SerializeField] private float overallAttackCooldown;
    private float currentOverallCooldown;

    [Header("Centipede Boss Behaviour: Melee General")]
    [SerializeField] private float meleeDistance;
    [SerializeField] private float meleeAngle;

    [Header("Centipede Boss Behaviour: Bite")]
    [SerializeField] private EnemyMeleeAttack biteAttack;
    [SerializeField] private float delayBeforeBiteAttack;
    [SerializeField] private float delayAfterBiteAttack;

    [Header("Centipede Boss Behaviour: Many Strikes")]
    [SerializeField] private EnemyMeleeAttack manyStrikesAttack;
    [SerializeField] private float delayBeforeManyStrikesAttack;
    [SerializeField] private float delayAfterManyStrikesAttack;

    [Header("Centipede Boss Behaviour: Whip")]
    [SerializeField] private EnemyMeleeAttack rightWhipAttack;
    [SerializeField] private EnemyMeleeAttack leftWhipAttack;
    [Space]
    [SerializeField] private EnemyTriggerZone rightWhipTrigger;
    [SerializeField] private EnemyTriggerZone leftWhipTrigger;
    [Space]
    [SerializeField] private float delayBeforeWhipAttack;
    [SerializeField] private float delayAfterWhipAttack;

    [Header("Centipede Boss Behaviour: Circle Attack")]
    [SerializeField] private EnemyObjectSpawner slimeSpawner;
    [SerializeField] private float circleAttackDistance;
    [SerializeField] private float circleAttackCooldown;
    [SerializeField] private float circleRadius;
    [SerializeField] private float circleMoveSpeed;
    [SerializeField] private float circleAcceleration;
    [SerializeField] private float circleTime;
    private float currentCircleCooldown;
    private Vector3 currentCirclePoint;
    [Space]
    [Range(0f, 1f)][SerializeField] private float circleAttackChance;
    [SerializeField] private float circlePrecision;
    [SerializeField] private float pointSwitchThreshold;
    private float currentAngle;
    private bool circleRight = true;

    [Header("Centipede Boss Behaviour: Dig In Hole")]
    [SerializeField] private float hideMoveSpeed;
    [SerializeField] private float hideAcceleration;
    [SerializeField] private LayerMask holeMask;
    [SerializeField] private float distanceToBeginDig;
    [SerializeField] private float digCooldown;
    private float currentDigCooldown;

    [Header("Centipede Boss Behaviour: Dig Out Of Hole")]
    [SerializeField] private SplineAnimate digOutSplineAnimate;
    [SerializeField] private float sitInHoleTime;
    [SerializeField] private float prepareOutOfHoleTime;
    [SerializeField] private float outOfHoleAnimation;
    [SerializeField] private float outOfHoleTime;
    private CentipedeHole foundHole;

    [Header("Centipede Boss Behaviour: Poison Clouds")]
    [SerializeField] private EnemyObjectSpawner poisonCloudSpawner;

    [Header("Centipede Boss Behaviour: Model Positions")]
    [SerializeField] private Transform leftTurnPosition;
    [SerializeField] private Transform rightTurnPosition;
    [SerializeField] private float switchModelPosDuration;

    private Vector3 originalModelLocalPos;

    [Header("Phase Control")]
    [SerializeField] private List<CentipedeBossPhase> phaseSettings;

    [Header("Components")]
    [SerializeField] private CentipedeColliderController colliderController;
    [SerializeField] private Transform bossModel;
    [SerializeField] private EnemyBossComponents components;

    private EnemySFXController sfxController => components.SFXController;
    private BossHealth enemyHealth => components.Health as BossHealth;
    private Animator animator => components.Animator;

    private int currentPhaseIndex;

    #region Animation Keys

    private const string ANIMATION_MOVE = "Move";

    private const string ANIMATION_BITE = "Bite";
    private const string ANIMATION_MANY_STRIKES = "Many Strikes";

    private const string ANIMATION_WHIP = "Whip";
    private const string ANIMATION_WHIP_DIRECTION = "Whip Right";

    private const string ANIMATION_CIRCLE = "Circle Attack";
    private const string ANIMATION_CIRCLE_DIRECTION = "Circle Right";

    private const string ANIMATION_OUT_OF_HOLE = "Out Of Hole";

    #endregion

    private void OnPhaseChange(BossPhaseSettings settings)
    {
        currentPhaseIndex = settings.PhaseIndex;
        var phase = phaseSettings.Find(p => p.Index == currentPhaseIndex);
        SetPhaseStats(phase);

        Debug.Log($"{DEBUG} {LogTags.RED_COLOR}[Phase] Change to Index {currentPhaseIndex}, Phase {currentPhaseIndex + 1}{LogTags.END_COLOR}");
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        originalModelLocalPos = bossModel.localPosition;

        SetDigCooldown();

        enemyHealth.OnPhaseChange += OnPhaseChange;
        rightWhipTrigger.OnPlayerTriggerEnter += RightWhipTrigger_OnPlayerTriggerEnter;
        leftWhipTrigger.OnPlayerTriggerEnter += LeftWhipTrigger_OnPlayerTriggerEnter;

        SwitchState(Idle, ref state);
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

    #region States

    private bool Idle()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, false);
            Debug.Log($"{DEBUG} Idle State");
        }

        if (IsNearCurrentTarget(seeDistance))
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
                Debug.LogWarning($"{DEBUG} Boss default Phase State was triggered");
                SwitchState(Move_Phase1, ref state);
                break;
        }

        return true;
    }

    private void Move()
    {
        if (IsNearCurrentTarget(meleeDistance))
        {
            PredictMoveTowardsTarget(2);
        }
        else
        {
            MoveTowardsTarget();
        }
    }

    private bool Move_Phase1()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, true);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(true);
            rightWhipTrigger.SetCollider(true);

            Debug.Log($"{DEBUG} [Phase 1] Move State");
        }

        CheckCooldowns();

        // Закопаться в землю
        if (currentDigCooldown <= 0)
        {
            SwitchState(DigInHole, ref state);
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

        Move();

        return true;
    }

    private bool Move_Phase2()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, true);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(true);
            rightWhipTrigger.SetCollider(true);

            Debug.Log($"{DEBUG} [Phase 2] Move State");
        }

        CheckCooldowns();

        // Закопаться в землю
        if (currentDigCooldown <= 0)
        {
            SwitchState(DigInHole, ref state);
        }

        // Атака по кругу
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

        Move();

        return true;
    }

    private bool Move_Phase3()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, true);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(true);
            rightWhipTrigger.SetCollider(true);

            // Спавн газовых облаков
            poisonCloudSpawner.StartSpawnWithTimeframe(true);

            Debug.Log($"{DEBUG} [Phase 3] Move State");
        }

        CheckCooldowns();

        // Закопаться в землю
        if (currentDigCooldown <= 0)
        {
            SwitchState(DigInHole, ref state);
        }

        // Атака по кругу
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

        Move();

        return true;
    }

    private bool Bite()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, false);
            animator.SetTrigger(ANIMATION_BITE);
            sfxController.PlayMoveSFX(false);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.enabled = false;

            Debug.Log($"{DEBUG} Bite State");
        }

        if (Wait(delayBeforeBiteAttack))
        {
            RotateTowardsTarget(rotateSpeed);
            return true;
        }

        if (DoOnce())
        {
            biteAttack.Attack();
        }

        if (Wait(biteAttack.TotalAttackTime + delayAfterBiteAttack))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;

            SetOverallCooldown();
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool ManyStrikes()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, false);
            animator.SetTrigger(ANIMATION_MANY_STRIKES);
            sfxController.PlayMoveSFX(false);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.enabled = false;

            Debug.Log($"{DEBUG} Many Strikes State");
        }

        if (Wait(delayBeforeManyStrikesAttack))
        {
            return true;
        }

        if (DoOnce())
        {
            manyStrikesAttack.Attack();
        }

        if (Wait(manyStrikesAttack.TotalAttackTime + delayAfterManyStrikesAttack))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;

            SetOverallCooldown();
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool WhipLeft()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, false);
            animator.SetFloat(ANIMATION_WHIP_DIRECTION, 0);
            animator.SetTrigger(ANIMATION_WHIP);
            sfxController.PlayMoveSFX(false);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.enabled = false;

            Debug.Log($"{DEBUG} Left Whip State");
        }

        if (Wait(delayBeforeWhipAttack))
        {
            return true;
        }

        if (DoOnce())
        {
            leftWhipAttack.Attack();
        }

        if (Wait(leftWhipAttack.TotalAttackTime + delayAfterWhipAttack))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;

            SetOverallCooldown();
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool WhipRight()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, false);
            animator.SetFloat(ANIMATION_WHIP_DIRECTION, 1);
            animator.SetTrigger(ANIMATION_WHIP);
            sfxController.PlayMoveSFX(false);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.enabled = false;

            Debug.Log($"{DEBUG} Right Whip State");
        }

        if (Wait(delayBeforeWhipAttack))
        {
            return true;
        }

        if (DoOnce())
        {
            rightWhipAttack.Attack();
        }

        if (Wait(rightWhipAttack.TotalAttackTime + delayAfterWhipAttack))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;

            SetOverallCooldown();
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool CircleAttack()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, false);
            animator.SetBool(ANIMATION_CIRCLE, true);
            animator.SetFloat(ANIMATION_CIRCLE_DIRECTION, circleRight ? 1 : 0);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            agent.speed = circleMoveSpeed;
            agent.acceleration = circleAcceleration;
            agent.angularSpeed = 999999f;
            agent.autoBraking = false;

            AnimateBossModelPosition_EveryoneRpc(
                circleRight ? rightTurnPosition.localPosition : leftTurnPosition.localPosition,
                switchModelPosDuration);

            slimeSpawner.StartSpawnWithDistance(true);

            currentCirclePoint = currentTarget.Position;
            currentAngle = GetInitialAngle(currentCirclePoint);
            agent.SetDestination(GetCurrentCirclePoint(circleRight));

            Debug.Log($"{DEBUG} Circle Attack State");
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
            animator.SetBool(ANIMATION_CIRCLE, false);

            slimeSpawner.StartSpawnWithDistance(false);

            agent.autoBraking = true;
            agent.speed = originalMoveSpeed;
            agent.acceleration = originalMoveAcceleration;
            agent.angularSpeed = originalTurnSpeed;

            AnimateBossModelPosition_EveryoneRpc(originalModelLocalPos, switchModelPosDuration);

            SetCircleAttackCooldown();
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool DigInHole()
    {
        if (DoOnce())
        {
            animator.SetBool(ANIMATION_MOVE, true);
            sfxController.PlayMoveSFX(true);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            poisonCloudSpawner.StartSpawnWithTimeframe(false);

            foundHole = FindClosestHoleFromPosition(currentTarget.Position);

            if (foundHole == null)
            {
                SwitchState(DecideState, ref state);
            }

            agent.speed = hideMoveSpeed;
            agent.acceleration = hideAcceleration;

            agent.SetDestination(foundHole.transform.position);

            Debug.Log($"{DEBUG} Dig In Hole State");
        }

        if (agent.remainingDistance <= distanceToBeginDig)
        {
            SwitchState(DigOutOfHole, ref state);
        }

        return true;
    }

    private bool DigOutOfHole()
    {
        if (DoOnce())
        {
            agent.enabled = false;

            animator.SetBool(ANIMATION_MOVE, false);
            sfxController.PlayMoveSFX(false);

            leftWhipTrigger.SetCollider(false);
            rightWhipTrigger.SetCollider(false);

            colliderController.SetColliders_EveryoneRpc(false);

            if (foundHole != null)
            {
                foundHole.PlaySFX_EveryoneRpc();
            }

            transform.DOMove(transform.position + new Vector3(0, -5f, 0), 1f);

            agent.speed = originalMoveSpeed;
            agent.acceleration = originalMoveAcceleration;

            Debug.Log($"{DEBUG} Dig Out Of Hole State");
        }

        if (Wait(sitInHoleTime))
        {
            return true;
        }

        if (DoOnce())
        {
            if (foundHole != null)
            {
                foundHole.StopSFX_EveryoneRpc();
            }

            foundHole = FindClosestHoleFromPosition(currentTarget.Position);
            foundHole.PlayBeforeJumpOutVFX_EveryoneRpc(true);
        }

        if (Wait(prepareOutOfHoleTime))
            return true;

        if (DoOnce())
        {
            if (foundHole != null)
            {
                foundHole.PlaySFX_EveryoneRpc();
            }

            foundHole.TurnSplineTowardsPoint(currentTarget.Position);
            foundHole.PlayBeforeJumpOutVFX_EveryoneRpc(false);
            foundHole.DoExplosion();

            animator.SetTrigger(ANIMATION_OUT_OF_HOLE);
            digOutSplineAnimate.Container = foundHole.GetSpline();
            digOutSplineAnimate.Duration = outOfHoleAnimation;
            digOutSplineAnimate.Restart(true);
        }

        if (Wait(outOfHoleTime))
        {
            return true;
        }

        if (Step())
        {
            if (foundHole != null)
            {
                foundHole.StopSFX_EveryoneRpc();
            }

            agent.enabled = true;
            colliderController.SetColliders_EveryoneRpc(true);
            SetDigCooldown();

            SwitchState(DecideState, ref state);
        }

        return true;
    }

    #endregion

    #region States Support Methods

    [Rpc(SendTo.Everyone)]
    private void AnimateBossModelPosition_EveryoneRpc(Vector3 position, float duration)
    {
        bossModel.DOLocalMove(position, duration);
    }

    private CentipedeHole FindClosestHoleFromPosition(Vector3 position)
    {
        Collider[] colliders = new Collider[16];

        if (Physics.OverlapSphereNonAlloc(position, seeDistance, colliders, holeMask, QueryTriggerInteraction.Collide) > 0)
        {
            CentipedeHole result = null;
            float minDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col == null)
                    continue;

                if (!col.TryGetComponent(out CentipedeHole hole))
                    continue;

                float distance = Vector3.Distance(position, col.transform.position);

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
        var angle = GetHorizontalAngleToTarget();
        var distance = GetHorizontalDistanceToTarget();

        return strikeDistance >= distance && strikeAngle >= angle;
    }

    #endregion

    #region Attack Checks

    private bool IsCircleAttack()
    {
        float randomValue = Random.value;

        bool chance = randomValue >= 0 && randomValue <= circleAttackChance;
        bool cooldowns = currentCircleCooldown <= 0 && currentOverallCooldown <= 0;

        return chance && cooldowns && IsNearCurrentTarget(circleAttackDistance);
    }

    private bool IsMeleeAttack()
    {
        return biteAttack.CanAttack && manyStrikesAttack.CanAttack && currentOverallCooldown <= 0;
    }

    private bool IsWhipAttack()
    {
        return leftWhipAttack.CanAttack && rightWhipAttack.CanAttack && currentOverallCooldown <= 0;
    }

    #endregion

    #region Cooldowns

    private void CheckCooldowns()
    {
        if(currentOverallCooldown > 0)
            currentOverallCooldown -= Time.deltaTime;

        if (currentCircleCooldown > 0)
            currentCircleCooldown -= Time.deltaTime;

        if (currentDigCooldown > 0)
            currentDigCooldown -= Time.deltaTime;
    }

    private void SetOverallCooldown()
    {
        currentOverallCooldown = overallAttackCooldown;
    }

    private void SetCircleAttackCooldown()
    {
        SetOverallCooldown();
        currentCircleCooldown = circleAttackCooldown;
    }

    private void SetDigCooldown()
    {
        SetOverallCooldown();
        currentDigCooldown = digCooldown;
    }

    #endregion

    private void SetPhaseStats(CentipedeBossPhase settings)
    {
        originalMoveSpeed = settings.MoveSpeed;
        originalMoveAcceleration = settings.Acceleration;
        originalTurnSpeed = settings.AngularSpeed;

        agent.speed = originalMoveSpeed;
        agent.acceleration = originalMoveAcceleration;
        agent.angularSpeed = originalTurnSpeed;

        biteAttack.AttackCooldown = settings.MeleeCooldown;
        manyStrikesAttack.AttackCooldown = settings.MeleeCooldown;

        leftWhipAttack.AttackCooldown = settings.WhipCooldown;
        rightWhipAttack.AttackCooldown = settings.WhipCooldown;

        // Атака по кругу
        circleAttackDistance = settings.CircleAttackDistance;
        circleRadius = settings.CircleRadius;
        circleMoveSpeed = settings.CircleMoveSpeed;
        circleAcceleration = settings.CircleAcceleration;
        circleTime = settings.CircleTime;
        circleAttackChance = settings.CircleAttackChance;
        circleAttackCooldown = settings.CircleAttackCooldown;

        // Атака с закопкой                                        
        hideMoveSpeed = settings.HoleMoveSpeed;
        hideAcceleration = settings.HoleAcceleration;
        digCooldown = settings.DigCooldown;
        sitInHoleTime = settings.SitInHoleTime;
        prepareOutOfHoleTime = settings.PrepareOutOfHoleTime;
        outOfHoleTime = settings.OutOfHoleTime;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Melee Attack
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeDistance);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, meleeAngle / 2, 0) * transform.forward * meleeDistance + transform.position);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, -meleeAngle / 2, 0) * transform.forward * meleeDistance + transform.position);

        // Circle Radius
        Gizmos.color = Color.magenta;
        if (!currentTarget.IsValid)
        {
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(currentTarget.Position, circleRadius);
        }
    }
}
