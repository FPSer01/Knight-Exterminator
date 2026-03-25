using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "SpawnItemChance", menuName = "Data/Spawn Item Chance")]
public class SpawnItemChance : ScriptableObject
{
    [SerializeField] private List<ItemPoolChance> items;

    public UpgradeItem GetItem()
    {
        float randomValue = Random.value;

        float topCeilChance = 0f;
        float bottomCeilChance = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Chance == 0f)
                continue;

            bottomCeilChance = topCeilChance;
            topCeilChance += items[i].Chance;

            if (bottomCeilChance < randomValue && randomValue <= topCeilChance)
                return items[i].GetRandomItem();
        }

        return null;
    }

    [Serializable]
    public struct ItemPoolChance
    {
        [SerializeField] private UpgradeItemContainer itemPool;
        [Range(0f, 1f)] public float Chance;

        public UpgradeItem GetRandomItem()
        {
            return itemPool.GetRandomItem();
        }
    }
}
