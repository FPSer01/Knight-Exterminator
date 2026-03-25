using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameLevels level;
    [SerializeField] private int enemiesAmount;
    [SerializeField] private List<Transform> spawnPoints;

    private List<Transform> availablePoints;
    private int enemiesAlive;

    public event Action OnAllEnemiesKilled;

    public void SpawnEnemies()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        availablePoints = spawnPoints.ToList();
        int count = Mathf.Min(enemiesAmount, availablePoints.Count);

        for (int i = 0; i < count; i++)
        {
            int randomPoint = Random.Range(0, availablePoints.Count);
            Vector3 spawnPoint = availablePoints[randomPoint].position;
            availablePoints.RemoveAt(randomPoint);

            GameObject enemyObject = EnemyManager.Instance.SpawnEnemy(level, spawnPoint);

            if (enemyObject.TryGetComponent<EntityHealth>(out var enemyHealth))
                enemyHealth.OnDeath += UpdateEnemiesAliveCount;
        }

        enemiesAlive = count;
    }

    private void UpdateEnemiesAliveCount()
    {
        enemiesAlive--;
        if (enemiesAlive <= 0)
        {
            enemiesAlive = 0;
            OnAllEnemiesKilled?.Invoke();
        }
    }
}