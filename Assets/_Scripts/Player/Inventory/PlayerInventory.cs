using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int inventorySlots = 12;
    [SerializeField] private Transform dropPoint;

    [Header("Inventory")]
    [SerializeField] private List<UpgradeItem> currentItems;
    [Space]
    [SerializeField] private UpgradeItem attackUpgrade;
    [SerializeField] private UpgradeItem generalUpgrade;
    [SerializeField] private UpgradeItem stanceUpgrade;

    [Header("Components")]
    [SerializeField] private PlayerComponents playerComponents;
    private PlayerInventoryWindow inventoryWindow => playerComponents.UI.InventoryWindow;

    public List<UpgradeItem> CurrentItems { get => currentItems; set => currentItems = value; }
    public UpgradeItem AttackUpgrade { get => attackUpgrade; set => attackUpgrade = value; }
    public UpgradeItem GeneralUpgrade { get => generalUpgrade; set => generalUpgrade = value; }
    public UpgradeItem StanceUpgrade { get => stanceUpgrade; set => stanceUpgrade = value; }
    public int InventorySlots { get => inventorySlots; }

    public event Action<UpgradeItem, int> OnItemGet;
    public event Action<UpgradeItem, int> OnItemDiscard;
    public event Action<UpgradeItem, int> OnItemRemove;
    public event Action OnClear;

    public event Action OnUpgradesChange;

    private void Start()
    {
        SetupItems();
        inventoryWindow.OnInventoryLayoutChange += InventoryWindow_OnInventoryLayoutChange;
    }

    private void InventoryWindow_OnInventoryLayoutChange()
    {
        OnUpgradesChange?.Invoke();
    }

    private void SetupItems()
    {
        currentItems = new();

        for (int i = 0; i < inventorySlots; i++)
        {
            currentItems.Add(null);
        }
    }

    /// <summary>
    /// Положить предмет в инвентарь
    /// </summary>
    /// <param name="item"></param>
    /// <returns>Можно ли положить в инвентарь предмет</returns>
    public bool PutItemInInventory(UpgradeItem item)
    {
        for (int i = 0; i < currentItems.Count; i++)
        {
            if (currentItems[i] == null)
            {
                currentItems[i] = item;
                OnItemGet?.Invoke(item, i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Убрать предмет по индексу
    /// </summary>
    /// <param name="itemIndex"></param>
    public void DiscardItemInInventory(int itemIndex)
    {
        OnItemDiscard?.Invoke(currentItems[itemIndex], itemIndex);

        ItemGenerator.Instance.SpawnItem(currentItems[itemIndex], dropPoint.position);
        currentItems[itemIndex] = null;
    }

    /// <summary>
    /// Убрать определенный предмет (ссылка)
    /// </summary>
    public void RemoveItemInInventory(UpgradeItem item)
    {
        int removeItemIndex = currentItems.FindIndex((i) => item == i);
        UpgradeItem removeItem = currentItems[removeItemIndex];

        OnItemRemove?.Invoke(removeItem, removeItemIndex);
        currentItems[removeItemIndex] = null;
    }

    /// <summary>
    /// Убрать определенный предмет (индекс)
    /// </summary>
    public void RemoveItemInInventory(int index)
    {
        UpgradeItem removeItem = currentItems[index];

        OnItemRemove?.Invoke(removeItem, index);
        currentItems[index] = null;
    }

    public void ClearInventory()
    {
        SetupItems();
        OnClear?.Invoke();
    }

    public bool IsInventoryFull()
    {
        if (currentItems.Contains(null))
            return false;

        return true;
    }

    public int GetEmptySlotsCount()
    {
        int count = currentItems.Count(item => item == null);
        return count;
    }

    public int GetOccupiedSlotsCount()
    {
        int count = currentItems.Count(item => item != null);
        return count;
    }
}
