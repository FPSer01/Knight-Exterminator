using System.Collections.Generic;
using UnityEngine;

public class TestXPGiver : MonoBehaviour, IInteractable
{
    [SerializeField] private string toolTipText;
    [SerializeField] private Outline outline;
    [Space]
    [SerializeField] private Transform teleportPoint;

    private void Start()
    {
        outline.enabled = false;
    }

    public string GetInteractionToolTip()
    {
        return toolTipText;
    }

    public void HighlightObject(bool highlight)
    {
        outline.enabled = highlight;
    }

    public void Interact(GameObject sender)
    {
        PlayerInteraction senderPlayer = sender.GetComponent<PlayerInteraction>();

        if (senderPlayer != null)
        {
            PlayerManager.Instance.TeleportPlayer(senderPlayer.OwnerClientId, teleportPoint.position);       
        }
    }
}
