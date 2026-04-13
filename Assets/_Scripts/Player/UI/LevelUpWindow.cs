using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LevelUpWindow : PlayerUIWindow
{
    [Header("References")]
    [SerializeField] private PlayerLevelController levelController;
    [SerializeField] private PlayerStatsController statsController;

    [Header("UI Parts")]
    [SerializeField] private TMP_Text healthValueText_Current;
    [SerializeField] private TMP_Text staminaValueText_Current;
    [SerializeField] private TMP_Text damageValueText_Current;
    [SerializeField] private TMP_Text defenseValueText_Current;
    [Space]
    [SerializeField] private TMP_Text levelValueText_New;
    [SerializeField] private TMP_Text healthValueText_New;
    [SerializeField] private TMP_Text staminaValueText_New;
    [SerializeField] private TMP_Text damageValueText_New;
    [SerializeField] private TMP_Text defenseValueText_New;
    [Space]
    [SerializeField] private TMP_Text currentXpValueText;
    [SerializeField] private TMP_Text newXpValueText;
    [SerializeField] private TMP_Text needXpValueText;
    [Space]
    [SerializeField] private TMP_Text costValueText;
    [SerializeField] private UIButton upCostButton;
    [SerializeField] private UIButton downCostButton;
    [Space]
    [SerializeField] private UIButton confirmButton;
    [SerializeField] private UIButton closeButton;

    private int newLevel;

    private void Start()
    {
        UpdateCostButtons();
        UpdateCurrentStats();

        confirmButton.onClick.AddListener(LevelUp);
        closeButton.onClick.AddListener(CloseWindow);
        upCostButton.onClick.AddListener(PreviewLevelUp);
        downCostButton.onClick.AddListener(PreviewLevelDown);
    }

    private void CloseWindow()
    {
        playerUI.SetWindow(GameUIWindowType.HUD);
    }

    public override void SetWindowActive(bool active, float timeToSwitch = 0.25f)
    {
        if (active == true)
        {
            newLevel = levelController.CurrentLevel;

            UpdateCurrentStats();
            UpdateNewStats(newLevel);
            UpdateCostButtons();
        }

        base.SetWindowActive(active, timeToSwitch);
    }

    private void LevelUp()
    {
        if (newLevel <= levelController.CurrentLevel)
            return;

        levelController.UpgradeLevel(newLevel - levelController.CurrentLevel);

        UpdateCurrentStats();
        UpdateNewStats(newLevel);
        UpdateCostButtons();
    }

    #region Cost Buttons

    private void PreviewLevelUp()
    {
        SetTargetLevel(newLevel + 1);
    }

    private void PreviewLevelDown()
    {
        SetTargetLevel(newLevel - 1); 
    }

    private void UpdateCostButtons()
    {
        bool canLevelUp = levelController.CheckCanLevelUp(newLevel - levelController.CurrentLevel);
        bool canAddLevel = levelController.CheckCanLevelUp(newLevel + 1 - levelController.CurrentLevel);

        if (newLevel <= levelController.CurrentLevel)
        {
            downCostButton.interactable = false;
            upCostButton.interactable = true;
        }
        else if (newLevel >= levelController.MaxLevel)
        {
            downCostButton.interactable = true;
            upCostButton.interactable = false;
        }
        else
        {
            downCostButton.interactable = true;
            upCostButton.interactable = true;
        }

        confirmButton.interactable = canLevelUp && newLevel != levelController.CurrentLevel;

        if (!canAddLevel)
        {
            upCostButton.interactable = false;
        }
    }

    private void SetTargetLevel(int level)
    {
        if (level < 1 || level > levelController.MaxLevel)
        {
            confirmButton.interactable = false;
            return;
        }

        newLevel = level;

        UpdateNewStats(newLevel);
        int cost = levelController.GetLevelUpCost(levelController.CurrentLevel, newLevel);
        newXpValueText.text = $"{levelController.CurrentXP - cost}";
        costValueText.text = $"{cost}";

        UpdateNeedValueText();
        UpdateCostButtons();
    }

    #endregion

    private float TrimNumber(float number)
    {
        return MathF.Round(number, 2);
    }

    private void UpdateCurrentStats()
    {
        healthValueText_Current.text = $"{TrimNumber(statsController.BaseHealth)}";
        staminaValueText_Current.text = $"{TrimNumber(statsController.BaseStamina)}";
        damageValueText_Current.text = $"{TrimNumber(statsController.BaseDamage.MainDamage)}";
        defenseValueText_Current.text = $"{TrimNumber(statsController.BaseHealthResist.FlatResistance)}";

        currentXpValueText.text = $"{levelController.CurrentXP}";
        newXpValueText.text = $"{levelController.CurrentXP}";
        UpdateNeedValueText();
    }

    private void UpdateNewStats(int level)
    {
        levelValueText_New.text = $"{level}";
        healthValueText_New.text = $"{TrimNumber(statsController.GetUpgradedHealth(level))}";
        staminaValueText_New.text = $"{TrimNumber(statsController.GetUpgradedStamina(level))}";
        damageValueText_New.text = $"{TrimNumber(statsController.GetUpgradedAttack(level))}";
        defenseValueText_New.text = $"{TrimNumber(statsController.GetUpgradedDefense(level))}";

        costValueText.text = "0";
    }

    private void UpdateNeedValueText()
    {
        bool canAddLevel = levelController.CheckCanLevelUp(newLevel - levelController.CurrentLevel + 1);

        if (canAddLevel)
        {
            needXpValueText.text = "";
        }
        else if (levelController.IsMaxLevel)
        {
            needXpValueText.text = $"Достигнут максимальный уровень";
        }
        else
        {
            int needXP = levelController.GetLevelUpCost(levelController.CurrentLevel, newLevel + 1) - levelController.CurrentXP;

            needXpValueText.text = $"До следующего уровня не хватает: {needXP}";
        }
    }
}
