using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoWindow : PlayerUIWindow
{
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemTypeText;
    [SerializeField] private TMP_Text itemRarityText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Image rareEffect;
    [Space]
    [SerializeField] private Vector2 offset;

    private RectTransform rect;
    private bool active = false;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public override void SetWindowActive(bool active, float timeToSwitch = 0.1f)
    {
        this.active = active;

        canvasGroup.DOFade(active ? 1 : 0, timeToSwitch).SetUpdate(true);
    }

    public void SetInfo(UpgradeItem item)
    {
        itemNameText.text = $"{item.ItemName}";
        itemTypeText.text = $"{item.GetTypeName()}";
        itemRarityText.text = $"{item.GetRarityName()}";
        itemRarityText.color = item.GetRarityColor();
        itemDescriptionText.text = $"{item.ItemDescription}";

        if (item.Rarity == ItemRarity.Common)
            rareEffect.color = new Color(0, 0, 0, 0);
        else
            rareEffect.color = item.GetRarityColor();
    }

    private void Update()
    {
        FollowPointer();
    }

    private void FollowPointer()
    {
        if (!active)
            return;

        var point = InputManager.Input.UI.Point.ReadValue<Vector2>();

        rect.position = point + offset;
    }
}
