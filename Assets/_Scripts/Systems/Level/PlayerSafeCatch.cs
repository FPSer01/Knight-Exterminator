using System;
using UnityEngine;

public class PlayerSafeCatch : MonoBehaviour
{
    public static PlayerSafeCatch Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    public event Action<PlayerMovement> OnPlayerEnter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement player))
        {
            OnPlayerEnter?.Invoke(player);
        }
    }
}
