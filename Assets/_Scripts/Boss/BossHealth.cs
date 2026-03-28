using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class BossHealth : EntityHealth, ICameraLockable
{
    [Header("SFX Settings")]
    [Range(0f, 1f)][SerializeField] private float hurtSFXVolume = 1;
    [Range(0f, 1f)][SerializeField] private float deathSFXVolume = 1;

    [Header("Settings")]
    [SerializeField] private List<BossPhaseSettings> phases;
    private BossPhaseSettings currentPhase;

    [Header("Boss Components")]
    [SerializeField] private Transform lockOnPoint;
    [SerializeField] private EnemyBossComponents components;

    private Collider enemyCollider => components.EnemyCollider;
    private EnemySFXController sfxController => components.SFXController;

    [Header("Other")]
    // Есть ли у босса сегменты чтобы их дамажить
    [SerializeField] private bool segmentsBehaviour;
    private bool canTakeDamage = true;

    public event Action<BossPhaseSettings> OnPhaseChange;

    private bool canGetLockPoint = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            phases = phases.OrderByDescending(phase => phase.PhaseIndex).ToList();
            UpdatePhase();
        }

        OnDeath += TriggerDeathEffect;
    }

    protected override void SetHealthValues()
    {
        maxHealth *= EnemyManager.Instance.HealthMult;

        base.SetHealthValues();
    }

    public override float TakeDamage(AttackDamageType damage, EntityHealth sender)
    {
        if (IsDead || !canTakeDamage || !enabled)
            return 0f;

        float finalDamage = MathF.Round(GetFinalDamage(damage), 2);

        sfxController.PlayHurtSFX(hurtSFXVolume);

        DoOnDamageTaken(finalDamage);

        var statusData = GetFinalStatus(damage.StatusData);
        DoOnStatusTaken(statusData);

        UpdatePhase();

        if (segmentsBehaviour)
        {
            canTakeDamage = false;
            StartCoroutine(TakeDamageCooldown());
        }

        if (IsServer)
        {
            ChangeServerHealth_Rpc(currentHealth.Value - finalDamage);
            CheckForDeath_ServerSide();
        }
        else
        {
            ChangeClientSideHealth(clientSideHealth - finalDamage);
            CheckForDeath_ClientSide();
        }

        if (BossRoom.Instance != null)
            BossRoom.Instance.UpdateBossHealthUIDamageNumbers(finalDamage);

        return finalDamage;
    }

    private void TriggerDeathEffect()
    {
        if (!enabled)
            return;

        enemyCollider.enabled = false;
        sfxController.PlayDeathSFX(deathSFXVolume);

        RequestEnemyDespawn_ServerRpc(Time.fixedDeltaTime);
    }

    public override void Heal(float healAmount)
    {
        if (IsDead)
            return;

        healAmount = MathF.Round(healAmount, 2);
        ChangeClientSideHealth(clientSideHealth + healAmount);
    }

    public override void CreateHitEffect(HitTransform hitTransform)
    {
        ExecuteCreateHitEffect(hitTransform);
        CreateHitEffect_NotOwnerRpc(hitTransform);
    }

    private void ExecuteCreateHitEffect(HitTransform hitTransform)
    {
        Vector3 newPosition = enemyCollider.ClosestPoint(hitTransform.Position);

        GameObject hitEffect = Instantiate(hitEffectPrefab, newPosition, hitTransform.Rotation);
        hitEffect.transform.localScale = hitEffectScale;
    }

    [Rpc(SendTo.NotOwner)]
    private void CreateHitEffect_NotOwnerRpc(HitTransform hitTransform)
    {
        ExecuteCreateHitEffect(hitTransform);
    }

    protected override void UpdateHealthUI()
    {
        if (BossRoom.Instance != null)
            BossRoom.Instance.UpdateBossHealthUI(currentHealth.Value / maxHealth);
    }

    private void UpdatePhase()
    {
        float healthPercent = currentHealth.Value / maxHealth;

        foreach (var phase in phases)
        {
            if (phase.PhaseIndex > currentPhase.PhaseIndex && phase.PercentHealth >= healthPercent)
            {
                currentPhase = phase;
                OnPhaseChange?.Invoke(currentPhase);
                return;
            }
        }
    }

    private IEnumerator TakeDamageCooldown()
    {
        yield return new WaitForFixedUpdate();

        canTakeDamage = true;
    }

    #region ICameraLockable

    public Transform GetLockOnPoint()
    {
        if (!enabled || !canGetLockPoint)
            return null;

        return lockOnPoint;
    }

    public void SetDeathCallback(Action callback)
    {
        OnNetworkDespawnEvent += callback;
    }

    public void DeleteDeathCallback(Action callback)
    {
        OnNetworkDespawnEvent -= callback;
    }

    public void SetCanGetLockPoint(bool canGetLockPoint)
    {
        SetCanGetLockPoint_ServerRpc(canGetLockPoint);
    }

    private void ExecuteSetCanGetLockPoint(bool canGetLockPoint)
    {
        this.canGetLockPoint = canGetLockPoint;
    }

    [Rpc(SendTo.Server)]
    private void SetCanGetLockPoint_ServerRpc(bool canGetLockPoint)
    {
        SetCanGetLockPoint_EveryoneRpc(canGetLockPoint);
    }

    [Rpc(SendTo.Everyone)]
    private void SetCanGetLockPoint_EveryoneRpc(bool canGetLockPoint)
    {
        ExecuteSetCanGetLockPoint(canGetLockPoint);
    }

    #endregion
}