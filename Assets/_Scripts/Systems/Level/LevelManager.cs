using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelManager : NetworkBehaviour
{
    [Serializable]
    public struct NetworkObjectsCollection
    {
        public string Tag;
        [SerializeField] private List<NetworkObject> networkObjects;

        public NetworkObject GetRandomObject()
        {
            if (networkObjects.Count <= 0)
                return null;

            var randomIndex = Random.Range(0, networkObjects.Count);
            return networkObjects[randomIndex];
        }
    }

    private readonly static string DEBUG_TAG = $"[{LogTags.ORANGE_COLOR}Level Manager{LogTags.END_COLOR}]";

    public static LevelManager Instance { get; private set; }

    [Header("General")]
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private LevelMap levelMap;

    [Header("Dynamic Network Objects")]
    [SerializeField] private List<NetworkObjectsCollection> dynamicObjects;

    private readonly Dictionary<Vector2Int, RoomBehaviour> roomRegistry = new();

    private Dictionary<ulong, Vector2Int> clientCurrentRoom = new();
    private bool enteredBoss;
    private bool battleInitiated = false;

    public int Seed { private set; get; }
    

    #region Initialization

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    public override void OnNetworkSpawn()
    {
        PlayerSafeCatch.Instance.OnPlayerEnter += CatchPlayerOutsideMap;
        BossRoom.Instance.OnBossBattleBegin += () => enteredBoss = true;

        if (IsServer)
        {
            GenerateSeed();
        }
    }

    private void GenerateSeed()
    {
        int seed = Random.Range(0, int.MaxValue);
        Seed = seed;

        Debug.Log($"{DEBUG_TAG} Server seed: {seed}");
        BroadcastLevelSeedClientRpc(seed);
    }

    #endregion

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastLevelSeedClientRpc(int seed)
    {
        levelGenerator.Generate(seed);
        Debug.Log($"{DEBUG_TAG} Client seed: {seed}");
    }

    public bool TryGetRoom(Vector2Int index, out RoomBehaviour room)
    {
        return roomRegistry.TryGetValue(index, out room);
    }

    public void RegisterRoom(Vector2Int index, RoomBehaviour room)
    {
        roomRegistry[index] = room;
        room.OnPlayerEnterRoom += Room_OnPlayerEnterRoom;
    }

    private void Room_OnPlayerEnterRoom(ulong clientId, Vector2Int roomIndex)
    {
        if (clientCurrentRoom.TryGetValue(clientId, out var currentRoomIndex))
        {
            if (currentRoomIndex == roomIndex)
                return;
        }

        clientCurrentRoom[clientId] = roomIndex;

        levelMap.DiscoverRoom(roomIndex);
        DiscoverRoom_ServerRpc(roomIndex);

        Debug.Log($"{DEBUG_TAG} Current Room: [{roomIndex.x}, {roomIndex.y}] for Client Id: {clientId}");
    }

    public void ForceSetCurrentRoom(Vector2Int roomIndex)
    {
        Room_OnPlayerEnterRoom(NetworkManager.LocalClientId, roomIndex);
    }

    private void CatchPlayerOutsideMap(PlayerMovement player)
    {
        if (!clientCurrentRoom.TryGetValue(player.OwnerClientId, out var roomIndex))
            return;

        if (!TryGetRoom(roomIndex, out var room))
            return;

        Vector3 position = enteredBoss ?
            BossRoom.Instance.GetPlayerTeleportPoint() :
            room.TeleportPoint.position;

        player.RequestTeleport_OwnerRpc(position);
    }

    #region Room: Start Battle

    [Rpc(SendTo.Server)]
    public void StartRoomBattle_ServerRpc(Vector2Int roomIndex, bool checkForEntered = true)
    { 
        StartRoomBattle_EveryoneRpc(roomIndex, checkForEntered);
    }

    [Rpc(SendTo.Everyone)]
    private void StartRoomBattle_EveryoneRpc(Vector2Int roomIndex, bool checkForEnteredState = true)
    {
        if (battleInitiated)
            return;

        battleInitiated = true;

        if (roomRegistry.TryGetValue(roomIndex, out var room))
        {
            room.StartBattle_Local(checkForEntered: checkForEnteredState);
        }
    }

    #endregion

    #region Room: End Battle

    [Rpc(SendTo.Server)]
    public void EndRoomBattle_ServerRpc(Vector2Int roomIndex, bool checkForCleared = true)
    {
        EndRoomBattle_EveryoneRpc(roomIndex, checkForCleared);
    }

    [Rpc(SendTo.Everyone)]
    private void EndRoomBattle_EveryoneRpc(Vector2Int roomIndex, bool checkForClearedState = true)
    {
        battleInitiated = false;

        if (roomRegistry.TryGetValue(roomIndex, out var room))
        {
            room.EndBattle_Local(checkForCleared: checkForClearedState);
        }
    }

    #endregion

    #region Map: Discover Room

    [Rpc(SendTo.Server)]
    public void DiscoverRoom_ServerRpc(Vector2Int roomIndex)
    {
        DiscoverRoom_EveryoneRpc(roomIndex);
    }

    [Rpc(SendTo.Everyone)]
    private void DiscoverRoom_EveryoneRpc(Vector2Int roomIndex)
    {
        levelMap.DiscoverRoom(roomIndex);
    }

    #endregion

    #region Room: Teleport Players On Battle

    public void TeleportPlayers(ulong senderId, Vector3 senderPosition, Vector2Int roomIndex, bool teleportDead = false, bool teleportToSender = false)
    {
        TeleportPlayers_ServerRpc(senderId, senderPosition, roomIndex, teleportDead, teleportToSender);
    }

    [Rpc(SendTo.Server)]
    private void TeleportPlayers_ServerRpc(ulong senderId, Vector3 senderPosition, Vector2Int roomIndex, bool teleportDead = false, bool teleportToSender = false)
    {
        TeleportPlayers_EveryoneRpc(senderId, senderPosition, roomIndex, teleportDead, teleportToSender);
    }

    [Rpc(SendTo.Everyone)]
    private void TeleportPlayers_EveryoneRpc(ulong senderId, Vector3 senderPosition, Vector2Int roomIndex, bool teleportDead = false, bool teleportToSender = false)
    {
        if (!roomRegistry.TryGetValue(roomIndex, out var room))
        {
            Debug.LogError($"{DEBUG_TAG} TeleportPlayers: Room Not Found");
            return;
        }

        Vector3 teleportPosition = senderPosition;

        if (!teleportToSender)
        {
            Vector3 center = room.transform.position;
            teleportPosition = Vector3.Lerp(senderPosition, center, 0.05f);
        }

        var localClient = NetworkManager.Singleton.LocalClient;

        if (localClient.ClientId == senderId)
        {
            Debug.Log($"{DEBUG_TAG} Ignoring Teleport Request: Sender Id == Local Client Id");
            return;
        }

        var playerObj = localClient.PlayerObject;

        if (!playerObj.TryGetComponent(out PlayerComponents components))
        {
            Debug.Log($"{DEBUG_TAG} Ignoring Teleport Request: No Player Components Found");
            return;
        }

        if (components.Health.IsDead && !teleportDead)
        {
            Debug.Log($"{DEBUG_TAG} Ignoring Teleport Request: Player Dead");
            return;
        }

        if (clientCurrentRoom[localClient.ClientId] == roomIndex)
        {
            Debug.Log($"{DEBUG_TAG} Ignoring Teleport Request: Player Already in Room [{roomIndex.x}, {roomIndex.y}]");
            return;
        }

        components.Movement.RequestTeleport_OwnerRpc(teleportPosition);
        Debug.Log($"{DEBUG_TAG} Request Teleport Successful!");
    }

    #endregion

    #region Dynamic Network Objects Spawn

    /// <summary>
    /// Заспавнить динамический сетевой объект (Network Object). Выполняется только сервером
    /// </summary>
    public NetworkObject SpawnDynamicObject(string tag, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!IsServer)
            return null;

        var foundCollection = dynamicObjects.Find(collection => collection.Tag == tag);
        NetworkObject networkObjectPrefab = foundCollection.GetRandomObject();

        if (networkObjectPrefab == null)
        {
            Debug.LogError($"{DEBUG_TAG} При попытке заспавнить динамический сетевой объект из коллекции \"{tag}\" произошла ошибка: префаб объекта отсутствует");
            return null;
        }

        var spawnedObject = NetworkManager.SpawnManager.InstantiateAndSpawn(
            networkObjectPrefab,
            ownerClientId: 0,
            destroyWithScene: true,
            isPlayerObject: false,
            position: position,
            rotation: rotation
            );

        if (parent != null)
            spawnedObject.TrySetParent(parent, true);

        return spawnedObject;
    }

    #endregion
}
