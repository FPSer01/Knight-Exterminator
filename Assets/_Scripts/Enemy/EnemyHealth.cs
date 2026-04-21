using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyHealth : EntityHealth, ICameraLockable
{
    [Header("SFX Settings")]
    [Range(0f, 1f)][SerializeField] private float hurtSFXVolume = 1;
    [Range(0f, 1f)][SerializeField] private float deathSFXVolume = 1;

    [Header("Enemy Components")]
    [SerializeField] private Transform lockOnPoint;
    [SerializeField] private EnemyComponents components;

    [Header("UI")]
    [SerializeField] private EnemyHealthUI healthUI;

    private bool ignoreDamage = false;
    private bool canGetLockPoint = true;

    private Collider enemyCollider => components.EnemyCollider;
    private EnemySFXController sfxController => components.SFXController;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        OnDeath += TriggerDeathEffect;
    }

    protected override void SetHealthValues()
    {
        maxHealth *= EnemyManager.Instance.HealthMult;

        base.SetHealthValues();
    }

    private bool CheckForTakeDamageIgnore()
    {
        return IsDead || !enabled || ignoreDamage;
    }

    public override float TakeDamage(AttackDamageType damage, EntityHealth sender)
    {
        if (CheckForTakeDamageIgnore())
            return 0f;

        float finalDamage = MathF.Round(GetFinalDamage(damage), 2);

        sfxController.PlayHurtSFX(hurtSFXVolume);

        DoOnDamageTaken(finalDamage);

        var statusData = GetFinalStatus(damage.StatusData);
        DoOnStatusTaken(statusData);

        if (IsServer)
        {
            ChangeServerHealth_Rpc(currentHealth.Value - finalDamage);
        }
        else
        {
            ChangeClientSideHealth(clientSideHealth - finalDamage);
        }

        healthUI.UpdateDamageNumber(finalDamage);

        return finalDamage;
    }

    public override void Heal(float healAmount)
    {
        if (IsDead)
            return;

        healAmount = MathF.Round(healAmount, 2);
        ChangeClientSideHealth(clientSideHealth + healAmount);
    }

    private void TriggerDeathEffect()
    {
        if (!enabled)
            return;

        if (components != null)
        {
            enemyCollider.enabled = false;
            components.Behaviour.enabled = false;
            sfxController.PlayDeathSFX(deathSFXVolume);
        }

        if (healthUI != null)
            healthUI.StopAllProcesses();

        RequestEnemyDespawn_ServerRpc(Time.fixedDeltaTime);
    }

    public override void CreateHitEffect(HitTransform hitTransform)
    {
        ExecuteCreateHitEffect(hitTransform);
        CreateHitEffect_NotOwnerRpc(hitTransform);
    }

    private void ExecuteCreateHitEffect(HitTransform hitTransform)
    {
        if (ignoreDamage)
            return;

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
        if (healthUI == null)
            return;

        healthUI.UpdateHealthBar(currentHealth.Value / maxHealth);
    }

    public void SetIgnoreDamage(bool ignoreDamageValue)
    {
        SetIgnoreDamage_ServerRpc(ignoreDamageValue);
    }

    private void ExecuteSetIgnoreDamage(bool ignoreDamageValue)
    {
        ignoreDamage = ignoreDamageValue;
    }

    [Rpc(SendTo.Server)]
    private void SetIgnoreDamage_ServerRpc(bool ignoreDamageValue)
    {
        SetIgnoreDamage_EveryoneRpc(ignoreDamageValue);
    }

    [Rpc(SendTo.Everyone)]
    private void SetIgnoreDamage_EveryoneRpc(bool ignoreDamageValue)
    {
        ExecuteSetIgnoreDamage(ignoreDamageValue);
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