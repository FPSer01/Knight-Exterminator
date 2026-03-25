using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EnemyProjectile : NetworkBehaviour
{
    [SerializeField] private Rigidbody projectileRigidbody;
    [SerializeField] private Collider projectileCollider;
    [Space]
    [SerializeField] private GameObject projectileHitEffectPrefab;

    private AttackDamageType projectileDamage;

    public event Action<PlayerHealth, AttackDamageType, HitTransform> OnHit;

    public void SetupProjectile(AttackDamageType damage)
    {
        projectileDamage = damage;
    }

    public void SetSpeed(float speed, Vector3 direction)
    {
        projectileRigidbody.linearVelocity = speed * direction;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log($"Collision with: {collision.gameObject.name}");

        if (collision.collider.TryGetComponent(out PlayerHealth player))
        {
            Vector3 hitPos = projectileCollider.ClosestPoint(player.transform.position);
            OnHit?.Invoke(player, projectileDamage, new HitTransform(hitPos, transform.rotation));
        }

        DoCollideResponce();
    }

    private void DoCollideResponce()
    {
        if (projectileHitEffectPrefab != null)
            Instantiate(projectileHitEffectPrefab, transform.position, Quaternion.identity);

        RequestDespawn_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestDespawn_ServerRpc()
    {
        NetworkObject.Despawn();
    }
}
