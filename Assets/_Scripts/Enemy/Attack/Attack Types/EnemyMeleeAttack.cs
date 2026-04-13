using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeAttack : BaseEnemyAttack
{
    [Header("Melee Settings")]
    [SerializeField] private List<EnemyMeleeAttackCollider> attackColliders;
    [SerializeField] private float timeBetweenEachAttack;

    [Header("SFX Settings")]
    [SerializeField] private bool useAttackSFX = true;
    [SerializeField] private string customSFXTag;
    [SerializeField] private bool playSFXAtEachAttack;
    [Range(0f, 1f)][SerializeField] private float attackSFXVolume = 1;

    [Header("Components")]
    [SerializeField] private EnemyComponents components;

    private EnemySFXController sfxController => components.SFXController;

    public float TimeBetweenEachAttack { get => timeBetweenEachAttack; }
    public int AttackCount { get => attackColliders.Count; }
    public float TotalAttackTime { get => timeBetweenEachAttack * attackColliders.Count; }

    private void Start()
    {
        attackColliders.ForEach((col) => col.OnHit += AttackCollider_OnHit);
    }

    private void AttackCollider_OnHit(PlayerHealth player, HitTransform transform)
    {
        player.TakeDamage(attackDamage, GetComponent<EnemyHealth>());
        player.CreateHitEffect(transform);
    }

    public override void Attack()
    {
        if (!canAttack)
            return;

        PlayAttackSFX();
        StartCoroutine(InitiateMeleeAttack());
    }

    private void PlayAttackSFX()
    {
        if (!playSFXAtEachAttack && useAttackSFX)
        {
            sfxController.PlayAttackSFX(attackSFXVolume);
        }
        else if (!useAttackSFX)
        {
            sfxController.PlayCustomSFX(customSFXTag);
        }
    }

    private IEnumerator InitiateMeleeAttack()
    {
        canAttack = false;

        foreach (var collider in attackColliders)
        {
            collider.StartAttackCheck();

            if (playSFXAtEachAttack)
            {
                sfxController.PlayAttackSFX(attackSFXVolume);
            }

            yield return new WaitForSeconds(timeBetweenEachAttack);
        }

        StartCoroutine(StartAttackCooldown());
    }
}
