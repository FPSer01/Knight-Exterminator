using Unity.Netcode;
using UnityEngine;

public class EnemyMimicAttack : BaseEnemyAttack
{
    [Header("Mimic Settings")]
    [SerializeField] private EnemyTouchAttackCollider attackCollider;

    private bool attackInitiated = false;

    private void Start()
    {
        attackCollider.OnHit += AttackCollider_OnHit;
    }

    private void AttackCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        if (!canAttack)
            return;

        player.TakeDamage(attackDamage, GetComponent<EnemyHealth>());
        player.CreateHitEffect(hitPos);

        StartAttackCooldown_EveryoneRpc();
    }

    public override void Attack()
    {
        attackInitiated = !attackInitiated;

        attackCollider.SetCollider(attackInitiated);
    }

    [Rpc(SendTo.Everyone)]
    private void StartAttackCooldown_EveryoneRpc()
    {
        StartCoroutine(StartAttackCooldown());
    }
}
