using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private Color originalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.black;

    private bool cursorInside;
    private Image image;

    public event Action<DraggableItem, Vector2> OnContextSummon;
    //public event Action OnContextClose;

    private void Start()
    {
        image = GetComponent<Image>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;

            var draggable = dropped.GetComponent<DraggableItem>();
            draggable.ParentAfterDrag = transform;
        }
        else // Обмен предметов между ячейками
        {
            GameObject dropped = eventData.pointerDrag;
            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();

            GameObject current = transform.GetChild(0).gameObject;
            DraggableItem currentDraggable = current.GetComponent<DraggableItem>();

            currentDraggable.transform.SetParent(draggableItem.ParentAfterDrag);
            draggableItem.ParentAfterDrag = transform;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        cursorInside = true;
        image.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cursorInside = false;
        image.color = originalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && cursorInside && transform.childCount != 0)
            OnContextSummon?.Invoke(transform.GetChild(0).GetComponent<DraggableItem>(), InputManager.Input.UI.Point.ReadValue<Vector2>());
    }
}
