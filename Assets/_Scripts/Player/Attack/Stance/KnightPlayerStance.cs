using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KnightPlayerStance : PlayerStanceBase
{
    [Header("Attack Stance (Thrust)")]
    [SerializeField] private PlayerAttackCollider thrustAttackCollider;
    [SerializeField] private float thrustDistance;
    [SerializeField] private float thrustAttackMult = 2f;
    [SerializeField] private float thrustDelay;

    [Header("Defense Stance (Reflect)")]
    [SerializeField] private float defenseStanceDelay;
    [SerializeField] private float cutDamageMult;
    [SerializeField] private ParticleSystem shieldVFX;

    [Header("Dexterity Stance (Infinite Stamina)")]
    [SerializeField] private float staminaStanceDelay;
    [SerializeField] private float moveMultIncrease;

    public override float StanceDamageMult { get => thrustAttackMult; set => thrustAttackMult = value; }

    #region Base Stance Methods

    public override void ExecuteSetStance(StanceType type)
    {
        base.ExecuteSetStance(type);

        thrustAttackCollider.OnEnemyHit += OnThrustHit;
        thrustAttackCollider.SetCollider(false);
        thrustAttackCollider.SetTriggerOnEnter(true);
    }

    public override void ResetSkillState(bool displayMessage = true)
    {
        base.ResetSkillState(displayMessage);

        StopStanceSFX();

        // ATTACK: выпад
        EnableThrustVFX(false);

        // DEFENSE: щит
        EnableShieldVFX(false);
        playerHealth.SetReflectStance(false);

        // DEXTERITY: стамина
        playerStamina.BlockStaminaConsumage(false);
        playerHealth.SetAvoidence(false);
        playerMovement.OverallMoveSpeedMult = 1f;
    }

    protected override void ActivateStanceSkill()
    {
        base.ActivateStanceSkill();

        switch (currentStance.Type)
        {
            case StanceType.Attack:
                StartCoroutine(DoThrustAttack());
                break;

            case StanceType.Defense:
                StartCoroutine(DoReflectAction());
                break;

            case StanceType.Dexterity:
                StartCoroutine(DoInfiniteStamina());
                break;

            default:
                Debug.LogError($"Нет заданной стойки у {nameof(KnightPlayerStance)}", this);
                break;
        }
    }

    #endregion

    #region Knight Stance Methods

    private IEnumerator DoThrustAttack()
    {
        if (!playerMovement.OnGround || playerMovement.MoveInput == Vector2.zero)
        {
            yield break;
        }

        skillActive = true;
        ActivateStanceAnimation(true);
        playerComponents.ActivateRig(false);
        playerMovement.BlockMovement(true);
        playerMovement.BlockTurn(true);

        PlayStanceSFX(StanceType.Attack);

        yield return new WaitForSeconds(thrustDelay);

        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);

        playerState.DoStanceBarAnimation(0, currentStance.Duration);

        thrustAttackCollider.SetCollider(true);
        EnableThrustVFX(true);
        playerMovement.Thrust(thrustDistance, currentStance.Duration, 0.25f);

        yield return new WaitForSeconds(currentStance.Duration);

        playerComponents.ActivateRig(true);
        EnableThrustVFX(false, true);
        thrustAttackCollider.SetCollider(false);

        skillActive = false;
        ActivateStanceAnimation(false);
        StartCoroutine(SkillCooldown(currentStance.Cooldown));
    }

    private IEnumerator DoReflectAction()
    {
        if (!playerMovement.OnGround)
        {
            yield break;
        }

        skillActive = true;
        ActivateStanceAnimation(true);
        playerMovement.BlockMovement(true);
        playerMovement.BlockTurn(true);

        EnableShieldVFX(true);
        PlayStanceSFX(StanceType.Defense);

        yield return new WaitForSeconds(defenseStanceDelay);

        ActivateStanceAnimation(false);

        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);
        playerState.DoStanceBarAnimation(0, currentStance.Duration);
        playerHealth.SetReflectStance(true, cutDamageMult);

        yield return new WaitForSeconds(currentStance.Duration);

        playerHealth.SetReflectStance(false);

        EnableShieldVFX(false);
        StopStanceSFX();
        
        StartCoroutine(SkillCooldown(currentStance.Cooldown));
        skillActive = false;
    }

    private IEnumerator DoInfiniteStamina()
    {
        if (!playerMovement.OnGround)
        {
            yield break;
        }

        skillActive = true;
        ActivateStanceAnimation(true);
        playerMovement.BlockMovement(true);
        playerMovement.BlockTurn(true);

        PlayStanceSFX(StanceType.Dexterity);
        PlayStaminaBurstVFX();

        yield return new WaitForSeconds(staminaStanceDelay);

        ActivateStanceAnimation(false);
        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);

        playerState.DoStanceBarAnimation(0, currentStance.Duration);
        playerStamina.BlockStaminaConsumage(true);
        playerHealth.SetAvoidence(true, 0.5f);
        playerMovement.OverallMoveSpeedMult = 1f + moveMultIncrease;

        yield return new WaitForSeconds(currentStance.Duration);

        playerStamina.BlockStaminaConsumage(false);
        playerHealth.SetAvoidence(false);
        playerMovement.OverallMoveSpeedMult = 1f;

        skillActive = false;
        StartCoroutine(SkillCooldown(currentStance.Cooldown));
    }

    private void OnThrustHit(EntityHealth enemy, HitTransform transform)
    {
        float enemyDamageTaken = enemy.TakeDamage(playerAttack.AttackDamage.GetMultDamage(thrustAttackMult), playerComponents.Health);
        enemy.CreateHitEffect(transform);

        playerAttack.TryVampireHeal(enemyDamageTaken);
    }

    #endregion

    #region Effects and Visuals

    #region Thrust VFX

    private void EnableThrustVFX(bool active, bool clear = false)
    {
        ExecuteEnableThrustVFX(active, clear);
        EnableThrustVFX_ToServerRpc(active, clear);
    }

    private void ExecuteEnableThrustVFX(bool active, bool clear = false)
    {
        thrustAttackCollider.SlashEffectActive(active, clear);
    }

    [Rpc(SendTo.Server)]
    private void EnableThrustVFX_ToServerRpc(bool active, bool clear = false)
    {
        EnableThrustVFX_ToEveryoneRpc(active, clear);
    }

    [Rpc(SendTo.NotOwner)]
    private void EnableThrustVFX_ToEveryoneRpc(bool active, bool clear = false)
    {
        thrustAttackCollider.SlashEffectActive(active, clear);
    }

    #endregion

    #region Shield VFX

    private void EnableShieldVFX(bool enable)
    {
        ExecuteEnableShieldVFX(enable);
        EnableShieldVFX_ToServerRpc(enable);
    }

    private void ExecuteEnableShieldVFX(bool enable)
    {
        if (enable)
        {
            shieldVFX.Play();
        }
        else
        {
            shieldVFX.Stop();
            shieldVFX.Clear(true);
        }
    }

    [Rpc(SendTo.Server)]
    private void EnableShieldVFX_ToServerRpc(bool enable)
    {
        EnableShieldVFX_ToEveryoneRpc(enable);
    }

    [Rpc(SendTo.NotOwner)]
    private void EnableShieldVFX_ToEveryoneRpc(bool enable)
    {
        ExecuteEnableShieldVFX(enable);
    }

    #endregion

    #region Stamina Burst VFX

    private void PlayStaminaBurstVFX()
    {
        ExecutePlayStaminaBurstVFX();
        PlayStaminaBurstVFX_ToServerRpc();
    }

    private void ExecutePlayStaminaBurstVFX()
    {
        playerStamina.PlayBurstVFX();
    }

    [Rpc(SendTo.Server)]
    private void PlayStaminaBurstVFX_ToServerRpc()
    {
        PlayStaminaBurstVFX_ToEveryoneRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayStaminaBurstVFX_ToEveryoneRpc()
    {
        ExecutePlayStaminaBurstVFX();
    }

    #endregion

    #endregion
}
