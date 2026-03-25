using DG.Tweening;
using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerLevelController : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxLevel = 15;
    [SerializeField] private int startXpToLevel = 1000;
    private int xpToLevel;

    [Header("Current Level")]
    [SerializeField] private int currentLevel;

    [Header("Current XP")]
    [SerializeField] private TMP_Text currentXPText;
    [SerializeField] private int currentXP;
    [SerializeField] private float animationTime_XP;

    [Header("Components")]
    [SerializeField] private PlayerComponents playerComponents;
    private PlayerStatsController playerStats => playerComponents.StatsController;
    private PlayerUI playerUI => playerComponents.UI;
    private PlayerStateUI playerState => playerComponents.UI.PlayerStateUI;

    private const float R = 1.3f;

    private bool blockLeveling = false;

    public int CurrentLevel { get => currentLevel; }
    public int MaxLevel { get => maxLevel; }
    public int CurrentXP { get => currentXP; }

    public bool IsMaxLevel { get => blockLeveling; }

    private void Start()
    {
        currentXPText.text = $"{currentXP}";
        UpdateLevelText();

        xpToLevel = startXpToLevel;
    }

    public override void OnNetworkSpawn()
    {
        EnemyManager.Instance.OnEnemyXPDrop += ChangeXP;
    }

    public override void OnNetworkDespawn()
    {
        EnemyManager.Instance.OnEnemyXPDrop -= ChangeXP;
    }

    /*public void OnEnable()
    {
        EnemyManager.Instance.OnEnemyXPDrop += ChangeXP;
    }

    public void OnDisable()
    {
        EnemyManager.Instance.OnEnemyXPDrop -= ChangeXP;
    }*/

    public void ChangeXP(int value)
    {
        int oldValue = currentXP;
        currentXP += value;
        UpdateXPText(oldValue, currentXP);
    }

    public void UpgradeLevel(int levelsToUpgrade)
    {
        int cost = GetLevelUpCost(currentLevel, currentLevel + levelsToUpgrade);

        if (!CheckCanLevelUp(levelsToUpgrade))
            return;

        currentLevel += levelsToUpgrade;
        playerStats.SetBaseStatsForLevel(currentLevel);
        UpdateLevelText();

        if (currentLevel == maxLevel)
            blockLeveling = true;

        ChangeXP(-cost);
        xpToLevel = GetRequiredXPToLevelUp(currentLevel);
    }

    public bool CheckCanLevelUp(int levelsToUpgrade)
    {
        int cost = GetLevelUpCost(currentLevel, currentLevel + levelsToUpgrade);

        if (cost > currentXP || blockLeveling)
            return false;

        return true;
    }

    public int GetLevelUpCost(int currentLevel, int newLevel)
    {
        if (currentLevel >= newLevel)
            return 0;

        int cost = 0;

        for (int i = currentLevel; i < newLevel; i++)
        {
            cost += GetRequiredXPToLevelUp(i);
        }

        return cost;
    }

    public PlayerLevelData PreviewLevel(int levelToPreview, bool getXpToLevelToThis = false)
    {
        PlayerLevelData data = new PlayerLevelData();

        data.Level = levelToPreview;

        if (getXpToLevelToThis)
            data.RequiredXP = GetLevelUpCost(currentLevel, levelToPreview);
        else 
            data.RequiredXP = GetRequiredXPToLevelUp(levelToPreview);

        data.Health = playerStats.GetUpgradedHealth(levelToPreview);
        data.Stamina = playerStats.GetUpgradedStamina(levelToPreview);
        data.Damage = playerStats.GetUpgradedAttack(levelToPreview);
        data.FlatDefense = playerStats.GetUpgradedDefense(levelToPreview);

        return data;
    }

    public int GetRequiredXPToLevelUp(int level)
    {
        if (level == 1)
            return startXpToLevel;

        return Mathf.RoundToInt(startXpToLevel * Mathf.Pow(R, level - 1));
    }

    #region UI Methods

    private void UpdateXPText(int oldValue, int newValue)
    {
        int value = oldValue;

        DOTween.Kill(currentXPText);
        DOTween.To(() => value, (x) => value = x, newValue, animationTime_XP)
            .SetEase(Ease.OutSine)
            .SetTarget(currentXPText)
            .OnUpdate(() => currentXPText.text = $"{value}");
    }

    private void UpdateLevelText()
    {
        playerState.SetLevelValue(currentLevel);
    }

    public void OpenLevelUpWindow()
    {
        playerUI.SetWindow(GameUIWindowType.LevelUp);
    }

    #endregion

    public void LogLevelRequirements()
    {
        string logFormat = "Level {0} - Need: {1}, Overall: {2}";
        int overall = 0;

        for (int i = 1; i <= maxLevel; i++)
        {
            int need = GetRequiredXPToLevelUp(i);
            overall += need;
            Debug.Log(string.Format(logFormat, i, need, overall));
        }
    }
}