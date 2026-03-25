using System;
using UnityEngine;

public class PlayerStatsController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float originalMoveSpeed;
    [SerializeField] private float currentBaseMoveSpeed;

    [Header("Health")]
    [SerializeField] private float originalMaxHealth;
    [SerializeField] private float currentBaseMaxHealth;
    [Space]
    [SerializeField] private HealthDamageResist originalHealthResist;
    [SerializeField] private HealthDamageResist currentBaseHealthResist;

    [Header("Stamina")]
    [SerializeField] private float originalMaxStamina;
    [SerializeField] private float currentBaseMaxStamina;

    [Header("Attack")]
    [SerializeField] private AttackDamageType originalDamage;
    [SerializeField] private AttackDamageType currentBaseDamage;

    [Header("Stance")]
    [SerializeField] private Stance originalStanceData;
    private float originalThrustAttackMult;

    [Header("Components")]
    [SerializeField] private PlayerComponents playerComponents;
    private PlayerMovement playerMovement => playerComponents.Movement;
    private PlayerHealth playerHealth => playerComponents.Health;
    private PlayerStamina playerStamina => playerComponents.Stamina;
    private PlayerAttackBase playerAttack => playerComponents.Attack;
    private PlayerStance playerStance => playerComponents.Stance;
    private PlayerInventory playerInventory => playerComponents.Inventory;

    public float BaseHealth { get => currentBaseMaxHealth; }
    public HealthDamageResist BaseHealthResist { get => currentBaseHealthResist; }
    public float BaseStamina { get => currentBaseMaxStamina; }
    public AttackDamageType BaseDamage { get => currentBaseDamage; }

    public event Action OnStatsUpdated;

    private void Start()
    {
        SetOriginalValues();

        playerInventory.OnUpgradesChange += PlayerInventory_OnUpgradesChange;
    }

    private void PlayerInventory_OnUpgradesChange()
    {
        ApplyAllUpgrades();
    }

    private void ApplyAllUpgrades(bool doResetBeforeApplying = true)
    {
        var attackUpgrade = playerInventory.AttackUpgrade;
        var generalUpgrade = playerInventory.GeneralUpgrade;
        var stanceUpgrade = playerInventory.StanceUpgrade;

        if (doResetBeforeApplying)
            ResetToBase();

        ApplyUpgradeEffect(attackUpgrade);
        ApplyUpgradeEffect(generalUpgrade);
        ApplyUpgradeEffect(stanceUpgrade);

        OnStatsUpdated?.Invoke();
    }

    private void SetOriginalValues()
    {
        originalMoveSpeed = playerMovement.MoveSpeed;
        currentBaseMoveSpeed = originalMoveSpeed;

        originalMaxHealth = playerHealth.MaxHealth;
        currentBaseMaxHealth = originalMaxHealth;

        originalHealthResist = playerHealth.ResistData;
        currentBaseHealthResist = originalHealthResist;

        originalMaxStamina = playerStamina.MaxStamina;
        currentBaseMaxStamina = originalMaxStamina;

        originalDamage = playerAttack.AttackDamage;
        currentBaseDamage = originalDamage;

        originalStanceData = playerStance.CurrentStance;
        originalThrustAttackMult = playerStance.ThrustAttackMult;
    }

    /// <summary>
    /// Определение базовых статов какого-либо уровня без предметов
    /// </summary>
    /// <param name="level"></param>
    public void SetBaseStatsForLevel(int level)
    {
        ClearAllSpecialEffects();

        // Урон
        currentBaseDamage.Physical = GetUpgradedAttack(level);
        var attackDamage = playerAttack.AttackDamage;
        attackDamage.Physical = currentBaseDamage.Physical;
        playerAttack.AttackDamage = attackDamage;

        // Защита
        currentBaseHealthResist.FlatResistance = GetUpgradedDefense(level);
        var resist = playerHealth.ResistData;
        resist.FlatResistance = currentBaseHealthResist.FlatResistance;
        playerHealth.ResistData = resist;

        // Здоровье
        currentBaseMaxHealth = GetUpgradedHealth(level);
        playerHealth.MaxHealth = currentBaseMaxHealth;

        // Стамина
        currentBaseMaxStamina = GetUpgradedStamina(level);
        playerStamina.MaxStamina = currentBaseMaxStamina;

        // Применяем эффекты предметов
        ApplyAllUpgrades(true);
    }

    private void ResetToBase()
    {
        playerAttack.AttackDamage = currentBaseDamage;
        playerHealth.ResistData = currentBaseHealthResist;
        playerHealth.MaxHealth = currentBaseMaxHealth;
        playerStamina.MaxStamina = currentBaseMaxStamina;
        playerStance.CurrentStance = originalStanceData;
        playerStance.ThrustAttackMult = originalThrustAttackMult;

        ClearAllSpecialEffects();
    }

    private float GetUpgradedStats(int level, float originalStat, float gainPerLevel)
    {
        float stat = originalStat * Mathf.Pow(1 + gainPerLevel, level - 1);

        return stat;
    }

    public float GetUpgradedHealth(int level)
    {
        float health = GetUpgradedStats(level, originalMaxHealth, playerStance.CurrentStance.HealthGainPerLevel);

        return health;
    }

    public float GetUpgradedStamina(int level)
    {
        float stamina = GetUpgradedStats(level, originalMaxStamina, playerStance.CurrentStance.StaminaGainPerLevel);

        return stamina;
    }

    public float GetUpgradedAttack(int level)
    {
        float attack = GetUpgradedStats(level, originalDamage.Physical, playerStance.CurrentStance.DamageGainPerLevel);

        return attack;
    }

    public float GetUpgradedDefense(int level)
    {
        float defense = GetUpgradedStats(level, originalHealthResist.FlatResistance, playerStance.CurrentStance.DefenseGainPerLevel);

        return defense;
    }

    private void ApplyUpgradeEffect(UpgradeItem item)
    {
        if (item == null)
            return;

        // Здоровье
        playerHealth.MaxHealth += item.FlatHealth;
        playerHealth.MaxHealth += currentBaseMaxHealth * item.PercentHealth;

        // Ресисты
        var resist = playerHealth.ResistData;

        resist += item.Resist;
        resist.FlatResistance += currentBaseHealthResist.FlatResistance * item.PercentFlatResistance;

        playerHealth.ResistData = resist;

        // Стамина
        playerStamina.MaxStamina += item.FlatStamina;
        playerStamina.MaxStamina += currentBaseMaxStamina * item.PercentStamina;

        // Физическая атака (базовая)
        var attackDamage = playerAttack.AttackDamage;

        attackDamage.Physical += item.FlatDamage.Physical;
        attackDamage.Physical += currentBaseDamage.Physical * item.PercentDamage.Physical;

        // Элементальная атака
        if (item.ElementPercentDamageFromPhysical)
        {
            attackDamage.Fire += item.FlatDamage.Fire;
            attackDamage.Fire += currentBaseDamage.Physical * item.PercentDamage.Fire;

            attackDamage.Electrical += item.FlatDamage.Electrical;
            attackDamage.Electrical += currentBaseDamage.Physical * item.PercentDamage.Electrical;
        }
        else
        {
            attackDamage.Fire += item.FlatDamage.Fire;
            attackDamage.Fire += currentBaseDamage.Fire * item.PercentDamage.Fire;

            attackDamage.Electrical += item.FlatDamage.Electrical;
            attackDamage.Electrical += currentBaseDamage.Electrical * item.PercentDamage.Electrical;
        }

        // Статус-эффекты атаки
        attackDamage.StatusData += item.FlatDamage.StatusData;
        attackDamage.StatusData += currentBaseDamage.StatusData * item.PercentDamage.StatusData;

        playerAttack.AttackDamage = attackDamage;

        // Урон стойки
        playerStance.ThrustAttackMult = originalThrustAttackMult + item.PercentStanceDamage;

        // Перезарядка стойки
        var stance = playerStance.CurrentStance;

        stance.Cooldown += item.FlatStanceCooldown;
        stance.Cooldown += originalStanceData.Cooldown * item.PercentStanceCooldown;

        // Длительность стойки
        stance.Duration += item.FlatStanceDuration;
        stance.Duration += originalStanceData.Duration * item.PercentStanceDuration;

        playerStance.CurrentStance = stance;

        // Различные эффекты
        SetSpecialEffect(item);
    }

    private void SetSpecialEffect(UpgradeItem item)
    {
        ItemSpeсialEffect effect = item.SpecialEffect;

        switch (effect)
        {
            case ItemSpeсialEffect.Vampirism:
                playerAttack.EnableVampirism(true, item.VampirismHealPercent);
                break;

            default:
                break;
        }
    }

    private void ClearAllSpecialEffects()
    {
        playerAttack.EnableVampirism(false);
    }
}
