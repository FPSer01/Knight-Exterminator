using Unity.Netcode;
using UnityEngine;

public class ChestObject : InteractableObject
{
    [Header("Chest Drops")]
    [SerializeField] private Transform dropCenter;
    [SerializeField] private Vector3 size;
    [SerializeField] private int minItemsToDrop;
    [SerializeField] private int maxItemsToDrop;
    [SerializeField] private SpawnItemChance dropsSettings;
    [Space]
    [SerializeField] private GameObject openVFXPrefab;

    public override void Interact(GameObject sender)
    {
        base.Interact(sender);

        int itemsToDrop = Random.Range(minItemsToDrop, maxItemsToDrop + 1);

        for (int i = 0; i < itemsToDrop; i++)
        {
            UpgradeItem dropItem = dropsSettings.GetItem();

            if (dropItem != null)
            {
                var point = GetRandomPoint(dropCenter.position, size);
                ItemGenerator.Instance.SpawnItem(dropItem, point);
            }
        }

        RequestChestDespawn_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestChestDespawn_ServerRpc()
    {
        RequestChestDespawn_EveryoneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void RequestChestDespawn_EveryoneRpc()
    {
        Instantiate(openVFXPrefab, dropCenter.position, Quaternion.identity);
        gameObject.SetActive(false);

        if (IsServer)
            NetworkObject.Despawn(false);
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
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(dropCenter.localPosition, size);
    }
}
