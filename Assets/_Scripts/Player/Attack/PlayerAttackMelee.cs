using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttackMelee : PlayerAttackBase
{
    [Header("Melee: Settings")]
    [SerializeField] private float timeToEndCombo;

    [Header("Melee: Components")]
    [SerializeField] private List<PlayerAttackCollider> attackColliders;

    private int currentAttackIndex;
    private Coroutine comboTimerCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SubscribeToAttackColliders(true);

        IsInitialized = true;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        SubscribeToAttackColliders(false);
    }

    protected override void UpdateAttackType()
    {
        ElementalAttackState state = ElementalAttackState.None;

        if (attackDamage.Fire > 0)
            state = ElementalAttackState.Fire;
        else if (attackDamage.Electrical > 0)
            state = ElementalAttackState.Electric;

        attackColliders.ForEach((col) => col.SetElementalAttackState(state));
    }

    public override void BlockAttack(bool block)
    {
        base.BlockAttack(block);

        if (blockAttack == true && block == false)
        {
            if (comboTimerCoroutine != null)
                StopCoroutine(comboTimerCoroutine);

            SetCurrentAttackIndex(0);
        }

        blockAttack = block;
    }

    private void SetCurrentAttackIndex(int index)
    {
        currentAttackIndex = index;
        animator.SetInteger("Attack Index", currentAttackIndex);
    }

    /// <summary>
    /// Метод для проверки удара ближней атакой. Вызывается в AnimationEvent
    /// </summary>
    /// <param name="attackIndex"></param>
    public void CheckForHit(int attackIndex)
    {
        SetCurrentAttackIndex(attackIndex);

        if (currentAttackIndex > 0)
        {
            if (comboTimerCoroutine != null)
                StopCoroutine(comboTimerCoroutine);

            comboTimerCoroutine = StartCoroutine(EndComboTimer());
        }

        sfxController.PlayAttackSFX(false);
        playerStamina.ConsumeStamina(playerStamina.AttackConsumage);

        attackColliders[attackIndex].FixedUpdateAttackCheck();

        if (playerStamina.CurrentStamina < playerStamina.AttackConsumage && attackInput)
        {
            SetAttackInput(false);
            SetCurrentAttackIndex(0);
        }
    }

    private void SubscribeToAttackColliders(bool subscribe)
    {
        if (subscribe)
        {
            foreach (var attackCol in attackColliders)
            {
                attackCol.OnEnemyHit += AttackCollider_OnEnemyHit;
            }
        }
        else
        {
            foreach (var attackCol in attackColliders)
            {
                attackCol.OnEnemyHit -= AttackCollider_OnEnemyHit;
            }
        }
    }

    private void AttackCollider_OnEnemyHit(EntityHealth enemy, HitTransform hit)
    {
        var damage = attackDamage;

#if UNITY_EDITOR || DEBUG
        if (infiniteDamageCheatEnabled)
        {
            damage = attackDamage.GetMultDamage(99999);
            damage.IgnoreDefence = true;
        }
#endif

        float enemyDamagetaken = enemy.TakeDamage(damage, playerHealth);
        enemy.CreateHitEffect(hit);

        TryVampireHeal(enemyDamagetaken);
    }

    private IEnumerator EndComboTimer()
    {
        yield return new WaitForSeconds(timeToEndCombo);

        SetCurrentAttackIndex(0);
    }
}
