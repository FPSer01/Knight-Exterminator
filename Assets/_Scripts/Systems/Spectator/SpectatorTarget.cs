using System;
using Unity.Netcode;
using UnityEngine;

public class SpectatorTarget : NetworkBehaviour
{
    [SerializeField] private PlayerComponents playerComponents;
    private PlayerNetworkObject boundPlayer;

    public PlayerComponents Components { get => playerComponents; }
    public PlayerNetworkObject BoundPlayer { get => boundPlayer; }

    public string TargetName => boundPlayer != null ? boundPlayer.PlayerName.Value.ToString() : "Player";

    public bool IsDead => Components.Health.IsDead;

    public override void OnNetworkSpawn()
    {
        if (boundPlayer == null && StartGameData.GameMode == Gamemode.Multiplayer)
        {
            FindAndBindLocalData();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (StartGameData.GameMode == Gamemode.Multiplayer)
        {
            UnboundPlayer();
        }
    }

    private void FindAndBindLocalData()
    {
        var allDataObjects = FindObjectsByType<PlayerNetworkObject>(FindObjectsSortMode.None);

        foreach (var dataObj in allDataObjects)
        {
            if (dataObj.OwnerClientId == OwnerClientId)
            {
                BoundToPlayer(dataObj);
                break;
            }
        }
    }

    private void BoundToPlayer(PlayerNetworkObject player)
    {
        if (boundPlayer != null)
        {
            UnboundPlayer();
        }

        boundPlayer = player;
    }

    private void UnboundPlayer()
    {
        boundPlayer = null;
    }
}