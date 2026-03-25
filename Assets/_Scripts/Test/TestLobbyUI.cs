using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestLobbyUI : MonoBehaviour
{
    [SerializeField] private Button startHost;
    [SerializeField] private Button startClient;

    private void Start()
    {
        startHost.onClick.AddListener(StartHost);
        startClient.onClick.AddListener(StartClient);
    }

    private void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        gameObject.SetActive(false);
    }

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        gameObject.SetActive(false);
    }
}
