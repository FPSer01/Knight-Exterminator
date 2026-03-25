using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneWayTrap : MonoBehaviour
{
    [SerializeField] private List<TrapAttackCollider> attackColliders;
    [SerializeField] private AttackDamageType damage;
    [Space]
    [SerializeField] private List<Transform> spikes;
    [SerializeField] private Vector3 startPos;
    [SerializeField] private Vector3 endPos;
    [Space]
    [SerializeField] private float showTime;
    [SerializeField] private float damageDelay;
    [SerializeField] private float retractTime;
    [SerializeField] private float cooldownTime;
    private bool canTrigger = true;

    private void Start()
    {
        spikes.ForEach(spike => spike.transform.localPosition = startPos);

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
            StartCoroutine(TrapSequence());
        }
    }

    private IEnumerator TrapSequence()
    {
        canTrigger = false;

        spikes.ForEach(spike => spike.DOLocalMove(endPos, showTime).SetEase(Ease.OutExpo));

        yield return new WaitForSeconds(damageDelay);

        attackColliders.ForEach(c => c.FixedUpdateAttackCheck());

        yield return new WaitForSeconds(showTime - damageDelay);

        spikes.ForEach(spike => spike.DOLocalMove(startPos, retractTime));

        yield return new WaitForSeconds(cooldownTime);

        canTrigger = true;
    }
}
