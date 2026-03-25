using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapAttackCollider : MonoBehaviour
{
    [SerializeField] private Collider attackCollider;

    public event Action<PlayerHealth, HitTransform> OnHit;

    private void Start()
    {
        SetCollider(false);
    }

    public void SetCollider(bool active)
    {
        attackCollider.enabled = active;
    }

    public void FixedUpdateAttackCheck()
    {
        SetCollider(false);
        StartCoroutine(CheckColliders());
    }

    private IEnumerator CheckColliders()
    {
        SetCollider(true);

        yield return new WaitForFixedUpdate();

        SetCollider(false);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider enemyCollider)
    {
        if (enemyCollider.TryGetComponent(out PlayerHealth player))
        {
            Vector3 hitPos = attackCollider.ClosestPoint(player.gameObject.transform.position);
            OnHit?.Invoke(player, new HitTransform(hitPos, transform.rotation));
        }
    }
}
