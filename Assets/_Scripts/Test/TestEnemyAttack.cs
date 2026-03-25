using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TestEnemyAttack : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] protected AttackDamageType attackDamage;
    [SerializeField] protected float attackCooldown;
    protected float currentAttackCooldown;

    public AttackDamageType AttackDamage { get => attackDamage; set => attackDamage = value; }
    public float AttackCooldown { get => attackCooldown; set => attackCooldown = value; }
    public float CurrentAttackCooldown { get => currentAttackCooldown; set => currentAttackCooldown = value; }
    [SerializeField] private EnemyMeleeAttackCollider attackCollider;

    [Rpc(SendTo.Everyone)]
    private void ExecuteAttack_EveryoneRpc()
    {
        attackCollider.StartAttackCheck();
    }

    public override void OnNetworkSpawn()
    {
        attackCollider.OnHit += AttackCollider_OnHit;

        if (!IsServer)
            return;

        StartCoroutine(AttackSequence());
    }

    public override void OnNetworkDespawn()
    {
        attackCollider.OnHit -= AttackCollider_OnHit;

        if (!IsServer)
            return;
    }

    private IEnumerator AttackSequence()
    {
        while (true)
        {
            ExecuteAttack_EveryoneRpc();
            yield return new WaitForSeconds(attackCooldown);
        }
    }

    private void AttackCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(attackDamage, GetComponent<EnemyHealth>());
        player.CreateHitEffect(hitPos);
    }

}
