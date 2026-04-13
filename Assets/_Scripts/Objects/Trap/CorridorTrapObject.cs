using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CorridorTrapObject : NetworkBehaviour
{
    [SerializeField] private List<TrapAttackCollider> attackColliders;
    [SerializeField] private AttackDamageType damage;
    [Space]
    [SerializeField] private List<Transform> leftSpikes;
    [SerializeField] private Vector3 leftStartPos;
    [SerializeField] private Vector3 leftEndPos;
    [Space]
    [SerializeField] private List<Transform> rightSpikes;
    [SerializeField] private Vector3 rightStartPos;
    [SerializeField] private Vector3 rightEndPos;
    [Space]
    [SerializeField] private float showTime;
    [SerializeField] private float damageDelay;
    [SerializeField] private float retractTime;
    [SerializeField] private float cooldownTime;
    private bool canTrigger = true;

    public override void OnNetworkSpawn()
    {
        leftSpikes.ForEach(spike => spike.transform.localPosition = leftStartPos);
        rightSpikes.ForEach(spike => spike.transform.localPosition = rightStartPos);

        attackColliders.ForEach(c => c.OnHit += AttackCollider_OnHit);
    }

    private void AttackCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(damage, null);
        player.CreateHitEffect(hitPos);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement player) && canTrigger)
        {
            ActivateTrap_ServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void ActivateTrap_ServerRpc()
    {
        ActivateTrap_EveryoneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ActivateTrap_EveryoneRpc()
    {
        StartCoroutine(TrapSequence());
    }

    private IEnumerator TrapSequence()
    {
        canTrigger = false;

        leftSpikes.ForEach(spike => spike.DOLocalMove(leftEndPos, showTime).SetEase(Ease.OutExpo));
        rightSpikes.ForEach(spike => spike.DOLocalMove(rightEndPos, showTime).SetEase(Ease.OutExpo));

        yield return new WaitForSeconds(damageDelay);

        attackColliders.ForEach(c => c.FixedUpdateAttackCheck());

        yield return new WaitForSeconds(showTime - damageDelay);

        leftSpikes.ForEach(spike => spike.DOLocalMove(leftStartPos, retractTime));
        rightSpikes.ForEach(spike => spike.DOLocalMove(rightStartPos, retractTime));

        yield return new WaitForSeconds(cooldownTime);

        canTrigger = true;
    }
}
