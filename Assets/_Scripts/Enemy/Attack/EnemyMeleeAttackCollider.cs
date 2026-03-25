using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class EnemyMeleeAttackCollider : NetworkBehaviour
{
    [SerializeField] private Collider attackCollider;
    [SerializeField] private ParticleSystem slashVFX;

    public event Action<PlayerHealth, HitTransform> OnHit;

    private void Start()
    {
        attackCollider.isTrigger = true;
        SetCollider(false);
    }

    public void StartAttackCheck()
    {
        StartAttackCheck_EveryoneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void StartAttackCheck_EveryoneRpc()
    {
        SetCollider(false);
        StartCoroutine(CheckAttack());
    }

    private IEnumerator CheckAttack()
    {
        slashVFX.Play();
        SetCollider(true);

        yield return new WaitForFixedUpdate();

        SetCollider(false);
    }

    public void SetCollider(bool active)
    {
        attackCollider.enabled = active;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out PlayerHealth player))
        {
            Vector3 hitPos = attackCollider.ClosestPoint(player.transform.position);
            OnHit?.Invoke(player, new HitTransform(hitPos, transform.rotation));
        }
    }
}
