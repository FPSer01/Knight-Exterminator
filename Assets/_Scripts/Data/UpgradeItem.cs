using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeItem", menuName = "Data/Upgrade Item")]
public class UpgradeItem : ScriptableObject
{
    public static readonly Color COMMON_RARITY_COLOR = Color.white;
    public static readonly Color RARE_RARITY_COLOR = Color.blue;
    public static readonly Color MYTHICAL_RARITY_COLOR = new Color(128, 0, 128);
    public static readonly Color BOSS_RARITY_COLOR = new Color(255, 173, 0);

    [Header("General")]
    [SerializeField] private string itemName;
    [TextArea(5,20)] [SerializeField] private string itemDescription;
    [Space]
    [SerializeField] private ItemType type;
    [SerializeField] private ItemRarity rarity;
    [Space]
    [SerializeField] private Sprite itemSprite;
    [Space]
    [SerializeField] private bool canBeSold;
    [SerializeField] private int buyCost;
    [SerializeField] private int sellCost;
    [Space(20f)]

    [Header("====== STATS ======")]

    [Space(20f)]
    [Header("Attack")]
    [SerializeField] private AttackDamageType flatDamage;
    [SerializeField] private bool elementPercentDamageFromPhysical;
    [SerializeField] private AttackDamageType percentDamage;

    [Space(20f)]
    [Header("Health")]
    [SerializeField] private float flatHealth;
    [SerializeField] private float percentHealth;
    [SerializeField] private HealthDamageResist resist;
    [SerializeField] private float percentFlatResistance;

    [Header("Stamina")]
    [SerializeField] private float flatStamina;
    [SerializeField] private float percentStamina;

    [Header("Stance")]
    [SerializeField] private float percentStanceDamage;
    [Space]
    [SerializeField] private float flatStanceCooldown;
    [SerializeField] private float percentStanceCooldown;
    [Space]
    [SerializeField] private float flatStanceDuration;
    [SerializeField] private float percentStanceDuration;

    [Space(20f)]
    [Header("Special")]
    [SerializeField] private ItemSpeсialEffect specialEffect;
    [SerializeField] private float vampirismHealPercent;

    public string ItemName { get => itemName; }
    public string ItemDescription { get => itemDescription; }

    public ItemType Type { get => type; }
    public ItemRarity Rarity { get => rarity; }
    public Sprite ItemSprite { get => itemSprite; }

    public bool CanBeSold { get => canBeSold; set => canBeSold = value; }
    public int BuyCost { get => buyCost; }
    public int SellCost { get => sellCost; }

    public AttackDamageType FlatDamage { get => flatDamage; }
    public AttackDamageType PercentDamage { get => percentDamage; }

    public float FlatHealth { get => flatHealth; }
    public float PercentHealth { get => percentHealth; }

    public HealthDamageResist Resist { get => resist; }
    public float PercentFlatResistance { get => percentFlatResistance; }

    public float FlatStamina { get => flatStamina; }
    public float PercentStamina { get => percentStamina; }

    public ItemSpeсialEffect SpecialEffect { get => specialEffect; }
    public bool ElementPercentDamageFromPhysical { get => elementPercentDamageFromPhysical; }

    public float PercentStanceDamage { get => percentStanceDamage; }
    public float FlatStanceCooldown { get => flatStanceCooldown; }
    public float PercentStanceCooldown { get => percentStanceCooldown; }
    public float FlatStanceDuration { get => flatStanceDuration; }
    public float PercentStanceDuration { get => percentStanceDuration; }
    public float VampirismHealPercent { get => vampirismHealPercent; }

    public string GetRarityName()
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return "Обычная";

            case ItemRarity.Rare:
                return "Редкая";

            case ItemRarity.Mythical:
                return "Мифическая";

            case ItemRarity.Boss:
                return "Особое";

            default: return null;
        }
    }

    public string GetTypeName()
    {
        switch (type)
        {
            case ItemType.Attack:
                return "Усиление атаки";

            case ItemType.General:
                return "Общее усиление";

            case ItemType.Stance:
                return "Усиление стойки";

            case ItemType.All:
                return "Универсальное усиление";

            default: return null;
        }
    }

    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return COMMON_RARITY_COLOR;

            case ItemRarity.Rare:
                return RARE_RARITY_COLOR;

            case ItemRarity.Mythical:
                return MYTHICAL_RARITY_COLOR;

            case ItemRarity.Boss:
                return BOSS_RARITY_COLOR;

            default: return Color.black;
        }
    }
}
