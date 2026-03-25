using System.Collections.Generic; using Unity.Netcode; using UnityEngine; using Random = UnityEngine.Random;  public class LevelManager : NetworkBehaviour {     public static LevelManager Instance { get; private set; }      [SerializeField] private LevelGenerator levelGenerator;     [SerializeField] private LevelMap levelMap;      private readonly Dictionary<Vector2Int, RoomBehaviour> roomRegistry = new();      private Vector2Int clientCurrentRoomIndex;     private bool enteredBoss;     private bool battleInitiated = false;      public int Seed { private set; get; }     public Vector2Int ClientCurrentRoom { get => clientCurrentRoomIndex; }      #region Initialization      private void Awake()     {         if (Instance == null)             Instance = this;         else             Destroy(this);     }      public override void OnNetworkSpawn()     {         if (IsLocalPlayer)         {             PlayerSafeCatch.Instance.OnPlayerEnter += CatchPlayerOutsideMap;             BossRoom.Instance.OnBossBattleBegin += () => enteredBoss = true;         }          if (!IsServer) return;          GenerateSeed();     }      private void GenerateSeed()     {         int seed = Random.Range(0, int.MaxValue);         Seed = seed;          Debug.Log($"[LevelNetworkManager] Server seed: {seed}");         BroadcastLevelSeedClientRpc(seed);     }      #endregion      [Rpc(SendTo.ClientsAndHost)]     private void BroadcastLevelSeedClientRpc(int seed)     {         levelGenerator.Generate(seed);         Debug.Log($"[LevelNetworkManager] Client seed: {seed}");     }      public bool TryGetRoom(Vector2Int index, out RoomBehaviour room)     {         return roomRegistry.TryGetValue(index, out room);     }      public void RegisterRoom(Vector2Int index, RoomBehaviour room)     {         roomRegistry[index] = room;         room.OnPlayerEnterRoom += Room_OnPlayerEnterRoom;     }      private void Room_OnPlayerEnterRoom(Vector2Int roomIndex)     {         clientCurrentRoomIndex = roomIndex;          levelMap.DiscoverRoom(roomIndex);         DiscoverRoom_ServerRpc(roomIndex);     }      public void ForceSetCurrentRoom(Vector2Int roomIndex)
    {
        Room_OnPlayerEnterRoom(roomIndex);
    }      private void CatchPlayerOutsideMap(PlayerMovement player)     {         if (!TryGetRoom(clientCurrentRoomIndex, out var room))             return;          Vector3 position = enteredBoss ?             BossRoom.Instance.GetPlayerTeleportPoint() :             room.TeleportPoint.position;          player.RequestTeleport_OwnerRpc(position);     }

    #region Room: Start Battle 
    [Rpc(SendTo.Server)]     public void StartRoomBattle_ServerRpc(Vector2Int roomIndex)     {          StartRoomBattle_EveryoneRpc(roomIndex);     }      [Rpc(SendTo.Everyone)]     private void StartRoomBattle_EveryoneRpc(Vector2Int roomIndex)     {         if (battleInitiated)             return;          battleInitiated = true;          if (roomRegistry.TryGetValue(roomIndex, out var room))         {             room.StartBattle();         }     }

    #endregion 
    #region Room: End Battle 
    [Rpc(SendTo.Server)]     public void EndRoomBattle_ServerRpc(Vector2Int roomIndex)     {         EndRoomBattle_EveryoneRpc(roomIndex);     }      [Rpc(SendTo.Everyone)]     private void EndRoomBattle_EveryoneRpc(Vector2Int roomIndex)     {         battleInitiated = false;          if (roomRegistry.TryGetValue(roomIndex, out var room))         {             room.EndBattle();         }     }

    #endregion 
    #region Map: Discover Room

    [Rpc(SendTo.Server)]     public void DiscoverRoom_ServerRpc(Vector2Int roomIndex)     {         DiscoverRoom_EveryoneRpc(roomIndex);     }      [Rpc(SendTo.Everyone)]     private void DiscoverRoom_EveryoneRpc(Vector2Int roomIndex)     {         levelMap.DiscoverRoom(roomIndex);     }

    #endregion 
    #region Room: Teleport Players On Battle 
    public void TeleportPlayers(ulong senderId, Vector3 senderPosition, Vector2Int roomIndex)
    {
        TeleportPlayers_ServerRpc(senderId, senderPosition, roomIndex);
    }

    [Rpc(SendTo.Server)]
    private void TeleportPlayers_ServerRpc(ulong senderId, Vector3 senderPosition, Vector2Int roomIndex)
    {
        if (!roomRegistry.TryGetValue(roomIndex, out var room))
        {
            Debug.LogError("TeleportPlayers: Room Not Found");
            return;
        }

        Vector3 center = room.transform.position;
        Vector3 teleportPosition = Vector3.Lerp(senderPosition, center, 0.05f);

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == senderId)
                continue;

            PlayerManager.Instance.TeleportPlayer(clientId, teleportPosition);
        }
    }

    #endregion } 