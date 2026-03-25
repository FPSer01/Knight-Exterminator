using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttackCollider : NetworkBehaviour
{
    [SerializeField] private Collider attackCollider;
    [SerializeField] private ParticleSystem slashEffect;
    [SerializeField] private ParticleSystem fireEffect;
    [SerializeField] private ParticleSystem electricEffect;
    private ParticleSystem currentEffect;

    public event Action<EntityHealth, HitTransform> OnEnemyHit;

    [SerializeField] private bool triggerOnEnter = false;

    private void Awake()
    {
        currentEffect = slashEffect;
    }

    private void Start()
    {
        SetCollider(false);
    }

    public void SetElementalAttackState(ElementalAttackState state)
    {
        ExecuteSetElementalAttackState(state);
        SetElementalAttackState_Rpc(state);
    }

    [Rpc(SendTo.NotOwner)]
    private void SetElementalAttackState_Rpc(ElementalAttackState state)
    {
        ExecuteSetElementalAttackState(state);
    }

    private void ExecuteSetElementalAttackState(ElementalAttackState state)
    {
        switch (state)
        {
            case ElementalAttackState.None:
                currentEffect = slashEffect;
                break;

            case ElementalAttackState.Fire:
                currentEffect = fireEffect;
                break;

            case ElementalAttackState.Electric:
                currentEffect = electricEffect;
                break;
        }
    }

    public void SlashEffectActive(bool active, bool clear = false)
    {
        if (currentEffect == null)
            return;

        if (active)
            currentEffect.Play();
        else
        {
            currentEffect.Stop();

            if (clear)
                currentEffect.Clear();
        }

    }

    public void SetTriggerOnEnter(bool onEnter)
    {
        triggerOnEnter = onEnter;
    }

    public void SetCollider(bool active)
    {
        attackCollider.enabled = active;
    }

    public void FixedUpdateAttackCheck()
    {
        if (!IsOwner)
            return;

        SetCollider(false);
        StartCoroutine(CheckColliders());
    }

    private IEnumerator CheckColliders()
    {
        PlayCurrentEffect();
        SetCollider(true);

        yield return new WaitForFixedUpdate();

        SetCollider(false);
    }

    private void PlayCurrentEffect()
    {
        ExecutePlayCurrentEffect();
        PlayCurrentEffect_NotOwnerRpc();
    }

    private void ExecutePlayCurrentEffect()
    {
        currentEffect.Play();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayCurrentEffect_NotOwnerRpc()
    {
        ExecutePlayCurrentEffect();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter || !IsOwner)
            return;

        TryDealDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (triggerOnEnter || !IsOwner)
            return;

        TryDealDamage(other);
    }

    private void TryDealDamage(Collider enemyCollider)
    {
        if (enemyCollider.TryGetComponent(out EntityHealth enemy) && IsOwner)
        {
            Vector3 hitPos = attackCollider.ClosestPoint(enemy.gameObject.transform.position);
            OnEnemyHit?.Invoke(enemy, new HitTransform(hitPos, transform.rotation));
        }
    }
}
