using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyTouchAttackCollider : NetworkBehaviour
{
    [SerializeField] private Collider attackCollider;
    [SerializeField] private float touchCooldown = 0.2f;
    [SerializeField] private bool onStay = false;
    private bool canDealDamage = true;

    public event Action<PlayerHealth, HitTransform> OnHit;

    private void Start()
    {
        SetCollider(false);
    }

    public void SetCollider(bool active)
    {
        ExecuteSetCollider(active);
        SetCollider_NotOwnerRpc(active);
    }

    private void ExecuteSetCollider(bool active)
    {
        attackCollider.enabled = active;
    }

    [Rpc(SendTo.NotOwner)]
    private void SetCollider_NotOwnerRpc(bool active)
    {
        ExecuteSetCollider(active);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onStay)
            return;

        if (other.TryGetComponent(out PlayerHealth player) && canDealDamage)
        {
            Vector3 hitPos = attackCollider.ClosestPoint(player.transform.position);
            OnHit?.Invoke(player, new HitTransform(hitPos, transform.rotation));

            StartCoroutine(StartCooldown());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!onStay)
            return;

        if (other.TryGetComponent(out PlayerHealth player) && canDealDamage)
        {
            Vector3 hitPos = attackCollider.ClosestPoint(player.transform.position);
            OnHit?.Invoke(player, new HitTransform(hitPos, transform.rotation));

            StartCoroutine(StartCooldown());
        }
    }

    private IEnumerator StartCooldown()
    {
        canDealDamage = false;
        yield return new WaitForSeconds(touchCooldown);
        canDealDamage = true;
    }
}
