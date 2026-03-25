using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryContextCloser : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private InventoryContextMenu contextMenu;

    private void Start()
    {
        canvasGroup.alpha = 0f;
        contextMenu.OnActiveChange += ContextMenu_OnActiveChange;
    }

    private void ContextMenu_OnActiveChange(bool active)
    {
        canvasGroup.blocksRaycasts = active;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Middle)
        {
            contextMenu.SetVisible(false);
            contextMenu.SetTarget(null);
        }
    }
}
