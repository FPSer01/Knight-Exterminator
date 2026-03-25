using System;
using System.Collections;
using UnityEngine;

public class MimicAttackCollider : MonoBehaviour
{
    [SerializeField] private Collider attackCollider;

    public event Action<PlayerHealth, HitTransform> OnHit;

    private void Start()
    {
        attackCollider.isTrigger = true;
        attackCollider.enabled = false;
    }

    public void StartAttack()
    {
        attackCollider.enabled = true;
    }

/*    private IEnumerator AttackTiming()
    {
        attackCollider.enabled = false;
        yield return new WaitForSeconds(timeBetweenHits);
        attackCollider.enabled = true;
    }*/

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out PlayerHealth player))
        {
            Vector3 hitPos = attackCollider.ClosestPoint(player.transform.position);
            OnHit?.Invoke(player, new HitTransform(hitPos, transform.rotation));

            //StartCoroutine(AttackTiming());
        }
    }
}
