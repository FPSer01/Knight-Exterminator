using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class ItemGenerator : NetworkBehaviour
{
    public static ItemGenerator Instance { private set; get; }

    [Header("Upgrade Items")]
    [SerializeField] private GameObject itemObjectPrefab;
    [SerializeField] private UpgradeItemDatabase database;

    [Header("Other Items")]
    [SerializeField] private ObjectSpawnChance objectItems;

    [Header("General Settings")]
    [SerializeField] private float spawnForce = 5f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    public void SpawnItem(UpgradeItem item, Vector3 position)
    {
        int itemIndex = database.GetItemIndex(item);

        SpawnItem_ServerRpc(itemIndex, position);
    }

    [Rpc(SendTo.Server)]
    private void SpawnItem_ServerRpc(int itemIndex, Vector3 position)
    {
        GameObject instancedItem = Instantiate(itemObjectPrefab, position, Quaternion.identity);
        ItemObject itemObject = instancedItem.GetComponent<ItemObject>();
        NetworkObject itemNetworkObject = instancedItem.GetComponent<NetworkObject>();
        itemNetworkObject.Spawn(true);

        NetworkItem networkItem = new() { ItemDatabaseIndex = itemIndex };
        itemObject.NetworkItem.Value = networkItem;

        Vector3 direction = Random.onUnitSphere;
        direction.y = Mathf.Clamp01(direction.y);

        Rigidbody rb = instancedItem.GetComponent<Rigidbody>();
        rb.AddForce(direction * spawnForce, ForceMode.VelocityChange);

        Debug.Log($"Item [{LogTags.BLUE_COLOR}{database.GetItem(itemIndex).ItemName}{LogTags.END_COLOR}] Spawned: {LogTags.GREEN_COLOR}Success{LogTags.END_COLOR}");
    }

    public void SpawnObjectItem(GameObject objectItemPrefab, Vector3 position)
    {
        var index = objectItems.GetObjectIndex(objectItemPrefab);
        SpawnObjectItem_ServerRpc(index, position);
    }

    [Rpc(SendTo.Server)]
    private void SpawnObjectItem_ServerRpc(int objectIndex, Vector3 position)
    {
        GameObject objectPrefab = objectItems.GetObject(objectIndex);
        GameObject instancedItem = Instantiate(objectPrefab, position, Quaternion.identity);
        NetworkObject netObj = instancedItem.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        Vector3 direction = Random.onUnitSphere;
        direction.y = Mathf.Clamp01(direction.y);

        Rigidbody rb = instancedItem.GetComponent<Rigidbody>();
        rb.AddForce(direction * spawnForce, ForceMode.VelocityChange);

        Debug.Log($"Object Item [{LogTags.BLUE_COLOR}{instancedItem.name}{LogTags.END_COLOR}] Spawned: {LogTags.GREEN_COLOR}Success{LogTags.END_COLOR}");
    }
}
