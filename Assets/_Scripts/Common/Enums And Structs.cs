using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;

#region Enums

public enum StanceType
{
    None = 0,

    // Рыцарь
    Default = 1,
    Attack = 2,
    Defense = 3,
    Dexterity = 4,

    // Маг
    Frost = 5,
    Pyro = 6,
    Electric = 7
}

public enum ChunkCullType
{
    None,
    GameObjects,
    Renderers,
    Full
}

public enum StatusType
{
    Poison,
    Bloodloss,
    Slowness,
    Stun
}

public enum GameUIWindowType
{
    None,
    HUD,
    LevelUp,
    Menu,
    Inventory,
    Settings,
    GameOver,
    Merchant,
    Map,
    Revive,
    Spectator
}

public enum ItemType
{
    None,
    Attack,
    General,
    Stance,
    All
}

public enum ItemRarity
{
    Common,
    Rare,
    Mythical,
    Boss
}

public enum ItemSpeсialEffect
{
    None,
    Vampirism
}

public enum ElementalAttackState
{
    None,
    Fire,
    Electric
}

public enum GameOverStatus 
{
    Defeat,
    Victory
}

public enum LevelRoomType
{
    Regular,
    Start,
    Special,
    Merchant,
    Boss
}

public enum MainMenuWindowType
{
    Main,
    Settings,
    SingleplayerGame,
    MultiplayerGame,
    Gamemode,
    Lobby
}

public enum GameLevels
{
    Level_1 = GameScenes.LEVEL_1,
    Level_2 = GameScenes.LEVEL_2,
    Level_3 = GameScenes.LEVEL_3,
    Final_Level = GameScenes.FINAL_LEVEL,
    Test = GameScenes.TEST
}

public enum Gamemode
{
    None,
    Singleplayer,
    Multiplayer
}

public enum MerchantOperation
{
    Buy,
    Sell
}

public enum GameState
{
    Lobby,
    InGame
}

#endregion

#region Structs

public struct PlayerLevelData
{
    public int Level;
    public int RequiredXP;
    public float Health;
    public float Stamina;
    public float Damage;
    public float FlatDefense;
}

public struct HitTransform : INetworkSerializable
{
    public HitTransform(Vector3 pos, Quaternion rot)
    {
        Position = pos;
        Rotation = rot;
    }

    public Vector3 Position;
    public Quaternion Rotation;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
    }
}

[Serializable]
public struct StatusInflictData : INetworkSerializable
{
    [Header("Status Effects")]
    public float Poison;
    public float PoisonWearOffTime;
    [Space]
    public float Bloodloss;
    public float BloodlossWearOffTime;
    [Space]
    public float SlownessAmount;
    [Range(0f, 1f)] public float SlownessIntensity;
    public float SlownessWearOffTime;
    [Space]
    public float StunAmount;
    public float StunWearOffTime;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Poison);
        serializer.SerializeValue(ref PoisonWearOffTime);

        serializer.SerializeValue(ref Bloodloss);
        serializer.SerializeValue(ref BloodlossWearOffTime);

        serializer.SerializeValue(ref SlownessAmount);
        serializer.SerializeValue(ref SlownessIntensity);
        serializer.SerializeValue(ref SlownessWearOffTime);

        serializer.SerializeValue(ref StunAmount);
        serializer.SerializeValue(ref StunWearOffTime);
    }

    public static StatusInflictData operator +(StatusInflictData right, StatusInflictData left)
    {
        StatusInflictData result = new StatusInflictData();

        result.Poison = right.Poison + left.Poison;
        result.Bloodloss = right.Bloodloss + left.Bloodloss;
        result.SlownessAmount = right.SlownessAmount + left.SlownessAmount;
        result.StunAmount = right.StunAmount + left.StunAmount;

        result.PoisonWearOffTime = right.PoisonWearOffTime + left.PoisonWearOffTime;
        result.BloodlossWearOffTime = right.BloodlossWearOffTime + left.BloodlossWearOffTime;
        result.SlownessWearOffTime = right.SlownessWearOffTime + left.SlownessWearOffTime;
        result.StunWearOffTime = right.StunWearOffTime + left.StunWearOffTime;

        return result;
    }

    public static StatusInflictData operator *(StatusInflictData right, StatusInflictData left)
    {
        StatusInflictData result = new StatusInflictData();

        result.Poison = right.Poison * left.Poison;
        result.Bloodloss = right.Bloodloss * left.Bloodloss;
        result.SlownessAmount = right.SlownessAmount * left.SlownessAmount;
        result.StunAmount = right.StunAmount * left.StunAmount;

        result.PoisonWearOffTime = right.PoisonWearOffTime * left.PoisonWearOffTime;
        result.BloodlossWearOffTime = right.BloodlossWearOffTime * left.BloodlossWearOffTime;
        result.SlownessWearOffTime = right.SlownessWearOffTime * left.SlownessWearOffTime;
        result.StunWearOffTime = right.StunWearOffTime * left.StunWearOffTime;

        return result;
    }
}

