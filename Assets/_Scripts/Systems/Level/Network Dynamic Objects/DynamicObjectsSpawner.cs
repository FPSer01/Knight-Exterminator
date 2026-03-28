using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class DynamicObjectsSpawner : MonoBehaviour
{
    [Serializable]
    public class DynamicObjectSpawnSettings
    {
        [SerializeField] private string tag;
        [SerializeField] private int spawnAmount;
        [SerializeField] private List<Transform> possiblePoints;

        private List<Transform> availablePoints;

        public void Spawn(Transform parent)
        {
            if (string.IsNullOrWhiteSpace(tag) || spawnAmount <= 0 || this.possiblePoints.Count <= 0)
                return;

            availablePoints = possiblePoints.ToList();

            for (int i = 0; i < spawnAmount; i++)
            {
                int randomPoint = Random.Range(0, availablePoints.Count);
                Transform spawnPoint = availablePoints[randomPoint];
                availablePoints.RemoveAt(randomPoint);

                LevelManager.Instance.SpawnDynamicObject(tag, spawnPoint.position, spawnPoint.rotation, parent);
            }
        }
    }

    [SerializeField] private List<DynamicObjectSpawnSettings> spawnSettings;

    private void Start()
    {
        SpawnObjects();
    }

    private void SpawnObjects()
    {
        foreach (var settings in spawnSettings)
        {
            settings.Spawn(transform);
        }
    }
}
