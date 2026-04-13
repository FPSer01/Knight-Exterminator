using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyObjectSpawner : NetworkBehaviour
{
    [Serializable]
    public class DistanceSpawnSettings : SpawnSettings
    {
        [Header("Spawn With Distance")]
        public float MinDistanceToSpawn;
    }

    [Serializable]
    public class TimeframeSpawnSettings : SpawnSettings
    {
        [Header("Timeframe")]
        public float FirstSpawnDelay;
        public float Timeframe;
    }

    [Serializable]
    public class OnHitSpawnSettings : SpawnSettings
    {
        [Header("Spawn On Damage Taken")]
        public float SpawnCooldown;
    }

    [Serializable]
    public abstract class SpawnSettings
    {
        [Header("General")]
        public List<Transform> SpawnPoints;
        public List<GameObject> Prefabs;
        [Space]
        public bool StartSpawnAfterInitiate = true;
        [Space]
        public bool UseRaycast;
        public float RaycastDistance;
        public LayerMask RaycastMask;

        public Transform GetRandomSpawnPoint()
        {
            int randomIndex = Random.Range(0, SpawnPoints.Count);
            return SpawnPoints[randomIndex];
        }

        public GameObject GetRandomPrefab()
        {
            int randomIndex = Random.Range(0, Prefabs.Count);
            return Prefabs[randomIndex];
        }
    }

    [SerializeField] private DistanceSpawnSettings distanceSettings;
    private Coroutine distanceSpawnCoroutine;
    [Space]
    [SerializeField] private TimeframeSpawnSettings timeframeSettings;
    private Coroutine timeframeSpawnCoroutine;
    [Space]
    [SerializeField] private EnemyComponents components;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        StartSpawnWithDistance(distanceSettings.StartSpawnAfterInitiate);
        StartSpawnWithTimeframe(timeframeSettings.StartSpawnAfterInitiate);
    }

    private NetworkObject TrySpawnNetworkPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab.TryGetComponent(out NetworkObject netObj))
        {
            return NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(
                netObj,
                ownerClientId: 0,
                destroyWithScene: true,
                position: position,
                rotation: rotation
                );
        }
        else
        {
            Debug.LogError("EnemyObjectSpawner: Нет NetworkObject у префаба");
            return null;
        }
    }

    #region Distance Spawn

    public void StartSpawnWithDistance(bool enable)
    {
        if (distanceSettings == null)
            return;

        if (distanceSpawnCoroutine != null)
        {
            StopCoroutine(distanceSpawnCoroutine);
            distanceSpawnCoroutine = null;
        }

        if (enable)
            distanceSpawnCoroutine = StartCoroutine(SpawnWithDistance(distanceSettings));
    }

    private IEnumerator SpawnWithDistance(DistanceSpawnSettings settings)
    {
        Transform lastSpawned = null;

        while (true)
        {
            var point = settings.GetRandomSpawnPoint();
            var prefab = settings.GetRandomPrefab();

            point.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

            bool spawnFromRaycast = false;

            if (settings.UseRaycast)
            {
                if (Physics.Raycast(point.position, point.forward, out RaycastHit hit, settings.RaycastDistance, settings.RaycastMask))
                {
                    position = hit.point;
                    rotation = Quaternion.LookRotation(hit.normal);

                    spawnFromRaycast = true;
                }
            }

            if (settings.UseRaycast && !spawnFromRaycast)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            if (lastSpawned == null)
            {
                lastSpawned = TrySpawnNetworkPrefab(prefab, position, rotation).transform;
            }
            else if (Vector3.Distance(lastSpawned.position, position) >= settings.MinDistanceToSpawn)
            {
                lastSpawned = TrySpawnNetworkPrefab(prefab, position, rotation).transform;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    #endregion

    #region Timeframe Spawn

    public void StartSpawnWithTimeframe(bool enable)
    {
        if (timeframeSettings == null)
            return;

        if (timeframeSpawnCoroutine != null)
        {
            StopCoroutine(timeframeSpawnCoroutine);
            timeframeSpawnCoroutine = null;
        }

        if (enable)
            timeframeSpawnCoroutine = StartCoroutine(SpawnWithTimeframe(timeframeSettings));
    } 

    private IEnumerator SpawnWithTimeframe(TimeframeSpawnSettings settings)
    {
        yield return new WaitForSeconds(settings.FirstSpawnDelay);

        float timeframe = Math.Max(settings.Timeframe, Time.fixedDeltaTime);

        while (true)
        {
            var point = settings.GetRandomSpawnPoint();
            var prefab = settings.GetRandomPrefab();

            point.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

            bool spawnFromRaycast = false;

            if (settings.UseRaycast)
            {
                if (Physics.Raycast(point.position, point.forward, out RaycastHit hit, settings.RaycastDistance, settings.RaycastMask))
                {
                    position = hit.point;
                    rotation = Quaternion.LookRotation(hit.normal);

                    spawnFromRaycast = true;
                }
            }

            if (settings.UseRaycast && !spawnFromRaycast)
            {
                yield return new WaitForSeconds(timeframe);
                continue;
            }

            TrySpawnNetworkPrefab(prefab, position, rotation);

            yield return new WaitForSeconds(timeframe);
        }
    }

    #endregion

    #region Spawn On Hit (Damage Taken)

    /*private void SubsribeToOnHit(SpawnSettings settings)
    {
        components.Health.OnDamageTaken += (damageTaken) =>
        {
            SpawnOnHit(settings);
        };
    }

    private void SpawnOnHit(SpawnSettings settings)
    {
        if (settings.SpawnType != SpawnType.OnHit)
            return;

        var point = settings.GetRandomSpawnPoint();
        var prefab = settings.GetRandomPrefab();

        point.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        if (settings.UseRaycast)
        {
            if (Physics.Raycast(point.position, point.forward, out RaycastHit hit, settings.RaycastDistance, settings.RaycastMask))
            {
                position = hit.point;
                rotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                return;
            }
        }

        TrySpawnNetworkPrefab(prefab, position, rotation);
    }*/

    #endregion
}
