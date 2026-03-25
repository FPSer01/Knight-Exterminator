using System;
using System.Collections.Generic;
using UnityEngine;
using static PamukAI.PAI;
using Random = UnityEngine.Random;

public class ScorpionBossBehaviour : EnemyBehaviour
{
    [Header("General")]
    [SerializeField] private float seeDistance;
    [SerializeField] private float closeDistance;
    [SerializeField] private float turnSpeedToTarget;
    [SerializeField] private float overallCooldown;
    [SerializeField] private float meleeDistance;
    [SerializeField] private float meleeAngle;
    private float currentOverallCooldown;

    private float originalMoveSpeed;
    private float originalAcceleration;
    private float originalAngularSpeed;

    [Header("Claws")]
    [SerializeField] private float clawsCooldown;
    private float currentClawsCooldown;
    [SerializeField] private float clawsAttackDelay;
    [SerializeField] private float clawsTimeAfterAttack;
    [SerializeField] private float clawsTimeBeforeAttack;
    private bool clawsAttackHappened = false;

    [Header("Tail")]
    [SerializeField] private float tailCooldown;
    private float currentTailCooldown;
    [SerializeField] private float tailAttackDelay;
    [SerializeField] private float tailTimeAfterAttack;
    [SerializeField] private float tailTimeBeforeAttack;

    [Header("Wind")]
    [SerializeField] private float windPositionDistance;
    [SerializeField] private float windMoveSpeed;
    [Range(0f, 1f)] [SerializeField] private float windAttackChance;
    [SerializeField] private float windCooldown;
    private float currentWindCooldown;
    [SerializeField] private float windAttackDelay;
    [SerializeField] private float windTimeAfterAttack;
    [SerializeField] private float windTimeBeforeAttack;

    [Header("Dig")]
    [SerializeField] private float moveSpeedUnderground;
    [Range(0f, 1f)] [SerializeField] private float digChance;
    [SerializeField] private float digInTime;
    [SerializeField] private float digOutDelay;
    [SerializeField] private float digOutTime;
    [SerializeField] private float digCooldown;
    private float currentDigCooldown;
    [SerializeField] private ParticleSystem moveUndergroundVFX;
    [SerializeField] private AudioSource moveUndergroundSFX;

    [Header("Phase Control")]
    [SerializeField] private List<ScorpionBossPhase> phaseSettings;

    [Header("Components")]
    [SerializeField] private EnemySFXController sfxController;
    [SerializeField] private BossHealth enemyHealth;
    [SerializeField] private ScorpionBossAttack enemyAttack;
    [SerializeField] private Animator animator;
    [SerializeField] private List<Collider> colliders;

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

