using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerUsernameUI : NetworkBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text usernameText;
    private PlayerNetworkObject boundPlayer;

    public override void OnNetworkSpawn()
    {
        CheckOwnership();

        if (boundPlayer == null && StartGameData.GameMode == Gamemode.Multiplayer)
        {
            FindAndBindLocalData();
        }
    }

    public override void OnNetworkDespawn()
    {
        CheckOwnership();

        UnboundPlayer();
    }

    public override void OnGainedOwnership()
    {
        CheckOwnership();
    }

    public override void OnLostOwnership()
    {
        CheckOwnership();
    }

    private void CheckOwnership()
    {
        usernameText.gameObject.SetActive(!IsOwner);
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

    public void BoundToPlayer(PlayerNetworkObject player)
    {
        if (boundPlayer != null) UnboundPlayer();

        boundPlayer = player;
        boundPlayer.PlayerName.OnValueChanged += OnPlayerNameChanged;

        ExecuteSetUsername(boundPlayer.PlayerName.Value.ToString());
    }

    public void UnboundPlayer()
    {
        boundPlayer.PlayerName.OnValueChanged -= OnPlayerNameChanged;
        boundPlayer = null;
    }

    private void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        ExecuteSetUsername(newValue.ToString());
    }

    private void ExecuteSetUsername(string username)
    {
        usernameText.text = username;
    }

    public void EnableVisibility(bool enable)
    {
        canvasGroup.alpha = enable ? 1 : 0;
    }
}
