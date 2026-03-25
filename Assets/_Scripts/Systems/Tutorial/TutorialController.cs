using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TutorialController : NetworkBehaviour
{
    [SerializeField] private Transform playerSpawnPosition;

    private void Start()
    {
        PlayerUI.BlockMap = true;
        StartGameData.Stance = StanceType.Attack;

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

        PlayerManager.Instance.SetMiniMapVisibilityAll(false);

        PlayerComponents playerComponents = null;

        foreach (var netobj in NetworkManager.LocalClient.OwnedObjects)
        {
            playerComponents = netobj.GetComponent<PlayerComponents>();

            if (playerComponents != null) break;
        }

        playerComponents.UI.LevelNameUI.ShowLevelLabel("Обучение", 1.5f, 3f);
    }

    public void UnlockMap()
    {
        PlayerUI.BlockMap = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        UnlockMap();
    }
}
