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
        if (BossRoom.Instance != null)
        {
            BossRoom.Instance.BeginBossBattle();
        }
        else
        {
            Debug.LogError("Комнаты босса нет!");
        }
    }
}
