using System.Collections.Generic;
using UnityEngine;

public class TestTeleport : MonoBehaviour, IInteractable
{
    [SerializeField] private string toolTipText;
    [SerializeField] private Outline outline;
    [Space]
    [SerializeField] private int xpToGive;

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
        PlayerLevelController levelController = sender.GetComponentInChildren<PlayerLevelController>();

        if (levelController != null)
        {
            levelController.ChangeXP(xpToGive);
        }
        else
        {
            EnemyManager.Instance.DoOnEnemyXPDrop(xpToGive);
        }
    }
}
