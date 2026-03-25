using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyWindow : MainMenuWindow
{
    [Header("Player List")]
    [SerializeField] private TMP_Text playerListHeader;
    [SerializeField] private Transform playerEntryContent;
    [SerializeField] private GameObject playerEntryPrefab;

    [Header("Buttons")]
    [SerializeField] private UIButton readyButton;
    private CanvasGroup readyButtonCanvas;
    [SerializeField] private UIButton beginGameButton;
    private CanvasGroup beginGameButtonCanvas;
    [SerializeField] private UIButton exitButton;
    [SerializeField] private TextMeshProUGUI playerListText;

    [Header("Content")]
    [SerializeField] private StanceCard stanceCard;
    [SerializeField] private Transform stanceButtonsRoot;
    [SerializeField] private GameObject stanceButtonPrefab;
    [SerializeField] private ToggleGroup toggleGroup;
    [Space]
    [SerializeField] private List<StanceInfo> stanceInfos;

    private void Start()
    {
        exitButton.onClick.AddListener(Disconnect);
        readyButton.onClick.AddListener(Ready);
        beginGameButton.onClick.AddListener(BeginGame);

        readyButtonCanvas = readyButton.GetComponent<CanvasGroup>();
        beginGameButtonCanvas = beginGameButton.GetComponent<CanvasGroup>();  
    }

    private void OnEnable()
    {
        GameNetworkManager.Instance.OnPlayerConnected += Lobby_OnPlayerConnected;
        GameNetworkManager.Instance.OnPlayerDisconnected += Lobby_OnPlayerDisconnected;
    }

    private void OnDisable()
    {
        GameNetworkManager.Instance.OnPlayerConnected -= Lobby_OnPlayerConnected;
        GameNetworkManager.Instance.OnPlayerDisconnected -= Lobby_OnPlayerDisconnected;
    }

    public override void SetWindowActive(bool active, float timeToSwitch = 0.1f)
    {
        if (active)
        {
            SetStanceButtons();

            UpdateAll();
        }

        base.SetWindowActive(active, timeToSwitch);
    }

    private void Lobby_OnPlayerConnected(ulong id)
    {
        UpdateAll();
    }

    private void Lobby_OnPlayerDisconnected(ulong id)
    {
        UpdateAll();
    }

    private void Disconnect()
    {
        GameNetworkManager.Instance.DisconnectFromLobby();
    }

    private void Ready()
    {
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkObject>();
        localPlayer.ToggleReady_ServerRpc();
    }

    private void BeginGame()
    {
        GameNetworkManager.Instance.StartGame();
    }

    private void UpdatePlayerList()
    {
        if (playerEntryContent == null)
        {
            Debug.Log("I Hate This Shit");
            return;
        }

        for (int i = playerEntryContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = playerEntryContent.GetChild(i).gameObject;
            Destroy(child);
        }

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            CreatePlayerEntry(clientId);
        }
    }

    private void CreatePlayerEntry(ulong id)
    {
        GameObject entryObj = Instantiate(playerEntryPrefab, playerEntryContent);
        LobbyPlayerEntry entry = entryObj.GetComponent<LobbyPlayerEntry>();

        entry.Setup(id);
        entry.OnPlayerStateChange += Entry_OnPlayerStateChange;
    }

    private void Entry_OnPlayerStateChange()
    {
        UpdateReadyButtonVisibility();
        UpdateBeginGameVisibility();
    }

    private void UpdateListHeader()
    {
        playerListHeader.text = $"Игроков: {NetworkManager.Singleton.ConnectedClients.Count}/{GameNetworkManager.Instance.MaxPlayers}";
    }

    private void SetStanceButtons()
    {
        foreach (Transform child in stanceButtonsRoot)
        {
            Destroy(child.gameObject);
        }

        toggleGroup.allowSwitchOff = true;
        stanceCard.SetupCard(null);
        stanceCard.SetStaticState(true);

        foreach (var stance in stanceInfos)
        {
            GameObject buttonObj = Instantiate(stanceButtonPrefab, stanceButtonsRoot);
            StanceToggle button = buttonObj.GetComponent<StanceToggle>();
            button.Setup(stance, toggleGroup);

            button.OnStanceChoose += StanceToggle_OnStanceChoose;
        }
    }

    private void StanceToggle_OnStanceChoose(StanceInfo info)
    {
        if (toggleGroup.allowSwitchOff)
            toggleGroup.allowSwitchOff = false;

        stanceCard.SetupCard(info, false);

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkObject>();
        localPlayer.SetPlayerStance_ServerRpc(info.Type);
    }

    private void UpdateReadyButtonVisibility()
    {
        if (NetworkManager.Singleton.LocalClient == null)
        {
            EnableReady(false);
            return;
        }

        if (NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            EnableReady(false);
            return;
        }

        PlayerNetworkObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkObject>();

        if (localPlayer.Stance.Value != StanceType.None)
        {
            EnableReady(true);
        }
        else
        {
            EnableReady(false);
        }
    }

    private void UpdateBeginGameVisibility()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            EnableBeginGame(false);
            return;
        }

        var players = FindObjectsByType<PlayerNetworkObject>(FindObjectsSortMode.None);

        if (players.Length == 2)
        {
            bool allReady = true;
            foreach (var p in players)
            {
                if (!p.IsReady.Value)
                    allReady = false;
            }

            EnableBeginGame(allReady);
        }
        else
        {
            EnableBeginGame(false);
        }
    }

    private void EnableBeginGame(bool enable)
    {
        beginGameButtonCanvas.alpha = enable ? 1 : 0;
        beginGameButtonCanvas.blocksRaycasts = enable;
        beginGameButtonCanvas.interactable = enable;
    }

    private void EnableReady(bool enable)
    {
        readyButtonCanvas.alpha = enable ? 1 : 0;
        readyButtonCanvas.blocksRaycasts = enable;
        readyButtonCanvas.interactable = enable;
    }

    private void UpdateAll()
    {
        UpdateListHeader();
        UpdatePlayerList();

        UpdateReadyButtonVisibility();
        UpdateBeginGameVisibility();
    }
}
