using Unity.Netcode;
using UnityEngine;

public class EnemyVampirism : NetworkBehaviour
{
    [SerializeField] private EnemyAttackCollider attackCollider;
    [Space]
    [SerializeField] private float healAmount;
    [Range(0f, 1f)][SerializeField] private float healFromMaxHealth;
    [Space]
    [SerializeField] private EnemyComponents components;

    public override void OnNetworkSpawn()
    {
        attackCollider.OnHit += AttackCollider_OnHit;
    }

    private void AttackCollider_OnHit(PlayerHealth target, HitTransform hitPos)
    {
        EnemyHealth health = components.Health as EnemyHealth;

        if (healAmount > 0)
        {
            health.Heal(healAmount);
        }
        else if (healFromMaxHealth > 0)
        {
            health.Heal(health.MaxHealth * healFromMaxHealth);
        }
    }
}
