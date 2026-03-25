using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "ObjectSpawnChance", menuName = "Data/Object Spawn Chance")]
public class ObjectSpawnChance : ScriptableObject
{
    [SerializeField] private List<ItemPoolChance> items;

    public GameObject GetRandomItem()
    {
        float randomValue = Random.value;

        float topCeilChance = 0f;
        float bottomCeilChance = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            bottomCeilChance = topCeilChance;
            topCeilChance += items[i].Chance;

            if (bottomCeilChance < randomValue && randomValue <= topCeilChance)
                return items[i].GetItem();
        }

        return null;
    }

    public int GetObjectIndex(GameObject obj)
    {
        var itemPool = items.Find(pool => obj == pool.GetItem());
        return items.IndexOf(itemPool);
    }

    public GameObject GetObject(int index)
    {
        var itemPool = items[index];
        return itemPool.GetItem();
    }

    [Serializable]
    public struct ItemPoolChance
    {
        [SerializeField] private GameObject item;
        [Range(0f, 1f)] public float Chance;

        public GameObject GetItem()
        {
            return item;
        }
    }
}
