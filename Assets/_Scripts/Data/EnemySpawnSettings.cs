using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "EnemySpawnSettings", menuName = "Data/Enemy Spawn Settings")]
public class EnemySpawnSettings : ScriptableObject
{
    [SerializeField] private List<EnemySpawn> enemies;

    public GameObject GetEnemyPrefab()
    {
        float randomValue = Random.value;

        float topCeilChance = 0f;
        float bottomCeilChance = 0f;

        for (int i = 0; i < enemies.Count; i++)
        {
            bottomCeilChance = topCeilChance;
            topCeilChance += enemies[i].Chance;

            if (bottomCeilChance < randomValue && randomValue <= topCeilChance)
                return enemies[i].EnemyPrefab;
        }

        return null;
    }

    [Serializable]
    public struct EnemySpawn
    {
        public GameObject EnemyPrefab;
        [Range(0f, 1f)] public float Chance;
    }
}
