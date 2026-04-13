using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static PamukAI.PAI;
using Random = UnityEngine.Random;

public class SpiderBossBehaviour : BaseEnemyBehaviour
{
    [Serializable]
    public struct SpiderBossPhase
    {
        [Header("Index")]
        public int Index;

        [Header("Agent Movement")]
        public float MoveSpeed;
        public float Acceleration;
        public float AngularSpeed;

        [Header("Range")]
        public float RangeAttackTriggerDistance;
        public float RangeCooldown;

        [Header("Summon")]
        public float SummonTime;
        public float OverallSummonCooldown;
        [Space]
        public int MinMiniSpidersAmount;
        public int MaxMiniSpidersAmount;
        public float SummonMiniSpidersCooldown;
        [Space]
        public int MinPoisonSpidersAmount;
        public int MaxPoisonSpidersAmount;
        public float SummonPoisonSpidersCooldown;
    }

    private readonly static string DEBUG = $"[{LogTags.BLUE_COLOR}Spider Behaviour{LogTags.END_COLOR}]";

    [Header("Spider Boss Behaviour: General")]
    [SerializeField] private float rotateSpeed;

    [Header("Spider Boss Behaviour: Flee")]
    [SerializeField] private float fleeTriggerDistance;
    [SerializeField] private float fleeDistance;
    [SerializeField] private float fleeCooldown;
    private float currentFleeCooldown;

    [Header("Spider Boss Behaviour: Melee Bite")]
    [SerializeField] private EnemyMeleeAttack meleeAttack;
    [SerializeField] private float meleeAttackDistance;
    [SerializeField] private float meleeAttackAngle;
    [SerializeField] private float delayBeforeMeleeAttack;
    [SerializeField] private float delayAfterMeleeAttack;

    [Header("Spider Boss Behaviour: Web Throw")]
    [SerializeField] private EnemyRangeAttack webAttack;
    [SerializeField] private float rangeAttackTriggerDistance;
    [SerializeField] private float delayBeforeRangeAttack;
    [SerializeField] private float delayAfterRangeAttack;

    [Header("Spider Boss Behaviour: Summon")]
    [SerializeField] private float summonTime;
    [SerializeField] private float overallSummonCooldown;
    private float currentOverallSummonCooldown;
    [SerializeField] private ParticleSystem summonVFX;
    [Space]
    [SerializeField] private EnemySummonAttack summonSpidersAttack;
    [SerializeField] private int minSpidersSummonAmount;
    [SerializeField] private int maxSpidersSummonAmount;
    [Space]
    [SerializeField] private EnemySummonAttack summonPoisonSpiderAttack;
    [SerializeField] private int minPoisonSpidersSummonAmount;
    [SerializeField] private int maxPoisonSpidersSummonAmount;

    [Header("Spider Boss Behaviour: Phases")]
    [SerializeField] private List<SpiderBossPhase> phaseSettings;

    [Header("Components")]
    [SerializeField] private EnemyBossComponents bossComponents;

    private EnemySFXController sfxController => bossComponents.SFXController;
    private BossHealth enemyHealth => bossComponents.Health as BossHealth;
    private Animator animator => bossComponents.Animator;

    private int currentPhaseIndex;

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

        enemyHealth.OnPhaseChange += OnPhaseChange;

