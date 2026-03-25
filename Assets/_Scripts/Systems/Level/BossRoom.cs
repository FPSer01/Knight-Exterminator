using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BossRoom : NetworkBehaviour
{
    public static BossRoom Instance { private set; get; }

    [Header("Boss Settings")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform bossSpawnPosition;

    [Header("Players Settings")]
    [SerializeField] private Transform playerSpawnPosition;

    [Header("Boss Room Objects")]
    [SerializeField] private LevelExitObject exitObject;

    private List<PlayerHealth> playerHealths = new();

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
            exitObject.SetActive(false);         
        }
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
            return;

        PlayerManager.Instance.TeleportAllPlayers(playerSpawnPosition.position);
        PlayerManager.Instance.SetMiniMapVisibilityAll(false);
        PlayerManager.Instance.BlockMapAll(true);

        SpawnBoss();

        if (LevelMusicController.Instance != null)
            LevelMusicController.Instance.SetBattleMusic(true);

        OnBossBattleBegin?.Invoke();
    }

    private void SpawnBoss()
    {
        GameObject bossObject = Instantiate(bossPrefab, bossSpawnPosition.position, Quaternion.identity);
        EnemyBossComponents bossComponents = bossObject.GetComponent<EnemyBossComponents>();

        BossHealthUI.Instance.SetActive(true);
        BossHealthUI.Instance.UpdateHealthBar(1, true);
        BossHealthUI.Instance.SetBossName(bossComponents.BossName);

        bossComponents.Health.OnStatusInflicted += (type) => BossHealthUI.Instance.SetStatusIcon(type, true);
        bossComponents.Health.OnStatusWearOff += (type) => BossHealthUI.Instance.SetStatusIcon(type, false);
        bossComponents.Health.OnDeath += OnBossDeath;

        bossComponents.NetworkObject.Spawn();
    }

    private void OnBossDeath()
    {
        exitObject.SetActive(true);
        BossHealthUI.Instance.SetActive(false);

        if (LevelMusicController.Instance != null)
            LevelMusicController.Instance.SetBattleMusic(false);

        UpgradePlayerHeal();
    }

    private void UpgradePlayerHeal()
    {
        foreach (var player in playerHealths)
        {
            player.ChangeHealsAmount(1);
        }
    }
}