[Serializable]
public struct HealthDamageResist
{
    [Header("General")]
    public float FlatResistance;

    [Header("Resists")]
    [Range(-1f, 1f)] public float Physical;
    [Range(-1f, 1f)] public float Fire;
    [Range(-1f, 1f)] public float Electrical;

    [Header("Resists of Status Effects")]
    [Range(0f, 1f)] public float Poison;
    [Range(0f, 1f)] public float Bloodloss;
    [Range(0f, 1f)] public float Slowness;
    [Range(0f, 1f)] public float Stun;

    public static HealthDamageResist operator +(HealthDamageResist right, HealthDamageResist left)
    {
        HealthDamageResist result = new HealthDamageResist();

        result.FlatResistance = right.FlatResistance + left.FlatResistance;

        result.Physical = Mathf.Clamp01(right.Physical + left.Physical);
        result.Fire = Mathf.Clamp01(right.Fire + left.Fire);
        result.Electrical = Mathf.Clamp01(right.Electrical + left.Electrical);
        result.Poison = Mathf.Clamp01(right.Poison + left.Poison);
        result.Bloodloss = Mathf.Clamp01(right.Bloodloss + left.Bloodloss);
        result.Slowness = Mathf.Clamp01(right.Slowness + left.Slowness);
        result.Stun = Mathf.Clamp01(right.Stun + left.Stun);

        return result;
    }

    public static HealthDamageResist operator *(HealthDamageResist right, HealthDamageResist left)
    {
        HealthDamageResist result = new HealthDamageResist();

        result.FlatResistance = right.FlatResistance * left.FlatResistance;

        result.Physical = Mathf.Clamp01(right.Physical * left.Physical);
        result.Fire = Mathf.Clamp01(right.Fire * left.Fire);
        result.Poison = Mathf.Clamp01(right.Poison * left.Poison);
        result.Bloodloss = Mathf.Clamp01(right.Bloodloss * left.Bloodloss);
        result.Slowness = Mathf.Clamp01(right.Slowness * left.Slowness);

        return result;
    }
}

[Serializable]
public struct BossPhaseSettings : INetworkSerializable
{
    public int PhaseIndex;
    [Range(0f, 1f)] public float PercentHealth;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PhaseIndex);
        serializer.SerializeValue(ref PercentHealth);
    }
}

[Serializable]
public struct AttackDamageType : INetworkSerializable
{
    public enum MainDamageType
    {
        Physical,
        Fire,
        Electrical
    }

    [Header("Attack")]
    public MainDamageType MainType;
    [Space]
    public float Physical;
    public float Fire;
    public float Electrical;

    [Header("Status")]
    public StatusInflictData StatusData;

    [Header("General")]
    public bool IgnoreDefence;
    public bool DisableHurtEffect;

    public float MainDamage { get => GetMainDamage(); set => SetMainDamage(value); }

    public AttackDamageType GetMultDamage(float mult, bool affectStatus = false)
    {
        AttackDamageType multAttack = new()
        {
            Physical = Physical * mult,
            Fire = Fire * mult,
            Electrical = Electrical * mult
        };

        if (affectStatus)
        {
            multAttack.StatusData.Poison = StatusData.Poison * mult;
            multAttack.StatusData.Bloodloss = StatusData.Bloodloss * mult;
            multAttack.StatusData.SlownessAmount = StatusData.SlownessAmount * mult;
            multAttack.StatusData.StunAmount = StatusData.StunAmount * mult;
        }

        return multAttack;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Physical);
        serializer.SerializeValue(ref Fire);
        serializer.SerializeValue(ref Electrical);

        serializer.SerializeValue(ref StatusData);

