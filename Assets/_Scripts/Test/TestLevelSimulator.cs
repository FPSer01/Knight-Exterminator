using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TestLevelSimulator : NetworkBehaviour
{
    [SerializeField] private float waitTime = 4f;
    [SerializeField] private Transform playerSpawnPosition;

    public override void OnNetworkSpawn()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        StartCoroutine(FakeGenerate());
    }

    private IEnumerator FakeGenerate()
    {
        yield return new WaitForSecondsRealtime(waitTime);

        StartCoroutine(WaitForPlayerSpawner());
    }

    private IEnumerator WaitForPlayerSpawner()
    {
        while (PlayerManager.Instance == null)
        {
            yield return new WaitForEndOfFrame();
        }

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            PlayerManager.Instance.SpawnPlayer(clientId, playerSpawnPosition.position);
        }

        ShowLevelName_AllRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ShowLevelName_AllRpc()
    {
        PlayerComponents playerComponents = null;

        foreach (var netobj in NetworkManager.LocalClient.OwnedObjects)
        {
            playerComponents = netobj.GetComponent<PlayerComponents>();

            if (playerComponents != null) break;
        }

        playerComponents.UI.LevelNameUI.ShowLevelLabel("Test Level", 1.5f, 3f);
    }

}
