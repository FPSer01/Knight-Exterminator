using System;
using Unity.Netcode;
using UnityEngine;

public class BasicEnemyDrops : BaseEnemyDrops
{
    [Header("Basic Enemy Drops Settings")]
    [SerializeField] private SpawnItemChance dropsSettings;
    [SerializeField] private ObjectSpawnChance objectSpawnSettings;

    protected override void GiveDrop()
    {
        base.GiveDrop();

        if (dropsSettings != null)
        {
            UpgradeItem dropItem = dropsSettings.GetItem();

            if (dropItem != null)
            {
                ItemGenerator.Instance.SpawnItem(dropItem, dropPoint.position);
            }
        }

        if (objectSpawnSettings != null)
        {
            var objectItem = objectSpawnSettings.GetRandomItem();

            if (objectItem != null)
            {
                ItemGenerator.Instance.SpawnObjectItem(objectItem, dropPoint.position);
            }
        }
    }
}
