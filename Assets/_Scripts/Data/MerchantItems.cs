using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "MerchantItems", menuName = "Data/Merchant Items")]
public class MerchantItems : ScriptableObject
{
    [SerializeField] private UpgradeItemDatabase database;
    [SerializeField] private List<ItemRarityChance> itemPools;

    public UpgradeItemDatabase Database { get => database; }

    /// <summary>
    /// Получить предметь согласно его шансу выпадения в настройках пула предметов
    /// </summary>
    /// <returns></returns>
    public UpgradeItem GetItem()
    {
        float randomValue = Random.value;

        float topCeilChance = 0f;
        float bottomCeilChance = 0f;

        for (int i = 0; i < itemPools.Count; i++)
        {
            if (itemPools[i].Chance == 0f)
                continue;

            bottomCeilChance = topCeilChance;
            topCeilChance += itemPools[i].Chance;

            if (bottomCeilChance < randomValue && randomValue <= topCeilChance)
            {
                return database.GetRandomItemFromRarity(itemPools[i].ItemRarity);
            }
        }

        return null;
    }

    [Serializable]
    public struct ItemRarityChance
    {
        public ItemRarity ItemRarity;
        [Range(0f, 1f)] public float Chance;
    }
}
