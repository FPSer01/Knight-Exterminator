using DG.Tweening;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class EntityStatusCollider : NetworkBehaviour
{
    [Header("General")]
    [SerializeField] private Collider col;
    [Space]
    [SerializeField] private AttackDamageType attackDamage;
    [Space]
    [SerializeField] private float tickTime;
    [SerializeField] private bool createHitEffect;

    [Header("Animation")]
    [SerializeField] private bool destroy;
    [SerializeField] private float destroyDelay;
    [Space]
    [SerializeField] private bool startAnimation;
    [SerializeField] private float inTime;
    [SerializeField] private float outTime;

    [Header("Movement")]
    [SerializeField] private bool useMovement;
    [SerializeField] private bool randomDirection;
    [SerializeField] private Vector3 moveDirection;
    [SerializeField] private float moveDistance;
    [SerializeField] private float moveTime;

    public event Action<PlayerHealth, HitTransform> OnHit;

    private Coroutine cycleCoroutine;

    public override void OnNetworkSpawn()
    {
        col.isTrigger = true;
        OnHit += EntityStatusCollider_OnHit;

        if (startAnimation)
        {
            Vector3 originalScale = transform.localScale;
            transform.DOScale(originalScale, inTime);
        }

        if (useMovement)
        {
            Vector3 endPos;

            if (randomDirection)
            {
                endPos = transform.position + Random.onUnitSphere.normalized * moveDistance;
            }
            else
            {
                endPos = transform.position + moveDirection.normalized * moveDistance;
            }

            transform.DOMove(endPos, moveTime);
        }

        cycleCoroutine = StartCoroutine(StartCycle());

        if (destroy && IsServer)
            Invoke(nameof(DestroySequence), destroyDelay);
    }

    private void EntityStatusCollider_OnHit(PlayerHealth player, HitTransform hit)
    {
        player.TakeDamage(attackDamage, null);

        if (createHitEffect)
            player.CreateHitEffect(hit);
    }

    private IEnumerator StartCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(tickTime);

            col.enabled = true;
            yield return new WaitForFixedUpdate();
            col.enabled = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out PlayerHealth player))
        {
            Vector3 hitPos = col.ClosestPoint(player.transform.position);
            OnHit?.Invoke(player, new HitTransform(hitPos, transform.rotation));
        }
    }

    private void DestroySequence()
    {
        StopCoroutine(cycleCoroutine);
        transform.DOScale(Vector3.zero, outTime).OnComplete(() => 
        {
            NetworkObject.Despawn();
        });
    }
}