        SwitchState(Idle, ref state);
    }

    private void SetPhaseStats(SpiderBossPhase phaseSettings)
    {
        agent.speed = phaseSettings.MoveSpeed;
        agent.acceleration = phaseSettings.Acceleration;
        agent.angularSpeed = phaseSettings.AngularSpeed;

        rangeAttackTriggerDistance = phaseSettings.RangeAttackTriggerDistance;
        webAttack.AttackCooldown = phaseSettings.RangeCooldown;

        summonTime = phaseSettings.SummonTime;
        overallSummonCooldown = phaseSettings.OverallSummonCooldown;

        minSpidersSummonAmount = phaseSettings.MinMiniSpidersAmount;
        maxSpidersSummonAmount = phaseSettings.MaxMiniSpidersAmount;
        summonSpidersAttack.AttackCooldown = phaseSettings.SummonMiniSpidersCooldown;

        minPoisonSpidersSummonAmount = phaseSettings.MinPoisonSpidersAmount;
        maxPoisonSpidersSummonAmount = phaseSettings.MaxPoisonSpidersAmount;
        summonPoisonSpiderAttack.AttackCooldown = phaseSettings.SummonPoisonSpidersCooldown;
    }

    protected override void Update()
    {
        base.Update();

        if (state != null)
            CheckCooldowns();
    }

    #region States

    private bool Idle()
    {
        if (DoOnce())
        {
            Debug.Log("Spider Idle State");
            animator.SetBool("Move", false);
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
                SwitchState(MoveTowardsTarget_Phase1, ref state);
                break;
            case 1:
                SwitchState(MoveTowardsTarget_Phase2, ref state);
                break;
            case 2:
                SwitchState(MoveTowardsTarget_Phase3, ref state);
                break;
            default:
                Debug.LogWarning("Boss default Phase State was triggered");
                SwitchState(MoveTowardsTarget_Phase1, ref state);
                break;
        }

        return true;
    }

    // Движение первой фазы
    private bool MoveTowardsTarget_Phase1()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);

            Debug.Log($"{DEBUG} [Phase 1] Move State");
        }

        // Проверка на ближнюю атаку
        if (CanMeleeStrike())
        {
            SwitchState(MeleeAttack, ref state);
        }

        // Проверка на дальнюю атаку
        if (CanWebThrow())
        {
            SwitchState(WebThrowAttack, ref state);
        }

        if (IsNearCurrentTarget(meleeAttackDistance))
        {
            PredictMoveTowardsTarget(2);
        }
        else
        {
            MoveTowardsTarget();
        }

        return true;
    }

    // Движение первой фазы
    private bool MoveTowardsTarget_Phase2()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            Debug.Log($"{DEBUG} [Phase 2] Move State");
        }

        // Проверка на призыв
        if (CanDoAnySummon())
        {
            DecideSummon();
        }

        // Проверка на ближнюю атаку
        if (CanMeleeStrike())
        {
            SwitchState(MeleeAttack, ref state);
        }

        // Проверка отступление
        if (CanFlee())
        {
            SwitchState(Flee, ref state);
        }

        // Проверка на дальнюю атаку
        if (CanWebThrow())
        {
            SwitchState(WebThrowAttack, ref state);
        }

        RotateTowardsTarget(rotateSpeed);

        return true;
    }

    // Движение первой фазы
    private bool MoveTowardsTarget_Phase3()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);
            Debug.Log($"{DEBUG} [Phase 3] Move State");
        }

        // Проверка на ближнюю атаку
        if (CanMeleeStrike())
        {
            SwitchState(MeleeAttack, ref state);
        }

        // Проверка на дальнюю атаку
        if (CanWebThrow())
        {
            SwitchState(WebThrowAttack, ref state);
        }

        // Проверка на призыв
        if (CanDoAnySummon())
        {
            DecideSummon();
        }

        if (IsNearCurrentTarget(meleeAttackDistance))
        {
            PredictMoveTowardsTarget(2);
        }
        else
        {
            MoveTowardsTarget();
        }

        return true;
    }

    private void DecideSummon()
    {
        bool canSummonMiniSpiders = CanSummonSpiders();
        bool canSummonPoisonSpider = CanSummonPoisonSpider();

        if (canSummonMiniSpiders && canSummonPoisonSpider)
        {
            var randomValue = Random.value;

            if (randomValue >= 0.5f)
                SwitchState(SummonPoisonSpiders, ref state);
            else
                SwitchState(SummonMiniSpiders, ref state);

            return;
        }
        
        if (canSummonMiniSpiders && !canSummonPoisonSpider)
        {
            SwitchState(SummonMiniSpiders, ref state);
        }
        else if (!canSummonMiniSpiders && canSummonPoisonSpider)
        {
            SwitchState(SummonPoisonSpiders, ref state);
        }
    }

    private bool MeleeAttack()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Bite");

            agent.enabled = false;

            Debug.Log($"{DEBUG} Melee Attack State");
        }

        if (Wait(delayBeforeMeleeAttack))
        {
            RotateTowardsTarget(rotateSpeed);
            return true;
        }

        if (DoOnce())
        {
            meleeAttack.Attack();
        }

        if (Wait(meleeAttack.TotalAttackTime + delayAfterMeleeAttack))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool WebThrowAttack()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Throw");

            agent.enabled = false;

            Debug.Log($"{DEBUG} Web Throw State");
        }

        if (Wait(delayBeforeRangeAttack))
        {
            RotateTowardsTarget(rotateSpeed);
            return true;
        }

        if (DoOnce())
        {
            var targetPosition = CalculatePredictedPosition(currentTarget, webAttack.ProjectileSpeed);

            webAttack.RangeAttack(targetPosition);
        }

        if (Wait(delayBeforeRangeAttack))
        {
            RotateTowardsTarget(rotateSpeed);
            return true;
        }

        if (Step())
        {
            agent.enabled = true;
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool SummonMiniSpiders()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            PlaySummonVFX_EveryoneRpc();

            agent.enabled = false;

            int amount = Random.Range(minSpidersSummonAmount, maxSpidersSummonAmount + 1);

            summonSpidersAttack.SummonAttack(amount);

            Debug.Log($"{DEBUG} Summon State");
        }

        if (Wait(summonTime))
        {
            return true;
        }

        if (Step())
        {
            SetSummonCooldown();

            StopSummonVFX_EveryoneRpc();
            agent.enabled = true;
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool SummonPoisonSpiders()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            PlaySummonVFX_EveryoneRpc();

            agent.enabled = false;

            int amount = Random.Range(minPoisonSpidersSummonAmount, maxPoisonSpidersSummonAmount + 1);

            summonPoisonSpiderAttack.SummonAttack(amount);

            Debug.Log($"{DEBUG} Special Summon State");
        }

        if (Wait(summonTime))
        {
            return true;
        }

        if (Step())
        {
            SetSummonCooldown();

            StopSummonVFX_EveryoneRpc();
            agent.enabled = true;
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    private bool Flee()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);
            MoveFromTarget(fleeDistance);

            Debug.Log($"{DEBUG} Flee State");
        }

        if (agent.remainingDistance <= 0.1f)
        {
            SetFleeCooldown();
            SwitchState(DecideState, ref state);
        }

        return true;
    }

    #endregion

    #region VFX

    [Rpc(SendTo.Everyone)]
    private void PlaySummonVFX_EveryoneRpc()
    {
        summonVFX.Play();
    }

    [Rpc(SendTo.Everyone)]
    private void StopSummonVFX_EveryoneRpc()
    {
        summonVFX.Stop();
    }

    #endregion

    #region Cooldown Methods

    private void SetFleeCooldown()
    {
        currentFleeCooldown = fleeCooldown;
    }

    private void SetSummonCooldown()
    {
        currentOverallSummonCooldown = overallSummonCooldown;
    }

    private void CheckCooldowns()
    {
        if (currentFleeCooldown > 0)
            currentFleeCooldown -= Time.deltaTime;

        if (currentOverallSummonCooldown > 0)
            currentOverallSummonCooldown -= Time.deltaTime;
    }

    #endregion

    #region Check Methods

    private bool CanMeleeStrike()
    {
        return IsNearCurrentTarget(meleeAttackDistance) && meleeAttackAngle >= GetHorizontalAngleToTarget() && meleeAttack.CanAttack;
    }

    private bool CanWebThrow()
    {
        return !IsNearCurrentTarget(rangeAttackTriggerDistance) && webAttack.CanAttack;
    }

    private bool CanDoAnySummon()
    {
        return CanSummonSpiders() || CanSummonPoisonSpider();
    }

    private bool CanSummonSpiders()
    {
        return summonSpidersAttack.SummonsCount <= 0 && summonSpidersAttack.CanAttack && currentOverallSummonCooldown <= 0;
    }

    private bool CanSummonPoisonSpider()
    {
        return summonPoisonSpiderAttack.SummonsCount <= 0 && summonPoisonSpiderAttack.CanAttack && currentOverallSummonCooldown <= 0;
    }

    private bool CanFlee()
    {
        return IsNearCurrentTarget(fleeTriggerDistance) && currentFleeCooldown <= 0;
    }

    #endregion

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Flee
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, fleeTriggerDistance);

        // Range Attack
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rangeAttackTriggerDistance);

        // Melee Attack
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackDistance);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, meleeAttackAngle / 2, 0) * transform.forward * meleeAttackDistance + transform.position);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, -meleeAttackAngle / 2, 0) * transform.forward * meleeAttackDistance + transform.position);
    }
}
