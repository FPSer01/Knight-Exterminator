using System;
using Unity.Netcode;
using UnityEngine;

public class MimicObject : InteractableObject
{
    [Header("Mimic Settings")]
    [SerializeField] private int interactableLayer;
    [SerializeField] private int enemyLayer;

    /// <summary>
    /// Событие пробуждения мимика. Подается id отправителя (того кто его потревожил)
    /// </summary>
    public event Action<ulong> OnWakeUp;
    [HideInInspector] public bool IsWakenUp = false;

    protected override void Start()
    {
        base.Start();

        gameObject.layer = interactableLayer;
    }

    public override void Interact(GameObject sender)
    {
        PlayerComponents components = sender.GetComponentInParent<PlayerComponents>();

        TryAwakenMimic_ServerRpc(components.OwnerClientId);
    }

    [Rpc(SendTo.Server)]
    private void TryAwakenMimic_ServerRpc(ulong senderId)
    {
        TryAwakenMimic_EveryoneRpc(senderId);
    }

    [Rpc(SendTo.Everyone)]
    private void TryAwakenMimic_EveryoneRpc(ulong senderId)
    {
        if (IsWakenUp)
            return;

        IsWakenUp = true;

        if (IsServer)
        {
            OnWakeUp?.Invoke(senderId);
        }

        gameObject.layer = enemyLayer;
        HighlightObject(false);
        enabled = false;
    }
}
