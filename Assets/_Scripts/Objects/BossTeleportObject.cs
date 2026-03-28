using System;
using Unity.Netcode;
using UnityEngine;

public class BossTeleportObject : InteractableObject
{
    public override void Interact(GameObject sender)
    {
        base.Interact(sender);

        TryEnterBossRoom_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void TryEnterBossRoom_ServerRpc()
    {
        TryEnterBossBattle();
        Debug.Log("Try Enter Boss Room...");
    }

    private void TryEnterBossBattle()
    {
        if (BossRoom.Instance != null)
        {
            BossRoom.Instance.BeginBossBattle();
        }
        else
        {
            Debug.LogError("No Boss Room in Scene!");
        }
    }
}
