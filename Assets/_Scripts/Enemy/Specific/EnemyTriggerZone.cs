using System;
using UnityEngine;

public class EnemyTriggerZone : MonoBehaviour
{
    [SerializeField] private Collider triggerCollider;

    public event Action OnPlayerTriggerEnter;

    private void Start()
    {
        triggerCollider.isTrigger = true;
    }

    public void SetCollider(bool active)
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
