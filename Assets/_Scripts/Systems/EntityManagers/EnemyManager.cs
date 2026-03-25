using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyManager : NetworkBehaviour
{
    [Serializable]
    public struct LevelEnemySpawnSettings 
    {
        public GameLevels Level;
        public EnemySpawnSettings Enemies;
    }

    public static EnemyManager Instance { get; private set; }

    [Header("Singleplayer: General")]
    [Range(1f, 4f)] [SerializeField] private float singleplayerHealthMult = 1f;
    [Range(1f, 4f)] [SerializeField] private float singleplayerDamageMult = 1f;

    [Header("Multiplayer: General")]
    [Range(0f, 2f)] [SerializeField] private float healthMultPerPlayer = 0.4f;

    [Header("Level Enemy Spawn Settings")]
    [SerializeField] private List<LevelEnemySpawnSettings> levelEnemySpawnSettings;

    public event Action<int> OnEnemyXPDrop;

    public float HealthMult
    {
        get
        {
            if (StartGameData.GameMode == Gamemode.Singleplayer)
            {
                return singleplayerHealthMult;
            }
            else if (StartGameData.GameMode == Gamemode.Multiplayer)
            {
                return NetworkManager.ConnectedClientsIds.Count > 1 ?
                    1f + healthMultPerPlayer * NetworkManager.ConnectedClientsIds.Count :
                    1f;
            }

            return 1f;
        } 
    }

    public float DamageMult
    {
        get
        {
            if (StartGameData.GameMode == Gamemode.Singleplayer)
            {
                return singleplayerDamageMult;
            }
            else if (StartGameData.GameMode == Gamemode.Multiplayer)
            {
                return 1f;
            }

            return 1f;
        }
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public void DoOnEnemyXPDrop(int xpAmount)
    {
        RequestEnemyXPDrop_EveryoneRpc(xpAmount);
    }

    [Rpc(SendTo.Everyone)]
    private void RequestEnemyXPDrop_EveryoneRpc(int xpAmount)
    {
        OnEnemyXPDrop?.Invoke(xpAmount);
    }

    public EnemySpawnSettings GetSpawnSettings(GameLevels level)
    {
        var foundSetting = levelEnemySpawnSettings.Find(settings => settings.Level == level);
        return foundSetting.Enemies;
    }

    /// <summary>
    /// Заспавнить врага по настройкам
    /// </summary>
    /// <param name="level"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public GameObject SpawnEnemy(GameLevels level, Vector3 position)
    {
        var foundSetting = GetSpawnSettings(level);

        GameObject enemyPrefab = foundSetting.GetEnemyPrefab();
        GameObject enemyObject = Instantiate(enemyPrefab, position, Quaternion.identity);

        NetworkObject enemyNetObj = enemyObject.GetComponent<NetworkObject>();
        enemyNetObj.Spawn(true);

        return enemyObject;
    }

    public void SetSingleplayerHealthMult(float newValue)
    {
        singleplayerHealthMult = newValue;
    }

    public void SetSingleplayerDamageMult(float newValue)
    {
        singleplayerDamageMult = newValue;
    }
}
