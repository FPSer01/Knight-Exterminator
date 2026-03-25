using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryWindow : PlayerUIWindow
{
    [Header("Slots")]
    [SerializeField] private GameObject draggableItemPrefab;
    [SerializeField] private Transform dragContainer;
    [SerializeField] private List<ItemSlot> slots;

    [Header("Special Slots")]
    [SerializeField] private SpecialItemSlot attackSlot;
    [SerializeField] private CanvasGroup attackSlotHighlightCanvas;
    [Space]
    [SerializeField] private SpecialItemSlot generalSlot;
    [SerializeField] private CanvasGroup generalSlotHighlightCanvas;
    [Space]
    [SerializeField] private SpecialItemSlot stanceSlot;
    [SerializeField] private CanvasGroup stanceSlotHighlightCanvas;
    [Space]
    [SerializeField] private float highlightTime;
    private IEnumerator highlightCoroutine;

    [Header("Context Menu")]
    [SerializeField] private InventoryContextMenu contextMenu;

    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStatsController playerStats;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerAttackMelee playerAttack;
    [SerializeField] private PlayerStance playerStance;

    [Header("UI")]
    [SerializeField] private TMP_Text healthStatText;
    [SerializeField] private TMP_Text staminaStatText;
    [SerializeField] private TMP_Text damageStatText;
    [SerializeField] private TMP_Text defenseStatText;
    [SerializeField] private Image stanceIcon;

    public event Action OnInventoryLayoutChange;

    private void Start()
    {
        stanceIcon.preserveAspect = true;
        stanceIcon.sprite = playerStance.GetStanceSprite();

        contextMenu.OnItemPutOut += ContextMenu_OnItemPutOut;
        playerInventory.OnItemGet += PlayerInventory_OnItemGet;
        playerInventory.OnItemRemove += PlayerInventory_OnItemRemove;
        playerInventory.OnClear += PlayerInventory_OnClear;

        playerStats.OnStatsUpdated += PlayerStats_OnStatsUpdated;

        slots.ForEach((slot) => slot.OnContextSummon += SummonContextMenu);

        StopHighlightSpecialSlot();
    }

    private void PlayerInventory_OnClear()
    {
        foreach (var slot in slots)
        {
            if (slot.transform.childCount == 1)
                Destroy(slot.transform.GetChild(0).gameObject);
        }
    }

    private void PlayerInventory_OnItemRemove(UpgradeItem item, int index)
    {
        var slot = slots[index];

        if (GetUpgradeItemFromSlot(slot) == item)
        {
            Destroy(slot.transform.GetChild(0).gameObject);
        }

        UpdateItemsDataBase();
    }

    private void PlayerStats_OnStatsUpdated()
    {
        UpdateStatsUI();
    }

    private void ContextMenu_OnItemPutOut(DraggableItem dragItem)
    {
        int index = slots.FindIndex(0, slots.Count, (slot) => IsItemFromSlot(dragItem, slot));

        playerInventory.DiscardItemInInventory(index);
    }

    private bool IsItemFromSlot(DraggableItem dragItem, ItemSlot slot)
    {
        if (slot.transform.childCount == 0)
            return false;

        var dragItemOther = slot.transform.GetChild(0).GetComponent<DraggableItem>();

        if (dragItemOther == dragItem)
            return true;

        return false;
    }

    private void UpdateItemsDataBase()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            playerInventory.CurrentItems[i] = GetUpgradeItemFromSlot(slots[i]);
        }

        playerInventory.AttackUpgrade = GetUpgradeItemFromSlot(attackSlot);
        playerInventory.GeneralUpgrade = GetUpgradeItemFromSlot(generalSlot);
        playerInventory.StanceUpgrade = GetUpgradeItemFromSlot(stanceSlot);

        OnInventoryLayoutChange?.Invoke();
    }

    private UpgradeItem GetUpgradeItemFromSlot(SpecialItemSlot slot)
    {
        if (slot.transform.childCount == 0)
        {
            return null;
        }
        else
        {
            Transform dragObj = slot.transform.GetChild(0);
            DraggableItem draggable = dragObj.GetComponent<DraggableItem>();

            return draggable.Item;
        }
    }

    private UpgradeItem GetUpgradeItemFromSlot(ItemSlot slot)
    {
        if (slot.transform.childCount == 0)
        {
            return null;
        }
        else
        {
            Transform dragObj = slot.transform.GetChild(0);
            DraggableItem draggable = dragObj.GetComponent<DraggableItem>();

            return draggable.Item;
        }
    }

    private void PlayerInventory_OnItemGet(UpgradeItem item, int index)
    {
        ItemSlot slot = slots[index];

        GameObject itemDraggableObj = Instantiate(draggableItemPrefab, slot.transform);
        var itemDraggable = itemDraggableObj.GetComponent<DraggableItem>();

        itemDraggable.SetupItem(playerUI, dragContainer, item);
        itemDraggable.OnDragEnd += UpdateItemsDataBase;
    }

    private void SummonContextMenu(DraggableItem context, Vector2 pos)
    {
        contextMenu.SetTarget(context);
        contextMenu.SetVisible(true, pos);
    }

    public override void SetWindowActive(bool active, float timeToSwitch = 0.1f)
    {
        if (active)
        {
            UpdateStatsUI();
        }

        base.SetWindowActive(active, timeToSwitch);
    }

    private void UpdateStatsUI()
    {
        // Здоровье
        SetStatText(healthStatText, "Здоровье: {0}", playerHealth.MaxHealth);

        // Выносливость
        SetStatText(staminaStatText, "Выносливость: {0}", playerStamina.MaxStamina);

        // Урон
        if (playerAttack.AttackDamage.Fire > 0 && playerAttack.AttackDamage.Electrical > 0) // Если есть огненный и электрический урон
        {
            SetStatText(
                damageStatText,
                "Урон: {0} " +
                "+ <color=#CD5400>{1}</color> " +
                "+ <color=#009EBF>{2}</color>", 
                playerAttack.AttackDamage.Physical, 
                playerAttack.AttackDamage.Fire,
                playerAttack.AttackDamage.Electrical
                );
        }
        if (playerAttack.AttackDamage.Fire > 0) // Если есть огненный урон
        {
            SetStatText(damageStatText, "Урон: {0} " +
                "+ <color=#CD5400>{1}</color>", 
                playerAttack.AttackDamage.Physical, 
                playerAttack.AttackDamage.Fire);
        }
        else if (playerAttack.AttackDamage.Electrical > 0) // Если есть электрический урон
        {
            SetStatText(damageStatText, "Урон: {0} " +
                "+ <color=#009EBF>{1}</color>", 
                playerAttack.AttackDamage.Physical, 
                playerAttack.AttackDamage.Electrical);
        }
        else // Если есть только обычный урон
        {
            SetStatText(damageStatText, "Урон: {0}", playerAttack.AttackDamage.Physical);
        }

        // Защита
        SetStatText(defenseStatText, "Защита: {0}", playerHealth.ResistData.FlatResistance);
    }

    private void SetStatText(TMP_Text textObject, string format, params float[] values)
    {
        textObject.text = string.Format(
            format, 
            values.Select(
                v => MathF.Round(v, 2)).Cast<object>().ToArray()
                );
    }

    public void StartHighlightSpecialSlot(ItemType type)
    {
        StopHighlightSpecialSlot();

        switch (type)
        {
            case ItemType.Attack:
                highlightCoroutine = HighlightSpecialSlot(attackSlotHighlightCanvas);
                break;

            case ItemType.Stance:
                highlightCoroutine = HighlightSpecialSlot(stanceSlotHighlightCanvas);
                break;

            case ItemType.General:
                highlightCoroutine = HighlightSpecialSlot(generalSlotHighlightCanvas);
                break;

            case ItemType.All:
                highlightCoroutine = HighlightSpecialSlots(attackSlotHighlightCanvas, stanceSlotHighlightCanvas, generalSlotHighlightCanvas);
                break;

        }

        StartCoroutine(highlightCoroutine);
    }

    public void StopHighlightSpecialSlot()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        attackSlotHighlightCanvas.DOKill();
        stanceSlotHighlightCanvas.DOKill();
        generalSlotHighlightCanvas.DOKill();

        attackSlotHighlightCanvas.alpha = 0f;
        stanceSlotHighlightCanvas.alpha = 0f;
        generalSlotHighlightCanvas.alpha = 0f;
    }

    private IEnumerator HighlightSpecialSlot(CanvasGroup highlightObject)
    {
        //Debug.Log("Start special item slot highlight");

        highlightObject.alpha = 0f;

        while (true)
        {
            highlightObject.DOFade(1, highlightTime).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(highlightTime);

            highlightObject.DOFade(0, highlightTime).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(highlightTime);
        }
    }

    private IEnumerator HighlightSpecialSlots(params CanvasGroup[] highlightObjects)
    {
        //Debug.Log("Start special item slot highlight");

        foreach (var item in highlightObjects)
        {
            item.alpha = 0f;
        }

        while (true)
        {
            foreach (var item in highlightObjects)
            {
                item.DOFade(1, highlightTime).SetEase(Ease.OutQuad);  
            }

            yield return new WaitForSeconds(highlightTime);

            foreach (var item in highlightObjects)
            {
                item.DOFade(0, highlightTime).SetEase(Ease.OutQuad);
            }

            yield return new WaitForSeconds(highlightTime);
        }
    }
}
