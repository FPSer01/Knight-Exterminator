using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    private readonly static string DEBUG_TAG = $"[{LogTags.BLUE_COLOR}Player Manager{LogTags.END_COLOR}]";

    public static PlayerManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject knightObjectPrefab;
    [SerializeField] private GameObject mageObjectPrefab;
    [Space]
    [SerializeField] private Vector3 playerStartPositionOffset;

    private Dictionary<ulong, GameObject> spawnedPlayers = new();

    public Dictionary<ulong, GameObject> SpawnedPlayers { get => spawnedPlayers; }

    public event Action<ulong, GameObject> OnPlayerObjectAdded;
    public event Action<ulong> OnPlayerObjectRemoved;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        NetworkManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        spawnedPlayers.Remove(clientId);
        OnPlayerObjectRemoved?.Invoke(clientId);
    }

    public void SpawnPlayer(ulong clientId, Vector3 position)
    {
        if (!IsServer)
        {
            return;
        }

        Vector3 finalPosition = position + playerStartPositionOffset;

        // Если уже есть объект игрока - просто телепорт
        if (spawnedPlayers.ContainsKey(clientId) && spawnedPlayers[clientId] != null)
        {
            GameObject existingPlayer = spawnedPlayers[clientId];
            TeleportAndResetPlayer(existingPlayer, finalPosition);
            return;
        }

        // Если нет - создание и настройка нового
        switch (StartGameData.GameMode)
        {
            case Gamemode.Singleplayer:
                SetupSpawnSingleplayer(clientId, position);
                break;

            case Gamemode.Multiplayer:
                SetupSpawnMultiplayer(clientId, position);
                break;
        }
    }

    #region Setup

    private void SetupSpawnSingleplayer(ulong clientId, Vector3 position)
    {
        StanceType stance = StartGameData.Stance;
        GameObject playerObject = SpawnPlayerPrefab(stance, position);

        if (playerObject == null)
        {
            return;
        }

        NetworkObject playerNetworkObject = playerObject.GetComponent<NetworkObject>();
        playerNetworkObject.DestroyWithScene = false;

        PlayerStanceBase playerStanceController = playerObject.GetComponent<PlayerComponents>().Stance;

        if (playerStanceController != null)
        {
            playerStanceController.ExecuteSetStance(stance);
        }

        playerNetworkObject.SpawnAsPlayerObject(clientId);
        spawnedPlayers[clientId] = playerObject;

        Debug.Log($"{DEBUG_TAG} Singleplayer Spawn Complete!");
        OnPlayerObjectAdded?.Invoke(clientId, playerObject);
    }

    private void SetupSpawnMultiplayer(ulong clientId, Vector3 position)
    {
        StanceType stance = StanceType.None;

        // Берем выбранную стойку (класс)
        foreach (var clientObject in NetworkManager.ConnectedClients[clientId].OwnedObjects)
        {
            if (clientObject.TryGetComponent(out PlayerNetworkObject playerStartData))
            {
                stance = playerStartData.Stance.Value;
                break;
            }
        }

        // Спавним объект
        GameObject playerObject = SpawnPlayerPrefab(stance, position);

        if (playerObject == null)
        {
            return;
        }

        NetworkObject playerNetworkObject = playerObject.GetComponent<NetworkObject>();
        playerNetworkObject.DestroyWithScene = false;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            PlayerNetworkObject playerData = null;

            foreach (var netObj in client.OwnedObjects)
            {
                if (netObj == null)
                {
                    Debug.Log($"{DEBUG_TAG} Setup Player: net obj == null");
                    continue;
                }

                playerData = netObj.GetComponent<PlayerNetworkObject>();

                if (playerData != null)
                {
                    playerObject.name = $"Player Object [{playerData.PlayerName.Value}]";
                    break;
                }
            }
        }
        else
        {
            Debug.LogError($"{DEBUG_TAG} Client (id: {clientId}) Not Found");
            return;
        }

        playerNetworkObject.SpawnAsPlayerObject(clientId);
        spawnedPlayers[clientId] = playerObject;

        Debug.Log($"{DEBUG_TAG} Player (id: {clientId}) Setup Complete");
        OnPlayerObjectAdded?.Invoke(clientId, playerObject);
    }

    private GameObject SpawnPlayerPrefab(StanceType stance, Vector3 position)
    {
        // Рыцарь
        if ((int)stance >= 1 && (int)stance <= 4 && stance != 0)
        {
            return Instantiate(knightObjectPrefab, position, Quaternion.identity);
        }
        // Маг
        else if ((int)stance >= 5 && (int)stance <= 7 && stance != 0)
        {
            return Instantiate(mageObjectPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.Log($"{DEBUG_TAG} Setup Player: Cannot spawn Player -> StanceType not valid");
            return null;
        }
    }

    #endregion

    #region Reset

    private void TeleportAndResetPlayer(GameObject playerObj, Vector3 pos)
    {
        SetActiveAllPlayers(true);

        PlayerComponents playerComponents = playerObj.GetComponent<PlayerComponents>();

        TeleportPlayer(playerComponents.OwnerClientId, pos);
        PlayerResetAll(playerComponents);
    }

    private void PlayerResetAll(PlayerComponents playerComponents)
    {
        playerComponents.Health.ResetAll_OwnerRpc();
        playerComponents.Stance.ResetSkillState_OwnerRpc();
        playerComponents.UI.ResetUI_OwnerRpc();
        playerComponents.Stamina.ResetAll_OwnerRpc();
    }

    #endregion

    #region Set Active

    public void SetActiveAllPlayers(bool active)
    {
        if (!IsServer)
            return;

        foreach (var clientId in spawnedPlayers.Keys)
        {
            if (!spawnedPlayers.TryGetValue(clientId, out GameObject player))
            {
                continue;
            }

            NetworkObject playerNet = player.GetComponent<NetworkObject>();

            SetActivePlayer(playerNet, active);
        }
    }

    private void SetActivePlayer(NetworkObject player, bool active)
    {
        if (!IsServer) return;

        // Управляем видимостью объекта локально у всех
        player.gameObject.SetActive(active);
        Debug.Log(
            $"{DEBUG_TAG} Player GameObject {player.gameObject.name} (id: {player.NetworkObjectId}) " +
            $"is {(active ? "active" : "not active")} for this server/host"
            );

        SetActivePlayer_ClientRpc(player.NetworkObjectId, active); 
    }

    [ClientRpc]
    private void SetActivePlayer_ClientRpc(ulong networkObjectId, bool active)
    {
        if (IsServer) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            netObj.gameObject.SetActive(active);
            Debug.Log(
                $"{DEBUG_TAG} Player GameObject {netObj.gameObject.name} (id: {netObj.NetworkObjectId}) " +
                $"is {(active ? "active" : "not active")} for this client"
                );
        }
    }

    #endregion

    #region Teleport

    public void TeleportAllPlayers(Vector3 position)
    {
        TeleportAllPlayers_ServerRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void TeleportAllPlayers_ServerRpc(Vector3 position)
    {
        foreach (var playerId in spawnedPlayers.Keys)
        {
            ExecuteTeleportPlayer(playerId, position);
        }
    }

    public void TeleportPlayer(ulong clientId, Vector3 position)
    {
        TeleportPlayer_ServerRpc(clientId, position);
    }

    [Rpc(SendTo.Server)]
    private void TeleportPlayer_ServerRpc(ulong clientId, Vector3 position)
    {
        ExecuteTeleportPlayer(clientId, position);
    }

    private void ExecuteTeleportPlayer(ulong targetId, Vector3 position)
    {
        if (!spawnedPlayers.TryGetValue(targetId, out var playerObj))
            return;

        PlayerComponents components = playerObj.GetComponent<PlayerComponents>();
        PlayerMovement playerMovement = components.Movement;

        if (playerMovement != null)
        {
            playerMovement.RequestTeleport_OwnerRpc(position);
        }
    }

    #endregion

    #region Block Map

    public void BlockMapAll(bool block)
    {
        BlockMapAll_ServerRpc(block);
    }

    [Rpc(SendTo.Server)]
    private void BlockMapAll_ServerRpc(bool block) 
    {
        BlockMapAll_EveryoneRpc(block);
    }

    [Rpc(SendTo.Everyone)]
    private void BlockMapAll_EveryoneRpc(bool block)
    {
        ExecuteBlockMiniMap(block);
    }

    private void ExecuteBlockMiniMap(bool block)
    {
        PlayerUI.BlockMap = block;
    }

    #endregion

    #region Mini Map Visibility

    public void SetMiniMapVisibilityAll(bool visible)
    {
        SetMiniMapVisibilityAll_ServerRpc(visible);
    }

    [Rpc(SendTo.Server)]
    private void SetMiniMapVisibilityAll_ServerRpc(bool visible)
    {
        foreach (var playerObj in spawnedPlayers.Values)
        {
            PlayerComponents playerComponents = playerObj.GetComponent<PlayerComponents>();
            playerComponents.UI.SetMiniMapVisible(visible);
        }
    }

    #endregion

    public void DestroyAllPlayerObjects()
    {
        if (!IsServer)
            return;

        foreach (var player in spawnedPlayers.Values)
        {
            if (player != null)
            {
                player.GetComponent<NetworkObject>().Despawn();
            }
        }

        spawnedPlayers.Clear();
    }
}
