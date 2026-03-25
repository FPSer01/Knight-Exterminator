using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BossItemDrops", menuName = "Data/Boss Item Drops")]
public class BossItemDrops : ScriptableObject
{
    [SerializeField] private List<UpgradeItem> bossItems;

    public UpgradeItem GetItem()
    {
        int randomIndex = Random.Range(0, bossItems.Count);

        return bossItems[randomIndex];
    }
}
