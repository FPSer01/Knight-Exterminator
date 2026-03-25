using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SpectatorManager : NetworkBehaviour
{
    public static SpectatorManager Instance { private set; get; }

    [SerializeField] private PlayerManager playerSpawner;

    private SortedDictionary<ulong, SpectatorTarget> avalableTargets = new();

    private ulong MyClientId => NetworkManager.Singleton.LocalClientId;
    private SpectatorTarget MyTarget;

    private List<ulong> clientsIds = new();

    private SpectatorTarget spectatedTarget;
    private ulong spectatedClientId = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    #region Network API

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerSpawner.OnPlayerObjectAdded += PlayerSpawner_OnPlayerObjectAdded;
            playerSpawner.OnPlayerObjectRemoved += PlayerSpawner_OnPlayerObjectRemoved;
        }

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!clientsIds.Contains(clientId))
                clientsIds.Add(clientId);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            playerSpawner.OnPlayerObjectAdded -= PlayerSpawner_OnPlayerObjectAdded;
            playerSpawner.OnPlayerObjectRemoved -= PlayerSpawner_OnPlayerObjectRemoved;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    #endregion

    #region Callbacks

    private void PlayerSpawner_OnPlayerObjectRemoved(ulong obj)
    {
        UpdateAvalableTargets();
    }

    private void PlayerSpawner_OnPlayerObjectAdded(ulong arg1, GameObject arg2)
    {
        UpdateAvalableTargets();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!clientsIds.Contains(clientId))
            clientsIds.Add(clientId);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        clientsIds.Remove(clientId);

        UpdateAvalableTargets();

        // Переключаемся только если мы сейчас в режиме спектатора
        if (spectatedTarget != null && spectatedClientId == clientId)
        {
            TrySwitchToNextTarget();
        }
    }

    #endregion

    #region Public

    public void TryEnterSpectatorMode()
    {
        UpdateAvalableTargets();

        if (MyTarget == null)
        {
            Debug.LogError("SpectatorManager: MyTarget is null — own SpectatorTarget not found!");
            return;
        }

        TrySwitchToNextTarget();
        Debug.Log("Entering spectator...");
    }

    public void ExitSpectatorMode()
    {
        if (spectatedTarget != null)
        {
            EnableTargetCamera(spectatedTarget, false);
            EnableTargetUsername(spectatedTarget, true);
            spectatedTarget = null;
        }

        spectatedClientId = 0;

        if (MyTarget != null)
            EnableTargetCamera(MyTarget, true);

        Debug.Log("Exiting spectator...");
    }

    #endregion

    private void UpdateAvalableTargets()
    {
        avalableTargets = new();

        var targets = FindObjectsByType<SpectatorTarget>(FindObjectsSortMode.None);

        foreach (var target in targets)
        {
            if (target.OwnerClientId == MyClientId)
            {
                MyTarget = target;
                continue;
            }

            if (target.IsDead == true)
                continue;

            avalableTargets.Add(target.OwnerClientId, target);
        }
    }

    private void TrySwitchToNextTarget()
    {
        var validTargets = GetValidTargetsIds();

        if (validTargets.Count == 0)
        {
            OnNoTargetsAvailable();
            return;
        }

        int currentIndex = validTargets.IndexOf(spectatedClientId);
        int nextIndex = (currentIndex + 1) % validTargets.Count;

        spectatedClientId = validTargets[nextIndex];
        ApplySpectatorTarget(spectatedClientId);
    }

    private void TrySwitchToPreviousTarget()
    {
        var validTargets = GetValidTargetsIds();

        if (validTargets.Count == 0)
        {
            OnNoTargetsAvailable();
            return;
        }

        int currentIndex = validTargets.IndexOf(spectatedClientId);
        int prevIndex = (currentIndex - 1 + validTargets.Count) % validTargets.Count;

        spectatedClientId = validTargets[prevIndex];
        ApplySpectatorTarget(spectatedClientId);
    }

    private List<ulong> GetValidTargetsIds()
    {
        return clientsIds
            .Where(id => id != MyClientId && avalableTargets.ContainsKey(id))
            .ToList();
    }

    private void ApplySpectatorTarget(ulong clientId)
    {
        if (MyTarget == null)
        {
            Debug.LogError("SpectatorManager: MyTarget is null in ApplySpectatorTarget!");
            return;
        }

        if (avalableTargets.TryGetValue(clientId, out SpectatorTarget target))
        {
            if (spectatedTarget == null)
            {
                // Первый вход в режим спектатора — выключаем свою камеру и переключаем UI
                MyTarget.Components.UI.SetWindow(GameUIWindowType.Spectator);
                EnableTargetCamera(MyTarget, false);
            }
            else
            {
                // Переключение между целями — выключаем предыдущую
                EnableTargetCamera(spectatedTarget, false);
                EnableTargetUsername(spectatedTarget, true);
            }

            spectatedTarget = target;

            EnableTargetCamera(target, true);
            EnableTargetUsername(target, false);
            SetTargetName(target.TargetName);
        }
        else
        {
            Debug.LogWarning($"SpectatorManager: Target for clientId {clientId} not found in avalableTargets.");
        }
    }

    private void SetTargetName(string targetName)
    {
        MyTarget.Components.UI.SpectatorWindow.SetTargetName(targetName);
    }

    private void EnableTargetCamera(SpectatorTarget target, bool enable)
    {
        if (target == null || target.Components == null) return;

        target.Components.MainCamera.enabled = enable;
        target.Components.AudioListener.enabled = enable;
    }

    private void EnableTargetUsername(SpectatorTarget target, bool enable)
    {
        if (target == null || target.Components == null) return;

        target.Components.UI.UsernameUI.EnableVisibility(enable);
    }

    private void OnNoTargetsAvailable()
    {
        CallEndOfGame_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void CallEndOfGame_ServerRpc()
    {
        foreach (var player in playerSpawner.SpawnedPlayers.Values)
        {
            if (player == null) continue;

            var components = player.GetComponent<PlayerComponents>();
            if (components != null)
            {
                components.UI.SetGameLoseUI_OwnerRpc();
            }
        }
    }
}