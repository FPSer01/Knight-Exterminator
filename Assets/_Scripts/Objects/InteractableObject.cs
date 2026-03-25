using Unity.Netcode;
using UnityEngine;

public class InteractableObject : NetworkBehaviour, IInteractable
{
    [Header("Main Interactable")]
    [SerializeField] protected string toolTipText;
    [SerializeField] protected Outline outline;

    protected virtual void Start()
    {
        if (outline != null)
            outline.enabled = false;
    }

    public virtual string GetInteractionToolTip()
    {
        return toolTipText;
    }

    public virtual void HighlightObject(bool highlight)
    {
        if (!enabled)
        {
            outline.enabled = false;
            return;
        }

        if (outline != null)
            outline.enabled = highlight;
    }

    public virtual void Interact(GameObject sender)
    {
        
    }
}
