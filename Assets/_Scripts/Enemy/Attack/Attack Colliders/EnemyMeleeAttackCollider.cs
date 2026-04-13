using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EnemyMeleeAttackCollider : EnemyAttackCollider
{
    [Header("Melee Attack Collider")]
    [SerializeField] private ParticleSystem slashVFX;

    public override void OnNetworkSpawn()
    {
        SetCollider(false);
    }

    public void StartAttackCheck()
    {
        StartAttackCheck_EveryoneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void StartAttackCheck_EveryoneRpc()
    {
        ExecuteSetCollider(false);
        StartCoroutine(CheckAttack());
    }

    private IEnumerator CheckAttack()
    {
        slashVFX.Play();
        ExecuteSetCollider(true);

        yield return new WaitForFixedUpdate();

        ExecuteSetCollider(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out PlayerHealth player))
        {
            Vector3 hitPos = attackCollider.ClosestPoint(player.transform.position);
            DoOnHit(player, new HitTransform(hitPos, transform.rotation));
        }
    }
}
