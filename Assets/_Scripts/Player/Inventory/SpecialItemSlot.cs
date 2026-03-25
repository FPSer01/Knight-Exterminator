using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpecialItemSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public ItemType Type;
    [SerializeField] private UITooltip slotToopTip;
    [TextArea(5, 20)]
    [SerializeField] private string info;

    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;

            var draggable = dropped.GetComponent<DraggableItem>();

            if (draggable.Item.Type == Type || draggable.Item.Type == ItemType.All)
                draggable.ParentAfterDrag = transform;
        }
        else
        {
            GameObject dropped = eventData.pointerDrag;
            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();

            GameObject current = transform.GetChild(0).gameObject;
            DraggableItem currentDraggable = current.GetComponent<DraggableItem>();

            if (draggableItem.Item.Type == Type && currentDraggable.Item.Type == Type)
            {
                currentDraggable.transform.SetParent(draggableItem.ParentAfterDrag);
                draggableItem.ParentAfterDrag = transform;
            }
        } 
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (transform.childCount > 0)
            return;

        slotToopTip.SetInfo(info);
        slotToopTip.SetVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        slotToopTip.SetVisible(false);
    }
}