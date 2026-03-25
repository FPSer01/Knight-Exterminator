using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialEnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private List<GameObject> doors;
        
    private bool spawned;

    private void Start()
    {
        LockDoors(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (spawned)
            return;

        spawned = true;
        LockDoors(true);

        GameObject enemyObj = Instantiate(enemyPrefab, enemySpawnPoint.position, Quaternion.identity);
        EnemyComponents enemy = enemyObj.GetComponent<EnemyComponents>();
        enemy.Health.OnDeath += Enemy_OnDeath;

        enemy.NetworkObject.Spawn();
    }

    private void LockDoors(bool lockDoors)
    {
        doors.ForEach(d => d.SetActive(lockDoors));
    }

    private void Enemy_OnDeath()
    {
        LockDoors(false);
    }
}