        SwitchState(Idle, ref state);
        Debug.Log($"<color=#FF0000>[Phase] Change to Index {currentPhaseIndex}, Phase {currentPhaseIndex + 1}</color>");
    }

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
            Debug.Log("Scorpion Idle State");
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
        }

        CheckCooldowns();

        if (IsWindAttack())
        {
            SwitchState(PrepareWindAttack, ref state);
        }

        if (IsClawsAttack() && !clawsAttackHappened)
        {
            SwitchState(ClawsAttack, ref state);
        }
        else if (IsTailAttack())
        {
            SwitchState(TailAttack, ref state);
        }

        if (IsNear(transform, mainTarget, closeDistance))
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
        }

        CheckCooldowns();

        if (IsDig())
        {
            SwitchState(DigIn, ref state);
        }

        if (IsWindAttack())
        {
            SwitchState(PrepareWindAttack, ref state);
        }

        if (IsClawsAttack())
        {
            SwitchState(ClawsAttack, ref state);
        }

        if (IsTailAttack())
        {
            SwitchState(TailAttack, ref state);
        }

        if (IsNear(transform, mainTarget, closeDistance))
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
        }

        CheckCooldowns();

        if (IsDig())
        {
            SwitchState(DigIn, ref state);
        }

        if (IsWindAttack())
        {
            SwitchState(PrepareWindAttack, ref state);
        }

        if (IsTailAttack())
        {
            SwitchState(TailAttack, ref state);
        }

        if (IsClawsAttack())
        {
            SwitchState(ClawsAttack, ref state);
        }

        if (IsNear(transform, mainTarget, closeDistance))
            PredictMoveTowardsTarget();
        else
            MoveTowardsTarget();

        return true;
    }

    private bool ClawsAttack()
    {
        if (Wait(clawsTimeBeforeAttack))
        {
            return true;
        }

        if (DoOnce())
        {
            Debug.Log("Scorpion Claws State");

            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Claws");

            agent.enabled = false;
        }

        if (Wait(clawsAttackDelay))
        {
            RotateTowardsTarget(turnSpeedToTarget);
            return true;
        }
        
        if (DoOnce())
        {
            enemyAttack.ClawsAttack();
        }

        if (Wait(clawsTimeAfterAttack))
        {
            return true;
        }

        if (Step())
        {
            clawsAttackHappened = true;
            agent.enabled = true;
            SetClawsCooldown();
            SetOverallCooldown();

            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool TailAttack()
    {
        if (Wait(tailTimeBeforeAttack))
        {
            return true;
        }

        if (DoOnce())
        {
            Debug.Log("Scorpion Tail State");

            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Tail");

            agent.enabled = false;
        }

        if (Wait(tailAttackDelay))
        {
            RotateTowardsTarget(turnSpeedToTarget);
            return true;
        }

        if (DoOnce())
        {
            enemyAttack.TailAttack();
        }

        if (Wait(tailTimeAfterAttack))
        {
            return true;
        }

        if (Step())
        {
            clawsAttackHappened = false;
            agent.enabled = true;
            SetTailCooldown();
            SetOverallCooldown();

            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool PrepareWindAttack()
    {
        if (DoOnce())
        {
            Debug.Log("Scorpion Prepare Wind State");
            agent.speed = windMoveSpeed;
            agent.autoBraking = false;

            Vector3 destinationPoint = transform.position + GetWindCirclePoint();
            agent.SetDestination(destinationPoint);
        }

        if (Wait(5f))
        {
            if (agent.remainingDistance < 0.1f)
            {
                SwitchState(WindAttack, ref state);
            }

            return true;
        }

        if (Step())
        {
            SwitchState(WindAttack, ref state);
        }

        return true;
    }

    private bool WindAttack()
    {
        if (Wait(windTimeBeforeAttack))
        {
            return true;
        }

        if (DoOnce())
        {
            Debug.Log("Scorpion Wind State");

            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Wind");

            agent.speed = originalMoveSpeed;
            agent.enabled = false;
        }

        if (Wait(windAttackDelay))
        {
            RotateBackwardsTarget(turnSpeedToTarget);
            return true;
        }

        if (DoOnce())
        {
            enemyAttack.WindAttack();
        }

        if (Wait(windTimeAfterAttack))
        {
            return true;
        }

        if (Step())
        {
            agent.autoBraking = true;
            agent.enabled = true;
            SetWindCooldown();
            SetOverallCooldown();

            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool DigIn()
    {
        if (DoOnce())
        {
            Debug.Log("Scorpion Dig In State");

            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetBool("Underground", true);

            agent.speed = moveSpeedUnderground;
            agent.enabled = false;
            SetColliders(false);
        }

        if (Wait(digInTime))
        {
            return true;
        }

        if (Step())
        {
            SwitchState(MoveUnderground, ref state);
        }

        return true;
    }

    private bool MoveUnderground()
    {
        if (DoOnce())
        {
            Debug.Log("Scorpion Move Underground State");

            moveUndergroundSFX.Play();

            agent.autoBraking = false;
            agent.enabled = true;
            moveUndergroundVFX.Play();
            MoveTowardsTarget();
        }

        if (IsNear(transform, mainTarget, 0.25f))
            SwitchState(DigOut, ref state);
        else
            MoveTowardsTarget();

        return true;
    }

    private bool DigOut()
    {
        if (Wait(digOutDelay))
        {
            return true;
        }

        if (DoOnce())
        {
            Debug.Log("Scorpion Dig Out State");

            moveUndergroundSFX.Stop();

            agent.enabled = false;
            moveUndergroundVFX.Stop();
            animator.SetBool("Underground", false);
            enemyAttack.DigOutExplosion();
        }

        if (Wait(digOutTime))
        {
            return true;
        }

        if (Step())
        {
            agent.speed = originalMoveSpeed;
            agent.autoBraking = true;
            agent.enabled = true;

            SetOverallCooldown();
            SetDigCooldown();
            SetColliders(true);

            SwitchState(DecideState, ref state);
        }

        return true;
    }

    #endregion

    private void SetColliders(bool active)
    {
        colliders.ForEach(col => col.enabled = active);
    }

    private Vector3 GetWindCirclePoint()
    {
        Vector3 direction = (mainTarget.position - transform.position).normalized;
        return direction * windPositionDistance;
    }

    #region Checks

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

    private bool IsClawsAttack()
    {
        return CheckForStrike(meleeDistance, meleeAngle) && currentClawsCooldown <= 0 && currentOverallCooldown <= 0;
    }

    private bool IsTailAttack()
    {
        return CheckForStrike(meleeDistance, meleeAngle) && currentTailCooldown <= 0 && currentOverallCooldown <= 0;
    }

    private bool IsWindAttack()
    {
        return (Random.value < windAttackChance * Time.deltaTime) && currentWindCooldown <= 0 && currentOverallCooldown <= 0;
    }

    private bool IsDig()
    {
        return (Random.value < digChance * Time.deltaTime) && currentDigCooldown <= 0 && currentOverallCooldown <= 0;
    }

    #endregion

    #region Cooldowns

    private void CheckCooldowns()
    {
        TickCooldown(ref currentOverallCooldown);
        TickCooldown(ref currentClawsCooldown);
        TickCooldown(ref currentTailCooldown);
        TickCooldown(ref currentWindCooldown);
        TickCooldown(ref currentDigCooldown);
    }

    private void SetOverallCooldown()
    {
        currentOverallCooldown = overallCooldown;
    }

    private void SetClawsCooldown()
    {
        currentClawsCooldown = clawsCooldown;
    }

    private void SetTailCooldown()
    {
        currentTailCooldown = tailCooldown;
    }

    private void SetWindCooldown()
    {
        currentWindCooldown = windCooldown;
    }

    private void SetDigCooldown()
    {
        currentDigCooldown = digCooldown;
    }

    private void TickCooldown(ref float cooldownParam)
    {
        if (cooldownParam > 0)
            cooldownParam -= Time.deltaTime;
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        // See
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, seeDistance);

        // Close
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, closeDistance);

        // Melee Attack
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeDistance);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, meleeAngle / 2, 0) * transform.forward * meleeDistance + transform.position);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, -meleeAngle / 2, 0) * transform.forward * meleeDistance + transform.position);

        // See
        if (mainTarget == null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, windPositionDistance);
        }
        else
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(mainTarget.position, windPositionDistance);
        }
    }

    private void SetPhaseStats(ScorpionBossPhase settings)
    {
        originalMoveSpeed = settings.MoveSpeed;
        originalAcceleration = settings.Acceleration;
        originalAngularSpeed = settings.AngularSpeed;

        agent.speed = originalMoveSpeed;
        agent.acceleration = originalAcceleration;
        agent.angularSpeed = originalAngularSpeed;

        overallCooldown = settings.OverallCooldown;

        clawsCooldown = settings.ClawsCooldown;
        clawsTimeBeforeAttack = settings.ClawsTimeBeforeAttack;
        clawsTimeAfterAttack = settings.ClawsTimeAfterAttack;

        tailCooldown = settings.TailCooldown;
        tailTimeBeforeAttack = settings.TailTimeBeforeAttack;
        tailTimeAfterAttack = settings.TailTimeAfterAttack;

        windCooldown = settings.WindCooldown;
        windAttackChance = settings.WindChance;
        windTimeBeforeAttack = settings.WindTimeBeforeAttack;
        windTimeAfterAttack = settings.WindTimeAfterAttack;

        digChance = settings.DigChance;
        digCooldown = settings.DigCooldown;
        digOutDelay = settings.DigOutDelay;
    }

    [Serializable]
    public class ScorpionBossPhase
    {
        [Header("Index")]
        public int Index;

        [Header("General")]
        public float OverallCooldown;
        public float MoveSpeed;
        public float Acceleration;
        public float AngularSpeed;

        [Header("Claws")]
        public float ClawsCooldown;
        public float ClawsTimeBeforeAttack;
        public float ClawsTimeAfterAttack;

        [Header("Tail")]
        public float TailCooldown;
        public float TailTimeBeforeAttack;
        public float TailTimeAfterAttack;

        [Header("Wind")]
        [Range(0f, 1f)] public float WindChance;
        public float WindCooldown;
        public float WindTimeBeforeAttack;
        public float WindTimeAfterAttack;

        [Header("Dig")]
        [Range(0f, 1f)] public float DigChance;
        public float DigCooldown;
        public float DigOutDelay;
    }
}


