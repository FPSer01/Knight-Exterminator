using System;
using System.Globalization;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkObject : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new(
        new FixedString32Bytes("Player"), 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public NetworkVariable<bool> IsReady = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public NetworkVariable<StanceType> Stance = new(
        StanceType.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        if (StartGameData.GameMode == Gamemode.Singleplayer)
            return;

        if (IsServer && GameNetworkManager.Instance != null)
        {
            if (GameNetworkManager.Instance.TryGetStoredPlayerName(OwnerClientId, out string storedName))
            {
                PlayerName.Value = storedName;
            }
        }

        PlayerName.OnValueChanged += PlayerName_OnValueChanged;
        gameObject.name = $"Player [{PlayerName.Value}] Data";

        DontDestroyOnLoad(gameObject);
    }

    private void PlayerName_OnValueChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        gameObject.name = newValue.ToString();
    }

    #region Lobby RPC

    [ServerRpc]
    public void ToggleReady_ServerRpc()
    {
        IsReady.Value = !IsReady.Value;
    }

    [ServerRpc]
    public void SetPlayerStance_ServerRpc(StanceType stanceType)
    {
        Stance.Value = stanceType;
    }

    #endregion
}
