using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerAttackMultiCollider : NetworkBehaviour
{
    public enum MultiColliderRegime
    {
        Sequence,
        Random
    }

    [Header("Parts")]
    [SerializeField] private List<PlayerAttackMultiColliderPart> parts;
    [SerializeField] private ParticleSystem attackVFX;
    
    [Header("Settings")]
    [SerializeField] private MultiColliderRegime regime;
    [SerializeField] private float timeInterval;

    private Coroutine loopCoroutine;

    public event Action<EntityHealth, HitTransform> OnHit;
    private void DoOnHit(EntityHealth enemy, HitTransform hitPos) => OnHit?.Invoke(enemy, hitPos);

    public override void OnNetworkSpawn()
    {
        parts.ForEach(part => { 
            if (part != null)
            {
                part.OnHit += Part_OnHit;
                part.SetColliderActive(false);
            }
        });
    }

    private void Part_OnHit(EntityHealth enemy, HitTransform hitPos)
    {
        DoOnHit(enemy, hitPos);
    }

    public void SetColliders(bool active)
    {
        parts.ForEach(part => {
            if (part != null)
            {
                part.SetColliderActive(active);
            }
        });
    }

    public void CheckForHit()
    {
        if (!IsOwner)
            return;

        SetColliders(false);
        switch (regime) 
        {
            case MultiColliderRegime.Sequence:
                StartCoroutine(DoCollidersSequence(false));
                break;
            case MultiColliderRegime.Random:
                StartCoroutine(DoCollidersRandom(false));
                break;
        }
    }

    public void CheckForHitLoop(bool startLoop)
    {
        if (!IsOwner)
            return;

        SetColliders(false);

        if (!startLoop && loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
            StopAttackVFX();

            return;
        }

        switch (regime)
        {
            case MultiColliderRegime.Sequence:
                loopCoroutine = StartCoroutine(DoCollidersSequence(true));
                break;
            case MultiColliderRegime.Random:
                loopCoroutine = StartCoroutine(DoCollidersRandom(true));
                break;
        }
    }

    private IEnumerator DoCollidersSequence(bool loop)
    {
        do
        {
            PlayAttackVFX();

            foreach (var part in parts)
            {
                part.SetColliderActive(true);
                yield return new WaitForFixedUpdate();
                part.SetColliderActive(false);

                yield return new WaitForSeconds(timeInterval);
            }
        }
        while (loop);
    }

    private IEnumerator DoCollidersRandom(bool loop)
    {
        do
        {
            PlayAttackVFX();

            List<PlayerAttackMultiColliderPart> avalableParts = parts.ToList();

            // Размешиваем лист по Фишеру–Йетсу
            for (int i = avalableParts.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (avalableParts[i], avalableParts[j]) = (avalableParts[j], avalableParts[i]);
            }

            foreach (var part in avalableParts)
            {
                part.SetColliderActive(true);
                yield return new WaitForFixedUpdate();
                part.SetColliderActive(false);

                yield return new WaitForSeconds(timeInterval);
            }
        }
        while (loop);
    }

    private void PlayAttackVFX()
    {
        ExecutePlayAttackVFX();
        PlayAttackVFX_NotOwnerRpc();
    }

    private void ExecutePlayAttackVFX()
    {
        attackVFX.Play();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayAttackVFX_NotOwnerRpc()
    {
        ExecutePlayAttackVFX();
    }

    private void StopAttackVFX()
    {
        ExecuteStopAttackVFX();
        StopAttackVFX_NotOwnerRpc();
    }

    private void ExecuteStopAttackVFX()
    {
        attackVFX.Stop();
    }

    [Rpc(SendTo.NotOwner)]
    private void StopAttackVFX_NotOwnerRpc()
    {
        ExecuteStopAttackVFX();
    }
}
