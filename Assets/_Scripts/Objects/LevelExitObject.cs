using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class LevelExitObject : InteractableObject
{
    [Header("Level Exit Options")]
    [SerializeField] private GameLevels level;
    [SerializeField] private bool isEndOfGame = false;
    [SerializeField] private ParticleSystem portalVFX;

    public override void HighlightObject(bool highlight) { }

    public override void Interact(GameObject sender)
    {
        base.Interact(sender);

        if (isEndOfGame)
        {
            SetGameWinUI_ServerRpc();
        }
        else
        {
            LoadManager.Instance.LoadLevel_ServerRpc((int)level);
        }
    }

    [Rpc(SendTo.Server)]
    private void SetGameWinUI_ServerRpc()
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            {
                continue;
            }

            foreach (var netObj in client.OwnedObjects)
            {
                if (netObj.TryGetComponent(out PlayerComponents playerComponents))
                {
                    playerComponents.UI.SetGameWinUI_OwnerRpc();
                    break;
                }
            }
        }
    }

    public void SetActive(bool active)
    {
        SetActive_ServerRpc(active);
    }

    [Rpc(SendTo.Server)]    
    private void SetActive_ServerRpc(bool active)
    {
        SetActive_EveryoneRpc(active);
    }

    [Rpc(SendTo.Everyone)]
    private void SetActive_EveryoneRpc(bool active)
    {
        gameObject.SetActive(active);

        if (active)
        {
            portalVFX.Play();
        }
        else
        {
            portalVFX.Stop();
        }
    }
}
