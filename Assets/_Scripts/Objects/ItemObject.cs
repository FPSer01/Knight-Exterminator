using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ItemObject : InteractableObject
{
    [Header("General")]
    [SerializeField] private UpgradeItem item;
    [SerializeField] private UpgradeItemDatabase database;
    [Space]
    [SerializeField] private List<ItemEffectData> effects;
    private ItemEffectData currentEffect;

    public NetworkVariable<NetworkItem> NetworkItem = new(
        new(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    private bool isSetup { get => item != null; }
    private bool highlight = false;

    public override void OnNetworkSpawn()
    {
        NetworkItem.OnValueChanged += Item_OnValueChanged;
    }

    public override void OnNetworkDespawn()
    {
        NetworkItem.OnValueChanged -= Item_OnValueChanged;
    }

    private void Item_OnValueChanged(NetworkItem previousValue, NetworkItem newValue)
    {
        UpgradeItem newItem = database.GetItem(newValue.ItemDatabaseIndex);

        SetupItemObject(newItem);
    }

    private void FixedUpdate()
    {
        if (!isSetup)
            return;

        HoldEffectStill();
    }

    private void HoldEffectStill()
    {
        var rotation = Quaternion.Euler(Vector3.zero);

        currentEffect.GlowEffect.transform.rotation = rotation;
        currentEffect.HighlightEffect.transform.rotation = rotation;
    }

    public override void HighlightObject(bool highlight)
    {
        if (this.highlight == highlight || currentEffect.HighlightEffect == null || !isSetup)
            return;

        this.highlight = highlight;

        if (highlight)
            currentEffect.HighlightEffect.Play();
        else
            currentEffect.HighlightEffect.Stop();
    }

    public override void Interact(GameObject sender)
    {
        if (!isSetup)
            return;

        if (sender.TryGetComponent(out PlayerInventory playerInventory))
        {
            bool success = playerInventory.PutItemInInventory(item);

            if (success)
            {
                DeleteItemObject_ServerRpc();
            }
        }
    }

    public void SetupItemObject(UpgradeItem item)
    {
        if (item == null)
        {
            Debug.LogError("Item tries to spawn without data!");
            DeleteItemObject_ServerRpc();
            return;
        }

        this.item = item;

        currentEffect = effects.Find((data) => data.Rarity == item.Rarity);

        currentEffect.Conteiner.SetActive(true);
        currentEffect.GlowEffect.Play();
        currentEffect.HighlightEffect.Stop();
    }

    [Rpc(SendTo.Server)]
    private void DeleteItemObject_ServerRpc()
    {
        NetworkObject.Despawn();
    }

    [Serializable]
    public struct ItemEffectData
    {
        public ItemRarity Rarity;
        public GameObject Conteiner;
        public ParticleSystem GlowEffect;
        public ParticleSystem HighlightEffect;
    }
}
