using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerStatsController : NetworkBehaviour
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
    private float originalStanceDamageMult;

    [Header("Components")]
    [SerializeField] private PlayerComponents playerComponents;
    private PlayerMovement playerMovement => playerComponents.Movement;
    private PlayerHealth playerHealth => playerComponents.Health;
    private PlayerStamina playerStamina => playerComponents.Stamina;
    private PlayerAttackBase playerAttack => playerComponents.Attack;
    private PlayerStanceBase playerStance => playerComponents.Stance;
    private PlayerInventory playerInventory => playerComponents.Inventory;

    public float BaseHealth { get => currentBaseMaxHealth; }
    public HealthDamageResist BaseHealthResist { get => currentBaseHealthResist; }
    public float BaseStamina { get => currentBaseMaxStamina; }
    public AttackDamageType BaseDamage { get => currentBaseDamage; }

    public event Action OnStatsUpdated;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        playerInventory.OnUpgradesChange += PlayerInventory_OnUpgradesChange;
        StartCoroutine(AwaitForInitialization());
    }

    private IEnumerator AwaitForInitialization()
    {
        yield return new WaitUntil(() => playerAttack.IsInitialized && playerStance.IsInitialized);

        SetOriginalValues();
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
        originalStanceDamageMult = playerStance.StanceDamageMult;
    }

    /// <summary>
    /// Определение базовых статов какого-либо уровня без предметов
    /// </summary>
    /// <param name="level"></param>
    public void SetBaseStatsForLevel(int level)
    {
        ClearAllSpecialEffects();

        // Урон
        currentBaseDamage.MainDamage = GetUpgradedAttack(level);
        var attackDamage = playerAttack.AttackDamage;
        attackDamage.MainDamage = currentBaseDamage.MainDamage;
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
        playerStance.StanceDamageMult = originalStanceDamageMult;

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
        float attack = GetUpgradedStats(level, originalDamage.MainDamage, playerStance.CurrentStance.DamageGainPerLevel);

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

        // Статус игрока
        SetStatusUpgradeEffect(item);
        // Статы атаки игрока
        SetAttackUpgradeEffect(item);
        // Статы стойки
        SetStanceUpgradeEffect(item);

        // Различные эффекты
        SetSpecialEffect(item);
    }

    private void SetStatusUpgradeEffect(UpgradeItem item)
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
    }

    private void SetAttackUpgradeEffect(UpgradeItem item)
    {
        if (item == null)
            return;

        var attackDamage = playerAttack.AttackDamage;

        // Основной тип атаки
        attackDamage.MainDamage += item.MainDamage;
        attackDamage.MainDamage += currentBaseDamage.MainDamage * item.MainDamageMult;

        // Физическая атака
        attackDamage.Physical += item.FlatDamage.Physical;
        attackDamage.Physical += currentBaseDamage.Physical * item.PercentDamage.Physical;

        bool statusGainFromMainDamage = item.ElementPercentDamageFromPhysical;

        // Огонь
        attackDamage.Fire += item.FlatDamage.Fire;
        attackDamage.Fire += statusGainFromMainDamage ? 
            currentBaseDamage.MainDamage * item.PercentDamage.Fire 
            : currentBaseDamage.Fire * item.PercentDamage.Fire;

        // Электричество
        attackDamage.Electrical += item.FlatDamage.Electrical;
        attackDamage.Electrical += statusGainFromMainDamage ?
            currentBaseDamage.MainDamage * item.PercentDamage.Electrical
            : currentBaseDamage.Electrical * item.PercentDamage.Electrical;

        // Статус-эффекты атаки
        attackDamage.StatusData += item.FlatDamage.StatusData;
        attackDamage.StatusData += currentBaseDamage.StatusData * item.PercentDamage.StatusData;

        playerAttack.AttackDamage = attackDamage;
    }

    private void SetStanceUpgradeEffect(UpgradeItem item)
    {
        if (item == null)
            return;

        // Урон стойки
        playerStance.StanceDamageMult = originalStanceDamageMult + item.PercentStanceDamage;

        // Перезарядка стойки
        var stance = playerStance.CurrentStance;

        stance.Cooldown += item.FlatStanceCooldown;
        stance.Cooldown += originalStanceData.Cooldown * item.PercentStanceCooldown;

        // Длительность стойки
        stance.Duration += item.FlatStanceDuration;
        stance.Duration += originalStanceData.Duration * item.PercentStanceDuration;

        playerStance.CurrentStance = stance;
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
