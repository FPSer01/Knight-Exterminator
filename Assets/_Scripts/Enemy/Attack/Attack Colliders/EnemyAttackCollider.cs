using System;
using Unity.Netcode;
using UnityEngine;

public class EnemyAttackCollider : NetworkBehaviour
{
    [Header("Base Attack Collider")]
    [SerializeField] protected Collider attackCollider;

    public event Action<PlayerHealth, HitTransform> OnHit;
    protected void DoOnHit(PlayerHealth target, HitTransform hitTransform) => OnHit?.Invoke(target, hitTransform);

    protected virtual void Awake()
    {
        attackCollider.isTrigger = true;
    }

    public void SetCollider(bool active)
    {
        SetCollider_EveryoneRpc(active);
    }

    [Rpc(SendTo.Everyone)]
    private void SetCollider_EveryoneRpc(bool active)
    {
        ExecuteSetCollider(active);
    }

    protected void ExecuteSetCollider(bool active)
    {
        attackCollider.enabled = active;
    }
}
