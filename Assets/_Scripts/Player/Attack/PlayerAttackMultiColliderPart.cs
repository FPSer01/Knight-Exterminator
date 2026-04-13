using System;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttackMultiColliderPart : NetworkBehaviour
{
    [SerializeField] private Collider colliderPart;

    public event Action<EntityHealth, HitTransform> OnHit;
    private void DoOnHit(EntityHealth enemy, HitTransform hitPos) => OnHit?.Invoke(enemy, hitPos);

    private void Start()
    {
        colliderPart.isTrigger = true;
    }

    public void SetColliderActive(bool active)
    {
        colliderPart.enabled = active;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out EntityHealth enemy) && IsOwner)
        {
            Vector3 hitPos = colliderPart.ClosestPoint(enemy.gameObject.transform.position);
            DoOnHit(enemy, new HitTransform(hitPos, transform.rotation));
        }
    }
}
