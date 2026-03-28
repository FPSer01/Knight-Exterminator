using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BossRoom : NetworkBehaviour
{
    private readonly static string DEBUG_TAG = $"[{LogTags.CYAN_COLOR}Boss Room{LogTags.END_COLOR}]";

    public static BossRoom Instance { private set; get; }

    [Header("Boss Settings")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform bossSpawnPosition;

    [Header("Players Settings")]
    [SerializeField] private Transform playerSpawnPosition;

    [Header("Boss Room Objects")]
    [SerializeField] private LevelExitObject exitObject;

    private List<PlayerComponents> playerComponents = new();

    public event Action OnBossBattleBegin;

    public Vector3 PlayerSpawnPoint { get => playerSpawnPosition.position; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(InitializeBossRoom(0.25f));
        }
    }

    private IEnumerator InitializeBossRoom(float delay = 0)
    {
        yield return new WaitForSeconds(delay);

        exitObject.SetActive(false);
    }

    public Vector3 GetPlayerTeleportPoint()
    {
        return playerSpawnPosition.position;
    }

    /// <summary>
    /// Начать босс файт. Вызывает только сервер
    /// </summary>
    public void BeginBossBattle()
    {
        if (!IsServer)
        {
            Debug.Log($"{DEBUG_TAG} BeginBossBattle Invoked By Client, Ignoring...");
            return;
        }

        foreach (var playerObj in PlayerManager.Instance.SpawnedPlayers.Values)
        {
            if (playerObj.TryGetComponent(out PlayerComponents components))
            {
                playerComponents.Add(components);
            }
        }

        PlayerManager.Instance.TeleportAllPlayers(playerSpawnPosition.position);
        PlayerManager.Instance.SetMiniMapVisibilityAll(false);
        PlayerManager.Instance.BlockMapAll(true);

        SpawnBoss();

        if (LevelMusicController.Instance != null)
            LevelMusicController.Instance.SetBattleMusic(true);

        OnBossBattleBegin?.Invoke();
        Debug.Log($"{DEBUG_TAG} Boss Battle Started");
    }

    private void SpawnBoss()
    {
        GameObject bossObject = Instantiate(bossPrefab, bossSpawnPosition.position, Quaternion.identity);
        EnemyBossComponents bossComponents = bossObject.GetComponent<EnemyBossComponents>();

        SetActiveBossHealthUI(true);
        SetBossNameUI(bossComponents.BossName);
        UpdateBossHealthUI(1, true);

        bossComponents.Health.OnStatusInflicted += (type) => SetBossStatusUI(type, true);
        bossComponents.Health.OnStatusWearOff += (type) => SetBossStatusUI(type, false);
        bossComponents.Health.OnDeath += OnBossDeath;

        bossComponents.NetworkObject.Spawn();
        Debug.Log($"{DEBUG_TAG} Boss Spawned");
    }

    private void OnBossDeath()
    {
        exitObject.SetActive(true);
        SetActiveBossHealthUI(false);

        if (LevelMusicController.Instance != null)
            LevelMusicController.Instance.SetBattleMusic(false);

        UpgradePlayerHeal();
        Debug.Log($"{DEBUG_TAG} Boss Defeated");
    }

    private void UpgradePlayerHeal()
    {
        foreach (var player in playerComponents)
        {
            player.Health.ChangeHealsAmount_OwnerRpc(1);
        }
    }

    #region Boss UI

    public void SetActiveBossHealthUI(bool active)
    {
        if (!IsServer)
        {
            Debug.Log($"{DEBUG_TAG} SetActiveBossHealthUI Invoked By Client, Ignoring...");
            return;
        }

        foreach (var components in playerComponents)
        {
            if (components == null)
                continue;

            components.UI.BossHealthUI.SetActive_OwnerRpc(active);
        }
    }

    public void SetBossNameUI(string name)
    {
        if (!IsServer)
        {
            Debug.Log($"{DEBUG_TAG} SetBossNameUI Invoked By Client, Ignoring...");
            return;
        }

        foreach (var components in playerComponents)
        {
            if (components == null)
                continue;

            components.UI.BossHealthUI.SetBossName_OwnerRpc(name);
        }
    }

    public void UpdateBossHealthUI(float sliderValue, bool updateDamageBarInstantly = false)
    {
        if (!IsServer)
        {
            Debug.Log($"{DEBUG_TAG} UpdateBossHealthUI Invoked By Client, Ignoring...");
            return;
        }

        foreach (var components in playerComponents)
        {
            if (components == null)
                continue;

            components.UI.BossHealthUI.UpdateHealthBar_OwnerRpc(sliderValue, updateDamageBarInstantly);
        }
    }

    public void SetBossStatusUI(StatusType type, bool active)
    {
        if (!IsServer)
        {
            Debug.Log($"{DEBUG_TAG} SetBossStatusUI Invoked By Client, Ignoring...");
            return;
        }

        foreach (var components in playerComponents)
        {
            if (components == null)
                continue;

            components.UI.BossHealthUI.SetStatusIcon_OwnerRpc(type, active);
        }
    }

    public void UpdateBossHealthUIDamageNumbers(float value)
    {
        UpdateBossHealthUIDamageNumbers_ServerRpc(value);
    }

    [Rpc(SendTo.Server)]
    private void UpdateBossHealthUIDamageNumbers_ServerRpc(float value)
    {
        foreach (var components in playerComponents)
        {
            if (components == null)
                continue;

            components.UI.BossHealthUI.UpdateDamageNumber_OwnerRpc(value);
        }
    }

    #endregion

}
