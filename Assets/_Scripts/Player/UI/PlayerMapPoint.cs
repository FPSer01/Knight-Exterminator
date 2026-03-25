using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMapPoint : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private NetworkVariable<Color> playerColor = new(
        Color.green,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        playerColor.OnValueChanged += OnColorChanged;

        if (IsServer)
        {
            Color assignedColor = PlayerColorManager.Instance.AssignPlayerColor(OwnerClientId);
            playerColor.Value = assignedColor;
        }
        else
        {
            ApplyColor(playerColor.Value);
        }

        if (IsOwner)
        {
            spriteRenderer.sortingOrder++;
        }
    }

    public override void OnNetworkDespawn()
    {
        playerColor.OnValueChanged -= OnColorChanged;

        if (IsServer)
        {
            PlayerColorManager.Instance.ReleaseColor(OwnerClientId);
        }
    }

    private void OnColorChanged(Color previousValue, Color newValue)
    {
        ApplyColor(newValue);
    }

    private void ApplyColor(Color color)
    {
        spriteRenderer.color = color;
    }
}
