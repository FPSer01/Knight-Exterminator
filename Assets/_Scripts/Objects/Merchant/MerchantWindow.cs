using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MerchantWindow : PlayerUIWindow
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerLevelController playerLevel;
    [SerializeField] private MerchantConfirmationWindow confirmationWindow;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("General")]
    [SerializeField] private GameObject itemPrefab;
    [Space]
    [SerializeField] private PageButton buyPageButton;
    [SerializeField] private GameObject buySideObject;
    [SerializeField] private Transform buySide;
    [Space]
    [SerializeField] private PageButton sellPageButton;
    [SerializeField] private GameObject sellSideObject;
    [SerializeField] private Transform sellSide;

    [Header("Actions")]
    [SerializeField] private UIButton refillButton;
    [SerializeField] private TMP_Text refillButtonText;
    [Space]
    [SerializeField] private UIButton rerollButton; 
    [SerializeField] private TMP_Text rerollButtonText;
    [Space]
    [SerializeField] private UIButton sellAllButton;
    [SerializeField] private TMP_Text sellAllButtonText;
    [Space]
    [SerializeField] private TMP_Text currencyText;

    [Header("UI")]
    [SerializeField] private UIButton exitButton;

    private MerchantItem currentMerchantItem;

    private int sellAllCost;
    private int oldCurrencyValue;

    private MerchantController boundMerchant;

    private void Start()
    {
        confirmationWindow.OnConfirm += ConfirmationWindow_OnConfirm;
        confirmationWindow.OnCancel += ConfirmationWindow_OnCancel;

        refillButton.onClick.AddListener(RefillHeals);
        exitButton.onClick.AddListener(ExitMerchant);

        rerollButton.onClick.AddListener(RerollItems);
        sellAllButton.onClick.AddListener(SellAll);

        buyPageButton.OnClick += BuyPageButton_OnClick;
        sellPageButton.OnClick += SellPageButton_OnClick;

        playerInventory.OnClear += UpdatePlayerSide;
        playerInventory.OnItemGet += OnItemInventoryChange;
        playerInventory.OnItemRemove += OnItemInventoryChange;

        SetBuyPage(true);
    }

    private void OnItemInventoryChange(UpgradeItem item, int arg2)
    {
        UpdatePlayerSide();
    }

    public override void SetWindowActive(bool active, float timeToSwitch = 0.1f)
    {
        if (active)
        {
            UpdateWindow();
        }
        else
        {
            confirmationWindow.SetActive(false);
            boundMerchant = null;
        }

        base.SetWindowActive(active, timeToSwitch);
    }

    public void BoundMerchant(MerchantController merchant)
    {
        UnboundMerchant();

        boundMerchant = merchant;
        boundMerchant.MerchantItems.OnListChanged += MerchantItems_OnListChanged;
    }

    public void UnboundMerchant()
    {
        boundMerchant = null;
    }

    private void MerchantItems_OnListChanged(NetworkListEvent<NetworkItem> changeEvent)
    {
        UpdateWindow();
    }

    #region Buttons

    private void ExitMerchant()
    {
        playerUI.SetWindow(GameUIWindowType.HUD);
    }

    private void SellAll()
    {
        boundMerchant.SellAllItems_ServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private void RerollItems()
    {
        if (boundMerchant.RerollCost >= playerLevel.CurrentXP)
            return;

        boundMerchant.RerollItems_ServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private void RefillHeals()
    {
        if (boundMerchant.RefillHealCost >= playerLevel.CurrentXP)
            return;

        boundMerchant.RefillHeal_ServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    #endregion

    #region Update Visuals

    private void UpdateWindow()
    {
        UpdateMerchantSide();
        UpdatePlayerSide();
        UpdateCurrencyText();
        UpdateRefillButton();

        refillButtonText.text = $"Пополнить 1 чайный гриб: {boundMerchant.RefillHealCost}";
        rerollButtonText.text = $"Обновить товары: {boundMerchant.RerollCost}";
    }

    /// <summary>
    /// Обновить сторону с продаваемыми предметами
    /// </summary>
    private void UpdateMerchantSide()
    {
        if (boundMerchant == null)
            return;

        foreach (Transform child in buySide)
        {
            Destroy(child.gameObject);
        }

        foreach (var networkItem in boundMerchant.MerchantItems)
        {
            if (networkItem.Equals(null))
                continue;

            UpgradeItem item = boundMerchant.GetItemFromDatabase(networkItem.ItemDatabaseIndex);
            SetupMerchantItemUI(item, MerchantOperation.Buy, buySide, BuyItem_OnItemPicked);
        }
    }

    /// <summary>
    /// Обновить сторону с имеющимися предметами в инвентаре
    /// </summary>
    private void UpdatePlayerSide()
    {
        foreach (Transform child in sellSide)
        {
            Destroy(child.gameObject);
        }

        var items = playerInventory.CurrentItems.ToList();
        sellAllCost = 0;

        foreach (var item in items)
        {
            if (item == null)
                continue;

            sellAllCost += item.SellCost;
            SetupMerchantItemUI(item, MerchantOperation.Sell, sellSide, SellItem_OnItemPicked);
        }

        UpdateSellAllButton();
    }

    private MerchantItem SetupMerchantItemUI(UpgradeItem item, MerchantOperation operation, Transform parent, Action<MerchantItem, UpgradeItem> callback)
    {
        GameObject itemObject = Instantiate(itemPrefab, parent);
        MerchantItem merchantItem = itemObject.GetComponent<MerchantItem>();

        merchantItem.SetupItem(item, playerUI, operation);
        merchantItem.OnItemPicked += callback;

        return merchantItem;
    }

    private void UpdateSellAllButton()
    {
        sellAllButtonText.text = $"Продать все: {sellAllCost}";
    }

    private void UpdateCurrencyText(bool imidiate = false)
    {
        if (imidiate)
        {
            currencyText.text = $"Кусочков хитина: {playerLevel.CurrentXP}";
        }
        else
        {
            int newValue = playerLevel.CurrentXP;
            DoUpdateCurrencyAnimation(oldCurrencyValue, newValue);
        }
    }

    private void DoUpdateCurrencyAnimation(int oldValue, int newValue)
    {
        int value = oldValue;

        DOTween.Kill(currencyText);
        DOTween.To(() => value, (x) => value = x, newValue, 0.5f)
            .SetEase(Ease.OutSine)
            .SetTarget(currencyText)
            .OnUpdate(() => currencyText.text = $"Кусочков хитина: {value}");
    }

    private void UpdateRefillButton()
    {
        if (playerHealth.CurrentHealAmount >= playerHealth.HealAmount || boundMerchant.RefillHealCost >= playerLevel.CurrentXP)
        {
            refillButton.interactable = false;
        }
        else
        {
            refillButton.interactable = true;
        }
    }

    #endregion

    public void ChangeCurrency(int value)
    {
        oldCurrencyValue = playerLevel.CurrentXP;
        playerLevel.ChangeXP(value);

        UpdateCurrencyText();
        UpdateRefillButton();
    }

    #region Pages

    private void SellPageButton_OnClick(PageButton button)
    {
        SetBuyPage(false);
    }

    private void BuyPageButton_OnClick(PageButton button)
    {
        SetBuyPage(true);
    }

    private void SetBuyPage(bool active)
    {
        if (active)
        {
            buyPageButton.Select();
            sellPageButton.Unselect();
        }
        else
        {
            sellPageButton.Select();
            buyPageButton.Unselect();
        }

        buySideObject.SetActive(active);
        sellSideObject.SetActive(!active);

        UpdateSellAllButton();
    }

    #endregion

    #region Confirmation Window

    private void ConfirmationWindow_OnCancel()
    {
        currentMerchantItem = null;
    }

    private void ConfirmationWindow_OnConfirm()
    {
        if (currentMerchantItem == null)
            return;

        ulong clientId = NetworkManager.Singleton.LocalClientId;
        int merchantItemIndex = currentMerchantItem.transform.GetSiblingIndex();
        int itemIndex = boundMerchant.GetItemIndexFromDatabase(currentMerchantItem.Item);

        switch (currentMerchantItem.ProvidedOperation)
        {
            case MerchantOperation.Buy:
                boundMerchant.BuyItem_ServerRpc(merchantItemIndex, itemIndex, clientId);
                break;

            case MerchantOperation.Sell:
                boundMerchant.SellItem_ServerRpc(merchantItemIndex, clientId);
                UpdatePlayerSide();
                break;
        }

        currentMerchantItem = null;
    }

    #endregion

    private void SellItem_OnItemPicked(MerchantItem pickedItem, UpgradeItem item)
    {
        confirmationWindow.SetWindowType(item, MerchantOperation.Sell);
        confirmationWindow.SetActive(true);

        currentMerchantItem = pickedItem;
    }

    private void BuyItem_OnItemPicked(MerchantItem pickedItem, UpgradeItem item)
    {
        if (item.BuyCost > playerLevel.CurrentXP || playerInventory.IsInventoryFull())
            return;

        confirmationWindow.SetWindowType(item, MerchantOperation.Buy);
        confirmationWindow.SetActive(true);

        currentMerchantItem = pickedItem;
    }
}
