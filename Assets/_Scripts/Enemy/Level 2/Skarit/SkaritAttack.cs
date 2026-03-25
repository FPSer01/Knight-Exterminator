using UnityEngine;

public class SkaritAttack : EnemyAttack
{
    [SerializeField] private EnemyMeleeAttackCollider attackCollider;
    [SerializeField] private EnemySFXController sfxController;

    private void Start()
    {
        attackCollider.OnHit += AttackCollider_OnHit;
    }

    private void AttackCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(attackDamage, GetComponent<EnemyHealth>());
        player.CreateHitEffect(hitPos);
    }

    public override void Attack()
    {
        sfxController.PlayAttackSFX();
        attackCollider.StartAttackCheck();
    }
}
