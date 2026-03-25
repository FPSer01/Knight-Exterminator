using System.Collections.Generic;
using UnityEngine;

public class BeeSwarmBehaviour : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private GameObject beeEnemyPrefab;
    [SerializeField] private int minSpawnAmount;
    [SerializeField] private int maxSpawnAmount;
    private int currentSpawnAmount;
    [Space]
    [SerializeField] private float spawnRange;

    [Header("Components")]
    [SerializeField] private BasicEnemyDrops swarmDrops;
    [SerializeField] private BeeSwarmHealth swarmHealth;

    private List<GameObject> bees;

    private void Start()
    {
        SpawnSwarm();
    }

    private void SpawnSwarm()
    {
        currentSpawnAmount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);
        SpawnBees(currentSpawnAmount);
    }

    private void SpawnBees(int amount)
    {
        bees = new();

        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPoint = Random.insideUnitSphere;
            spawnPoint = Vector3.Scale(spawnPoint, new Vector3(1, 0, 1) * spawnRange);

            GameObject beeObject = Instantiate(beeEnemyPrefab, transform.position + spawnPoint, Quaternion.identity);
            bees.Add(beeObject);

            EnemyHealth beeHealth = beeObject.GetComponent<EnemyHealth>();
            beeHealth.OnDeath += () => {
                bees.Remove(beeObject);
                Bee_OnDeath();
            };

            //BeeBehaviour beeBehaviour = beeObject.GetComponent<BeeBehaviour>();


        }
    }

    private void Bee_OnDeath()
    {
        currentSpawnAmount--;

        if (bees.Count == 1)
        {
            swarmDrops.SetDropPoint(bees[0].transform);
        }

        Debug.Log($"Swarm entities alive: {currentSpawnAmount}");

        if (currentSpawnAmount <= 0)
        {
            swarmHealth.ForceTriggerDeath();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRange);
    }
}
