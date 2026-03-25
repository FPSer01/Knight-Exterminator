using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySummonAttack : BaseEnemyAttack
{
    [Header("Summon Settings")]
    [SerializeField] private List<GameObject> summonPrefabs;
    [SerializeField] private float summonDistance;
    [SerializeField] private LayerMask summonMask;
    [Space]
    [SerializeField] private bool usePoints;
    [SerializeField] private List<Transform> summonPoints;

    [Header("SFX Settings")]
    [SerializeField] private bool useAttackSFX = true;
    [SerializeField] private string customSFXTag;
    [Range(0f, 1f)][SerializeField] private float attackSFXVolume = 1;

    [Header("Components")]
    [SerializeField] private EnemyComponents components;

    private EnemySFXController sfxController => components.SFXController;

    private List<GameObject> summonedEntities = new();

    public event Action OnEntitySpawned;
    public event Action OnEntityDespawned;
    public event Action OnAllEntitiesDespawned;

    public int SummonsCount => summonedEntities.Count;

    private void DoOnEntitySpawned() => OnEntitySpawned?.Invoke();
    private void DoOnEntityDespawned() => OnEntityDespawned?.Invoke();
    private void DoOnAllEntitiesDespawned() => OnAllEntitiesDespawned?.Invoke();

    private void PlayAttackSFX()
    {
        if (useAttackSFX)
        {
            sfxController.PlayAttackSFX(attackSFXVolume);
        }
        else if (!useAttackSFX)
        {
            sfxController.PlayCustomSFX(customSFXTag);
        }
    }

    public override void SummonAttack(int summonAmount)
    {
        if (!canAttack)
            return;

        PlayAttackSFX();

        if (usePoints)
        {
            TrySummonFromPoints(summonAmount);
        }
        else
        {
            TrySummonFromProviders(summonAmount);
        }

        base.SummonAttack(summonAmount);
    }

    private void TrySummonFromPoints(int summonAmount)
    {
        if (summonAmount >= summonPoints.Count)
        {
            SummonFromPoints(summonPoints);
        }
        else
        {
            List<Transform> pickedPoints = new();

            while (summonAmount > pickedPoints.Count)
            {
                int index = Random.Range(0, summonPoints.Count);
                var point = summonPoints[index];

                if (!pickedPoints.Contains(point))
                {
                    pickedPoints.Add(point);
                }
            }

            SummonFromPoints(pickedPoints);
        }
    }

    private void SummonFromPoints(List<Transform> points)
    {
        foreach (var point in points)
        {
            var summonedEntity = Instantiate(GetRandomSummon(), point.position, Quaternion.identity);
            summonedEntities.Add(summonedEntity);

            var health = summonedEntity.GetComponent<EntityHealth>();
            health.OnDeath += () => RemoveFromSummonsList(summonedEntity);

            NetworkObject netObj = summonedEntity.GetComponent<NetworkObject>();
            netObj.Spawn();

            DoOnEntitySpawned();
        }
    }

    private void TrySummonFromProviders(int summonAmount)
    {
        Collider[] objects = new Collider[256];

        if (Physics.OverlapSphereNonAlloc(transform.position, summonDistance, objects, summonMask) > 0)
        {
            int amountSpawned = 0;

            foreach (var obj in objects)
            {
                if (obj.TryGetComponent(out ISummonSpawnProvider summonProvider))
                {
                    var summonedEntity = summonProvider.Summon(GetRandomSummon());

                    if (summonedEntity != null)
                    {
                        var health = summonedEntity.GetComponent<EntityHealth>();
                        health.OnDeath += () => RemoveFromSummonsList(summonedEntity);

                        summonedEntities.Add(summonedEntity);
                        amountSpawned++;

                        DoOnEntitySpawned();
                    }
                }

                if (amountSpawned >= summonAmount)
                {
                    break;
                }
            }
        }
    }

    private void CheckForNoSummons()
    {
        if (summonedEntities.Count == 0)
        {
            DoOnAllEntitiesDespawned();
        }
    }

    private GameObject GetRandomSummon()
    {
        int index = Random.Range(0, summonPrefabs.Count);
        return summonPrefabs[index];
    }

    private void RemoveFromSummonsList(GameObject summonedEntity)
    {
        summonedEntities.Remove(summonedEntity);
        DoOnEntityDespawned();
        CheckForNoSummons();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, summonDistance);
    }
}
