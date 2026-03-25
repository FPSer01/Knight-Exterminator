using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private PlayerUI playerUI;
    [Space]
    [SerializeField] private Image image;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float alphaWhileDragging = 0.5f;
    public Transform ParentAfterDrag;

    private Transform parentOnDrag;

    private UpgradeItem item;
    public UpgradeItem Item { get => item; }

    public event Action OnDragEnd;

    public void SetupItem(PlayerUI ui, Transform parentOnDrag, UpgradeItem item)
    {
        playerUI = ui;
        this.item = item;
        this.parentOnDrag = parentOnDrag;

        image.sprite = item.ItemSprite;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        playerUI.DragActive = true;
        playerUI.SetInfoWindow(false, item);
        playerUI.InventoryWindow.StartHighlightSpecialSlot(item.Type);

        ParentAfterDrag = transform.parent;

        transform.SetParent(parentOnDrag);
        transform.SetAsLastSibling();

        canvasGroup.alpha = alphaWhileDragging;
        image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = InputManager.Input.UI.Point.ReadValue<Vector2>();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        playerUI.DragActive = false;
        playerUI.InventoryWindow.StopHighlightSpecialSlot();

        transform.SetParent(ParentAfterDrag);
        canvasGroup.alpha = 1f;
        image.raycastTarget = true;

        OnDragEnd?.Invoke();
        //Debug.Log("End Drag");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playerUI.DragActive)
            return;

        playerUI.SetInfoWindow(true, item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        playerUI.SetInfoWindow(false, item);
    }
}
