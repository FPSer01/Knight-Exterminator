using System.Collections;
using UnityEngine;

public class EnemyTouchAttackCollider : EnemyAttackCollider
{
    [Header("Touch Attack Collider")]
    [SerializeField] private float touchCooldown = 0.2f;
    [SerializeField] private bool onStay = false;
    private bool canDealDamage = true;

    public override void OnNetworkSpawn()
    {
        SetCollider(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onStay)
            return;

        if (other.TryGetComponent(out PlayerHealth player) && canDealDamage)
        {
            Vector3 hitPos = attackCollider.ClosestPoint(player.transform.position);
            DoOnHit(player, new HitTransform(hitPos, transform.rotation));

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
            DoOnHit(player, new HitTransform(hitPos, transform.rotation));

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
