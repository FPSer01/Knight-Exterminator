using UnityEngine;

public interface IInteractable
{
    void Interact(GameObject sender);
    void HighlightObject(bool highlight);
    string GetInteractionToolTip();
}
