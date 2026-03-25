using UnityEngine;

public class TutorialExitObject : InteractableObject
{
    public override void HighlightObject(bool highlight) { }

    public override void Interact(GameObject sender)
    {
        base.Interact(sender);

        if (sender.TryGetComponent(out PlayerUI playerUI))
        {
            playerUI.SetGameOverStatus(GameOverStatus.Victory);
            playerUI.SetWindow(GameUIWindowType.GameOver);
        }
    }
}
