using System;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "StanceInfo", menuName = "Data/Stance Info")]
public class StanceInfo : ScriptableObject
{
    [Header("Main Info")]
    [TextArea(2, 3)]
    [SerializeField] private string stanceName;
    [TextArea(5, 20)]
    [SerializeField] private string stanceDescription;
    [SerializeField] private Sprite stanceIcon;

    [Header("Settings")]
    [SerializeField] private StanceType type;
    [SerializeField] private int reviveCount;
    [Space]
    [SerializeField] private float duration;
    [SerializeField] private float cooldown;

    [Header("Level Settings")]
    [SerializeField] private float damagePerLevel;
    [SerializeField] private float defensePerLevel;
    [SerializeField] private float staminaPerLevel;
    [SerializeField] private float healthPerLevel;

    public string StanceName { get => stanceName; }
    public string Description { get => GetFullDescription(); }
    public string FullDescription { get => GetFullDescription() + "\n" + GetReviveDescription(); }
    public Sprite StanceIcon { get => stanceIcon;  }
    public StanceType Type { get => type; }

    public int ReviveCount { get => reviveCount; }

    public Stance StanceData { get => GetStanceData(); }

    private Stance GetStanceData()
    {
        Stance stanceData = new()
        {
            Type = type,
            Duration = duration,
            Cooldown = cooldown,
            DamageGainPerLevel = damagePerLevel,
            DefenseGainPerLevel = defensePerLevel,
            StaminaGainPerLevel = staminaPerLevel,
            HealthGainPerLevel = healthPerLevel,
            Revives = reviveCount
        };

        return stanceData;
    }

    private string GetFullDescription()
    {
        float minValue = Mathf.Min(damagePerLevel, defensePerLevel, staminaPerLevel, healthPerLevel);
        float maxValue = Mathf.Max(damagePerLevel, defensePerLevel, staminaPerLevel, healthPerLevel);

        string description = "Прибавка за уровень:";

        description += DecideLine(damagePerLevel, minValue, maxValue, "к урону");
        description += DecideLine(defensePerLevel, minValue, maxValue, "к защите");
        description += DecideLine(staminaPerLevel, minValue, maxValue, "к выносливости");
        description += DecideLine(healthPerLevel, minValue, maxValue, "к здоровью");

        description += "\n\n" + stanceDescription;

        if (cooldown > 0)
        {
            description += $"\n\nПерезарядка {Mathf.RoundToInt(cooldown)} секунд.";
        }

        return description;
    }

    private string DecideLine(float value, float minValue, float maxValue, string statName)
    {
        if (minValue == maxValue || value > minValue && value < maxValue)
        {
            return GetHighlightLine(value, statName);
        }

        if (value == minValue)
        {
            return GetDebuffLine(value, statName);
        }

        if (value == maxValue)
        {
            return GetBuffLine(value, statName);
        }

        return GetHighlightLine(value, statName);
    }

    private string GetDebuffLine(float value, string statName)
    {
        float percentValue = value * 100f;
        return $"\n<style=\"Debuff\">{(percentValue >= 0 ? $"+{percentValue}" : percentValue)}%</style> {statName}";
    }

    private string GetHighlightLine(float value, string statName)
    {
        float percentValue = value * 100f;
        return $"\n<style=\"Highlight\">{(percentValue >= 0 ? $"+{percentValue}" : percentValue)}%</style> {statName}";
    }

    private string GetBuffLine(float value, string statName)
    {
        float percentValue = value * 100f;
        return $"\n<style=\"Buff\">{(percentValue >= 0 ? $"+{percentValue}" : percentValue)}%</style> {statName}";
    }

    public string GetReviveDescription()
    {
        return $"\n\nВозрождений на этаж: <style=\"Buff\">{reviveCount}</style>";
    }
}
[Serializable]
public struct Stance
{
    public readonly int Index { get => (int)Type; }

    public StanceType Type;
    public float Duration;
    public float Cooldown;
    [Space]
    public float DamageGainPerLevel;
    public float DefenseGainPerLevel;
    public float StaminaGainPerLevel;
    public float HealthGainPerLevel;
    [Space]
    public int Revives;
}
