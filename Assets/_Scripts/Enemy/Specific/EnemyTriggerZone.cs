using System;
using Unity.Netcode;
using UnityEngine;

public class EnemyTriggerZone : NetworkBehaviour
{
    [SerializeField] private Collider triggerCollider;

    public event Action OnPlayerTriggerEnter;

    private void Start()
    {
        triggerCollider.isTrigger = true;
    }

    public void SetCollider(bool active)
    {
        SetCollider_EveryoneRpc(active);
    }

    [Rpc(SendTo.Everyone)]
    private void SetCollider_EveryoneRpc(bool active)
    {
        triggerCollider.enabled = active;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement player))
        {
            OnPlayerTriggerEnter?.Invoke();
        }
    }
}
