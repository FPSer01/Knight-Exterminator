using UnityEngine;

public class LevelUpObject : InteractableObject
{
    public override void Interact(GameObject sender)
    {
        base.Interact(sender);

        if (sender.TryGetComponent(out PlayerLevelController levelController))
        {
            levelController.OpenLevelUpWindow();
        }
        else
        {
            Debug.LogError("Нет контроллера уровня");
        }
    }
}
