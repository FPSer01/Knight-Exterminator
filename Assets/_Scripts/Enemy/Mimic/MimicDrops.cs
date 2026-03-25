using UnityEngine;

public class MimicDrops : BaseEnemyDrops
{
    [Header("Mimic Drops Settings")]
    [SerializeField] private Vector3 size;
    [SerializeField] private int minItemsToDrop;
    [SerializeField] private int maxItemsToDrop;
    [Space]
    [SerializeField] private SpawnItemChance dropsSettings;

    protected override void GiveDrop()
    {
        base.GiveDrop();

        int itemsToDrop = Random.Range(minItemsToDrop, maxItemsToDrop + 1);

        for (int i = 0; i < itemsToDrop; i++)
        {
            UpgradeItem dropItem = dropsSettings.GetItem();

            if (dropItem != null)
            {
                var point = GetRandomPoint(dropPoint.position, size);
                ItemGenerator.Instance.SpawnItem(dropItem, point);
            }
        }
    }

    private Vector3 GetRandomPoint(Vector3 center, Vector3 bounds)
    {
        Vector3 result = center;

        float xRand = Random.Range(-bounds.x / 2, bounds.x / 2);
        float yRand = Random.Range(-bounds.y / 2, bounds.y / 2);
        float zRand = Random.Range(-bounds.z / 2, bounds.z / 2);

        result += new Vector3(xRand, yRand, zRand);

        return result;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one
        );

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(dropPoint.localPosition, size);
    }
}
