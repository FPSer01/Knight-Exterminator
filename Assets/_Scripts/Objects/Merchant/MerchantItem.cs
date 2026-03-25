using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MerchantItem : PlayerUIWindow, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private const string COST_MASK = "Стоимость: {0}";

    [SerializeField] private UpgradeItem item;
    [Space]
    [SerializeField] private Image targetGraphic;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color hoverColor;
    [SerializeField] private Color pressColor;

    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemCost;

    public MerchantOperation ProvidedOperation { get; private set; }
    
    public UpgradeItem Item { get => item; }

    public event Action<MerchantItem, UpgradeItem> OnItemPicked;

    private bool prepareToShowConfirmation = false;

    public void SetupItem(UpgradeItem item, PlayerUI playerUI, MerchantOperation operation)
    {
        this.item = item;
        this.playerUI = playerUI;

        itemIcon.preserveAspect = true;
        itemIcon.sprite = item.ItemSprite;

        itemName.text = item.ItemName;

        ProvidedOperation = operation;

        switch (ProvidedOperation)
        {
            case MerchantOperation.Buy:
                itemCost.text = string.Format(COST_MASK, item.BuyCost);
                break;
            case MerchantOperation.Sell:
                itemCost.text = string.Format(COST_MASK, item.SellCost);
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetGraphic.DOColor(hoverColor, 0.1f);
        playerUI.SetInfoWindow(true, item);

        prepareToShowConfirmation = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetGraphic.DOColor(normalColor, 0.1f);
        playerUI.SetInfoWindow(false, item);

        prepareToShowConfirmation = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!prepareToShowConfirmation)
            return;

        targetGraphic.DOColor(normalColor, 0.1f);
        OnItemPicked?.Invoke(this, item);

        prepareToShowConfirmation = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        prepareToShowConfirmation = true;
        targetGraphic.DOColor(pressColor, 0.1f);
    }
}
