using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class EntityStatusCollider : MonoBehaviour
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

    public event Action<PlayerHealth, HitTransform> OnHit;

    private Coroutine cycleCoroutine;

    private void Start()
    {
        col.isTrigger = true;
        OnHit += EntityStatusCollider_OnHit;

        if (startAnimation)
        {
            Vector3 originalScale = transform.localScale;
            transform.DOScale(originalScale, inTime);
        }

        cycleCoroutine = StartCoroutine(StartCycle());

        if (destroy)
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
            Destroy(gameObject);
        });
    }
}
