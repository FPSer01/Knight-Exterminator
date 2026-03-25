using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerClientController : NetworkBehaviour
{
    [SerializeField] private List<Behaviour> playerBehaviorComponents;
    [Space]
    [SerializeField] private List<GameObject> playerObjects;
    [Space]
    [SerializeField] private List<GameObject> hostObjects;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            EnableComponents(true);
            EnableObjects(true);
        }
        else
        {
            EnableComponents(false);
            EnableObjects(false);
        }

        hostObjects.ForEach(obj => obj.SetActive(IsServer));
    }

    private void EnableComponents(bool enable)
    {
        playerBehaviorComponents.ForEach(comp => comp.enabled = enable);
    }

    private void EnableObjects(bool enable)
    {
        playerObjects.ForEach(comp => comp.SetActive(enable));
    }
}