        serializer.SerializeValue(ref IgnoreDefence);
        serializer.SerializeValue(ref DisableHurtEffect);
    }

    private void SetMainDamage(float newDamageValue)
    {
        switch (MainType) 
        {
            case MainDamageType.Physical: 
                Physical = newDamageValue;
                break;

            case MainDamageType.Fire: 
                Fire = newDamageValue; 
                break;

            case MainDamageType.Electrical:
                Electrical = newDamageValue;
                break;
        }
    }

    private float GetMainDamage()
    {
        switch (MainType)
        {
            case MainDamageType.Physical: return Physical;
            case MainDamageType.Fire: return Fire;
            case MainDamageType.Electrical: return Electrical;
            default: return Physical;
        }
    }
}

[Serializable]
public struct SFXMultisampleCollection
{
    public AudioSource Source;
    public List<AudioClip> ListSFX;
    public AudioClip AdditionalSFX;
    public float inDuration;
    public float outDuration;
}

[Serializable]
public struct SFXCollection
{
    public string Tag;
    [Space]
    public AudioSource OneSFXSource;
    public AudioClip OneSFX;
    [Space]
    public List<AudioClip> ListSFX;
    [Space]
    [Range(0f, 1f)] public float Volume;
    [Range(0f, 1f)] public float Chance;
}

[Serializable]
public struct EnemyTargetData
{
    public EnemyTargetData(ulong clientId, GameObject playerObject)
    {
        ClientId = clientId;
        PlayerObject = playerObject;
        Components = playerObject.GetComponent<PlayerComponents>();
        NetworkObject = Components.NetworkObject;
    }

    public ulong ClientId;
    public GameObject PlayerObject;
    public NetworkObject NetworkObject;
    public PlayerComponents Components;

    public Vector3 Position => GetTargetPosition();
    public bool IsDead => Components.Health.IsDead;

    public bool IsValid => PlayerObject != null && NetworkObject.IsSpawned;

    #region Methods

    private Vector3 GetTargetPosition()
    {
        if (Components == null)
            return default;

        if (Components.Movement == null)
            return default;

        return Components.Movement.transform.position;
    }

    public static bool operator ==(EnemyTargetData a, EnemyTargetData b)
    {
        return a.ClientId == b.ClientId;
    }

    public static bool operator !=(EnemyTargetData a, EnemyTargetData b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        return obj is EnemyTargetData other && this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClientId, PlayerObject);
    }

    #endregion
}

#endregion

public class GameScenes
{
    public const int MAIN_MENU = 0;
    public const int TUTORIAL = 1;
    public const int LEVEL_1 = 2;
    public const int LEVEL_2 = 3;
    public const int LEVEL_3 = 4;
    public const int FINAL_LEVEL = 5;
    public const int TEST = 6;
}

public static class StartGameData
{
    public static bool SingleplayerOneLife { get; set; }

    /// <summary>
    /// Выбранная стойка (ТОЛЬКО В ОДИНОЧНОМ РЕЖИМЕ)
    /// </summary>
    public static StanceType Stance { get; set; }
    public static Gamemode GameMode {
        set => gamemode = value;
        get 
        {
            return AutoValidateGamemode();
        }
    }

    private static Gamemode gamemode;

    private static Gamemode AutoValidateGamemode()
    {
        if (gamemode == Gamemode.None && NetworkManager.Singleton != null)
        {
            int clients = NetworkManager.Singleton.ConnectedClientsIds.Count;

            if (clients <= 1)
            {
                return Gamemode.Singleplayer;
            }
            else
            {
                return Gamemode.Multiplayer;
            }
        }
        
        return gamemode;
    }

}

public class LogTags 
{
    public const string RED_COLOR = "<color=#FF0000>";
    public const string GREEN_COLOR = "<color=#00FF00>";
    public const string BLUE_COLOR = "<color=#0000FF>";

    public const string ORANGE_COLOR = "<color=#FFBD31>";
    public const string PURPLE_COLOR = "<color=#800080>";
    public const string PINK_COLOR = "<color=#FFC0CB>";
    public const string YELLOW_COLOR = "<color=#FFFF00>";
    public const string CYAN_COLOR = "<color=#00FFFF>";

    public const string END_COLOR = "</color>";

    public const string BOLD_OPEN = "<b>";
    public const string BOLD_CLOSE = "</b>";
}