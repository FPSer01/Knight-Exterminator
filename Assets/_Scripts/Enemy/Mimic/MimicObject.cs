using System;
using Unity.Netcode;
using UnityEngine;

public class MimicObject : InteractableObject
{
    [Header("Mimic Settings")]
    [SerializeField] private int interactableLayer;
    [SerializeField] private int enemyLayer;

    public event Action OnWakeUp;
    [HideInInspector] public bool IsWakenUp = false;

    protected override void Start()
    {
        base.Start();

        gameObject.layer = interactableLayer;
    }

    public override void Interact(GameObject sender)
    {
        TryAwakenMimic_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void TryAwakenMimic_ServerRpc()
    {
        TryAwakenMimic_EveryoneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void TryAwakenMimic_EveryoneRpc()
    {
        if (IsWakenUp)
            return;

        IsWakenUp = true;

        if (IsServer)
        {
            OnWakeUp?.Invoke();
        }

        gameObject.layer = enemyLayer;
        HighlightObject(false);
        enabled = false;
    }
}
