using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade Item Database", menuName = "Data/Upgrade Item Database")]
public class UpgradeItemDatabase : ScriptableObject
{
    [Header("Items")]
    [SerializeField] private List<UpgradeItem> itemsDatabase = new();

    [Header("Categories")]
    [SerializeField] private List<UpgradeItem> commonItems = new();
    [SerializeField] private List<UpgradeItem> rareItems = new();
    [SerializeField] private List<UpgradeItem> mythicalItems = new();
    [SerializeField] private List<UpgradeItem> bossItems = new();

    private void SetupDatabase()
    {
        commonItems = new();
        rareItems = new();
        mythicalItems = new();
        bossItems = new();

        itemsDatabase.ForEach(item =>
        {
            switch (item.Rarity)
            {
                case ItemRarity.Common:
                    commonItems.Add(item);
                    break;

                case ItemRarity.Rare:
                    rareItems.Add(item);
                    break;

                case ItemRarity.Mythical:
                    mythicalItems.Add(item);
                    break;

                case ItemRarity.Boss:
                    bossItems.Add(item);
                    break;
            }
        });

        Debug.Log("Database Setup Complete!");
    }

    public UpgradeItem GetItem(int index)
    {
        return itemsDatabase[index];
    }

    public int GetItemIndex(UpgradeItem item)
    {
        return itemsDatabase.IndexOf(item);
    }

    public UpgradeItem GetRandomItemFromRarity(ItemRarity rarity)
    {
        int index = 0;

        switch (rarity)
        {
            case ItemRarity.Common:
                index = Random.Range(0, commonItems.Count);
                return commonItems[index];

            case ItemRarity.Rare:
                index = Random.Range(0, rareItems.Count);
                return rareItems[index];

            case ItemRarity.Mythical:
                index = Random.Range(0, mythicalItems.Count);
                return mythicalItems[index];

            case ItemRarity.Boss:
                index = Random.Range(0, bossItems.Count);
                return bossItems[index];

            default: return null;
        }
    }
}
