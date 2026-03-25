using Unity.Netcode;
using UnityEngine;

public class BossDrops : BaseEnemyDrops
{
    [Header("Boss Drops Settings")]
    [SerializeField] private BossItemDrops dropsSettings;

    protected override void GiveDrop()
    {
        base.GiveDrop();

        if (dropsSettings && dropPoint != null)
        {
            UpgradeItem dropItem = dropsSettings.GetItem();

            if (dropItem != null)
                ItemGenerator.Instance.SpawnItem(dropItem, dropPoint.position);
        }
    }
}
