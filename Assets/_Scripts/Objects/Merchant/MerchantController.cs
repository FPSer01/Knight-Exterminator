using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MerchantController : NetworkBehaviour
{
    [Header("Merchant Items")]
    [SerializeField] private MerchantItems items;
    [SerializeField] private int itemsAmount;

    [Header("Merchant Services")]
    [SerializeField] private int rerollCost = 100;
    [SerializeField] private int refillHealCost = 100;

    private int amountCanSell;

    public NetworkList<NetworkItem> MerchantItems = new(
        null, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public int RerollCost { get => rerollCost; }
    public int RefillHealCost { get => refillHealCost; }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            amountCanSell = itemsAmount;
            GenerateShop(itemsAmount);
        }
    }

    private void GenerateShop(int amount)
    {
        MerchantItems.Clear();
        
        for (int i = 0; i < amount; i++)
        {
            NetworkItem networkItem = new NetworkItem();

            UpgradeItem randomItem = items.GetItem();
            int itemIndex = items.Database.GetItemIndex(randomItem);
            networkItem.ItemDatabaseIndex = itemIndex;
            MerchantItems.Add(networkItem);
        }
    }

    public void ShowMerchantWindow(GameObject sender)
    {
        PlayerUI ui = sender.GetComponentInChildren<PlayerUI>();

        ui.MerchantWindow.BoundMerchant(this);
        ui.SetWindow(GameUIWindowType.Merchant);
    }

    public UpgradeItem GetItemFromDatabase(int index)
    {
        return items.Database.GetItem(index);
    }

    public int GetItemIndexFromDatabase(UpgradeItem item)
    {
        return items.Database.GetItemIndex(item);
    }

    [Rpc(SendTo.Server)]
    public void BuyItem_ServerRpc(int merchantItemIndex, int itemIndex, ulong clientId)
    {
        BuyItem_ClientRpc(clientId, itemIndex);

        MerchantItems.RemoveAt(merchantItemIndex);
        amountCanSell--;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BuyItem_ClientRpc(ulong clientId, int itemIndex)
    {
        if (NetworkManager.LocalClientId != clientId)
            return;

        PlayerComponents playerComponents = null;

        foreach (var netObj in NetworkManager.LocalClient.OwnedObjects)
        {
            playerComponents = netObj.GetComponent<PlayerComponents>();

            if (playerComponents != null) break;
        }

        PlayerInventory inventory = playerComponents.Inventory;
        PlayerUI playerUI = playerComponents.UI;

        UpgradeItem item = GetItemFromDatabase(itemIndex);
        int cost = item.BuyCost;

        inventory.PutItemInInventory(item);
        playerUI.MerchantWindow.ChangeCurrency(-cost);

        Debug.Log($"Merchant Item Buy: {LogTags.GREEN_COLOR}Success{LogTags.END_COLOR} [id: {GetItemIndexFromDatabase(item)}, name: {item.ItemName}]");
    }

    [Rpc(SendTo.Server)]
    public void SellItem_ServerRpc(int playerItemIndex, ulong clientId)
    {
        SellItem_ClientRpc(playerItemIndex, clientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SellItem_ClientRpc(int playerItemIndex, ulong clientId)
    {
        if (NetworkManager.LocalClientId != clientId)
            return;

        PlayerComponents playerComponents = null;

        foreach (var netObj in NetworkManager.LocalClient.OwnedObjects)
        {
            playerComponents = netObj.GetComponent<PlayerComponents>();

            if (playerComponents != null) break;
        }

        PlayerInventory inventory = playerComponents.Inventory;
        PlayerUI playerUI = playerComponents.UI;

        UpgradeItem item = inventory.CurrentItems[playerItemIndex];
        int cost = item.SellCost;

        playerUI.MerchantWindow.ChangeCurrency(cost);
        inventory.RemoveItemInInventory(playerItemIndex);

        Debug.Log($"Merchant Item Sell: {LogTags.GREEN_COLOR}Success{LogTags.END_COLOR} [id: {GetItemIndexFromDatabase(item)}, name: {item.ItemName}]");
    }

    [Rpc(SendTo.Server)]
    public void SellAllItems_ServerRpc(ulong clientId)
    {
        SellAllItems_ClientRpc(clientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SellAllItems_ClientRpc(ulong clientId)
    {
        if (NetworkManager.LocalClientId != clientId)
            return;

        PlayerComponents playerComponents = null;

        foreach (var netObj in NetworkManager.LocalClient.OwnedObjects)
        {
            playerComponents = netObj.GetComponent<PlayerComponents>();

            if (playerComponents != null) break;
        }

        PlayerInventory inventory = playerComponents.Inventory;
        PlayerUI playerUI = playerComponents.UI;

        int sellCost = 0;
        inventory.CurrentItems.ForEach(item =>
        {
            if (item != null)
            {
                sellCost += item.SellCost;
            }
        });

        playerUI.MerchantWindow.ChangeCurrency(sellCost);
        inventory.ClearInventory();

        Debug.Log($"Merchant All Item Sell: {LogTags.GREEN_COLOR}Success{LogTags.END_COLOR}");
    }

    [Rpc(SendTo.Server)]
    public void RerollItems_ServerRpc(ulong clientId)
    {
        GenerateShop(amountCanSell);
        ChangeCurrency_ClientRpc(clientId, -rerollCost);

        Debug.Log($"Merchant Reroll: {LogTags.GREEN_COLOR}Success{LogTags.END_COLOR} [client id: {clientId}]");
    }

    [Rpc(SendTo.Server)]
    public void RefillHeal_ServerRpc(ulong clientId)
    {
        RefillHeal_ClientRpc(clientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RefillHeal_ClientRpc(ulong clientId)
    {
        if (NetworkManager.LocalClientId != clientId)
            return;

        PlayerComponents playerComponents = null;

        foreach (var netObj in NetworkManager.LocalClient.OwnedObjects)
        {
            playerComponents = netObj.GetComponent<PlayerComponents>();

            if (playerComponents != null) break;
        }

        PlayerHealth playerHealth = playerComponents.Health;
        PlayerUI playerUI = playerComponents.UI;

        playerHealth.RefillHeals(1);
        playerUI.MerchantWindow.ChangeCurrency(-refillHealCost);

        Debug.Log($"Merchant Refill Heals: {LogTags.GREEN_COLOR}Success{LogTags.END_COLOR} [client id: {clientId}]");
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChangeCurrency_ClientRpc(ulong clientId, int value)
    {
        if (NetworkManager.LocalClientId != clientId)
            return;

        PlayerComponents playerComponents = null;

        foreach (var netObj in NetworkManager.LocalClient.OwnedObjects)
        {
            playerComponents = netObj.GetComponent<PlayerComponents>();

            if (playerComponents != null) break;
        }

        PlayerUI playerUI = playerComponents.UI;

        playerUI.MerchantWindow.ChangeCurrency(value);
    }
}
