using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class VersionController : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;

    private void Awake()
    {
        versionText.text = $"Version: {Application.version}";
    }

    private void Start()
    {
        StartCoroutine(WaitForNetworkManager());
    }

    private IEnumerator WaitForNetworkManager()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);

        NetworkManager.Singleton.NetworkConfig.ProtocolVersion = GetProtocolVersion();
    }

    public static ushort GetProtocolVersion()
    {
        string version = Application.version;
        int hash = version.GetHashCode();
        return (ushort)(hash ^ (hash >> 16));
    }
}
