using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerManager : NetworkBehaviour
{
    private readonly static string DEBUG_TAG = $"[{LogTags.BLUE_COLOR}Player Manager{LogTags.END_COLOR}]";

    public static PlayerManager Instance { get; private set; }

    [SerializeField] private GameObject playerObjectPrefab;
    [Space]
    [SerializeField] private Vector3 playerStartPositionOffset;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private StanceType startStance;

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

        if (spawnedPlayers.ContainsKey(clientId) && spawnedPlayers[clientId] != null)
        {
            GameObject existingPlayer = spawnedPlayers[clientId];
            TeleportAndResetPlayer(existingPlayer, finalPosition);
            return;
        }

        GameObject newPlayer = Instantiate(playerObjectPrefab, finalPosition, Quaternion.identity);

        NetworkObject netObj = newPlayer.GetComponent<NetworkObject>();
        netObj.DestroyWithScene = false;
        SetupPlayer(newPlayer, clientId);

        netObj.SpawnAsPlayerObject(clientId);
        spawnedPlayers[clientId] = newPlayer;

        OnPlayerObjectAdded?.Invoke(clientId, newPlayer);
    }

    #region Setup

    private void SetupPlayer(GameObject playerObject, ulong clientId)
    {
        switch (StartGameData.GameMode)
        {
            case Gamemode.Singleplayer:
                SetupForSingleplayer(playerObject);
                break;

            case Gamemode.Multiplayer:
                SetupForMultiplayer(playerObject, clientId);
                break;
        }
    }

    private void SetupForMultiplayer(GameObject playerObject, ulong clientId)
    {
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

            Debug.Log($"{DEBUG_TAG} Player (id: {clientId}) Setup Complete");
        }
        else
        {
            Debug.LogError($"{DEBUG_TAG} Client (id: {clientId}) Not Found");
        }       
    }

    private void SetupForSingleplayer(GameObject playerObject)
    {
        StanceType selectedStance = debugMode ? startStance : StartGameData.Stance;
        PlayerStance playerStanceController = playerObject.GetComponentInChildren<PlayerStance>();

        if (playerStanceController != null)
        {
            playerStanceController.ExecuteSetStance(selectedStance);
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

    #region 

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
