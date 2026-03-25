using DG.Tweening;
using System;
using UnityEngine;

public class InventoryContextMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [Space]
    [SerializeField] private UIButton putOutButton;
    [Space]
    [SerializeField] private Vector2 offset;

    private DraggableItem contextTarget;
    private bool active = false;

    public bool Active { get => active; set => active = value; }

    public event Action<DraggableItem> OnItemPutOut;
    public event Action<bool> OnActiveChange;

    private void Start()
    {
        SetVisible(false);
        putOutButton.onClick.AddListener(PutOutTarget);
    }

    public void SetVisible(bool active, Vector2 pos = new Vector2(), float timeToSwitch = 0.1f)
    {
        this.active = active;

        canvasGroup.blocksRaycasts = active;
        canvasGroup.interactable = active;

        if (active)
            transform.position = pos + offset;

        canvasGroup.DOFade(active ? 1 : 0, timeToSwitch).SetUpdate(true);
        OnActiveChange?.Invoke(active);
    }

    public void SetTarget(DraggableItem dragItem)
    {
        contextTarget = dragItem;
    }

    private void PutOutTarget()
    {
        SetVisible(false);
        OnItemPutOut?.Invoke(contextTarget);

        Destroy(contextTarget.gameObject);
        contextTarget = null;
    }
}
