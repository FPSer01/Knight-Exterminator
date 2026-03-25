using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : NetworkManager
{
    private const string ERR_SERVER_FULL = "Сервер переполнен.";
    private const string ERR_LOST_OR_NA = "Сервер не найден или отключен.";
    private const string ERR_GAME_BEGUN = "Нельзя подключиться во время игры. Игровая сессия уже запущена.";
    private const string ERR_SERVER_DOWN = "Сервер отключен. Хост завершил сеанс.";

    private const string USERNAME_DUBLICATE_FORMAT = "{0} ({1})";

    [SerializeField] private ushort maxPlayers = 2;
    [SerializeField] private bool multiplayerDebugMode = false;

    private Dictionary<ulong, string> clientNames = new();

    private bool blockDisconnectHandlers = false;

    #region Public Fields (Public Interface)

    public event Action<ulong> OnPlayerConnected;
    public event Action<ulong> OnPlayerDisconnected;

    private void DoPlayerConnected(ulong clientId) { OnPlayerConnected?.Invoke(clientId); }
    private void DoPlayerDisconnected(ulong clientId) { OnPlayerDisconnected?.Invoke(clientId); }

    public static GameNetworkManager Instance { get; private set; }
    public UnityTransport Transport { get; private set; }
    public NetworkGameData GameData { get; private set; }
    public ushort MaxPlayers { get => maxPlayers; }
    public GameState GameState { get => NetworkGameData.Instance.GameState.Value; private set => NetworkGameData.Instance.GameState.Value = value; }

    #endregion

    #region Unity API

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (Singleton != null)
            Destroy(gameObject);

        Transport = GetComponent<UnityTransport>();
        GameData = GetComponent<NetworkGameData>();
    }

    private void Start()
    {
        Singleton.ConnectionApprovalCallback = ApprovalCheck;

        Singleton.OnClientConnectedCallback += OnClientConnected;
        Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (Singleton != null)
        {
            Singleton.ConnectionApprovalCallback -= ApprovalCheck;

            Singleton.OnClientConnectedCallback -= OnClientConnected;
            Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            if (Singleton.gameObject == gameObject)
            {
                Singleton.Shutdown();
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (Singleton != null)
        {
            Singleton.Shutdown();
        }
    }

    #endregion

    #region Connection Events

    private void ApprovalCheck(ConnectionApprovalRequest request, ConnectionApprovalResponse response)
    {
        if (Singleton.ConnectedClientsIds.Count == 1 && StartGameData.GameMode == Gamemode.Singleplayer)
        {
            response.Approved = false;
            return;
        }

        if (Singleton.ConnectedClientsIds.Count >= maxPlayers)
        {
            response.Approved = false;
            response.Reason = ERR_SERVER_FULL;
            return;
        }

        if (GameState == GameState.InGame)
        {
            response.Approved = false;
            response.Reason = ERR_GAME_BEGUN;
            return;
        }

        string name = "Player";
        if (request.Payload != null && request.Payload.Length > 0)
        {
            name = Encoding.UTF8.GetString(request.Payload);
        }

        AddClientListName(request.ClientNetworkId, name);

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Pending = false;

        ulong id = request.ClientNetworkId;

        if (Singleton.IsServer)
        {
            switch (StartGameData.GameMode)
            {
                case Gamemode.Singleplayer:
                    Debug.Log($"Connected to [{LogTags.GREEN_COLOR}Singleplayer{LogTags.END_COLOR}]");
                    break;

                case Gamemode.Multiplayer: 
                    Debug.Log($"Connection Approved for [{LogTags.GREEN_COLOR}{clientNames[id]}{LogTags.END_COLOR}] with Cliend Id: [{id}].");
                    break;
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!Singleton.IsServer && clientId == Singleton.LocalClientId)
        {
            LoadManager.Instance.SetConnectionScreenActive(false);
            MainMenu.Instance.SetMainMenuWindow(MainMenuWindowType.Lobby);
        }

        if (Singleton.IsServer)
        {
            if (Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                PlayerNetworkObject pno = null;

                foreach (var netObj in client.OwnedObjects)
                {
                    pno = netObj.GetComponent<PlayerNetworkObject>();

                    if (pno != null) break;
                }

                if (clientNames.TryGetValue(clientId, out string playerName))
                {
                    pno.PlayerName.Value = playerName;
                }
            }

            Debug.Log($"Connected Client Id: [{clientId}]. Clients: {Singleton.ConnectedClientsIds.Count}/{maxPlayers}.");
        }

        DoPlayerConnected(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        HandleDisconnectReason(clientId);

        if (clientId == 0 || Singleton.LocalClientId == clientId)
        {
            DetermineExitBehaviour();
        }

        if (Singleton.IsHost)
        {
            clientNames.Remove(clientId);
            Debug.Log($"Disconnected client id: {clientId}. Clients: {Singleton.ConnectedClientsIds.Count}/{maxPlayers}.");
        }

        DoPlayerDisconnected(clientId);

        blockDisconnectHandlers = false;
    }

    private void HandleDisconnectReason(ulong clientId)
    {
        if (Singleton.IsServer || blockDisconnectHandlers)
        {
            return;
        }

        if (clientId == Singleton.LocalClientId)
        {
            string reason = Singleton.DisconnectReason;

            if (string.IsNullOrEmpty(reason))
            {
                LoadManager.Instance.ShowConnectionErrorMessage(ERR_LOST_OR_NA);
                Debug.LogError($"[{LogTags.RED_COLOR}DISCONNECT REASON{LogTags.END_COLOR}]: EMPTY");
            }
            else
            {
                LoadManager.Instance.ShowConnectionErrorMessage(reason);
                Debug.LogError($"[{LogTags.RED_COLOR}DISCONNECT REASON{LogTags.END_COLOR}]: {reason}");
            }
        }
    }

    #endregion

    private void AddClientListName(ulong id, string name)
    {
        string finalName = name;
        int counter = 1;

        while (clientNames.ContainsValue(finalName))
        {
            finalName = string.Format(USERNAME_DUBLICATE_FORMAT, name, counter);
            counter++;
        }

        clientNames.Add(id, finalName);
    }

    public bool TryGetStoredPlayerName(ulong clientId, out string playerName)
    {
        return clientNames.TryGetValue(clientId, out playerName);
    }

    #region Connection Methods

    public bool StartHost(string username, ushort port)
    {
        string ip = "0.0.0.0";
#if UNITY_EDITOR
        ip = "127.0.0.1";
#endif

        Transport.SetConnectionData(ip, port);
        Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(username);

        return Singleton.StartHost();
    }

    public bool StartClient(string username, string ip, ushort port)
    {
        Transport.SetConnectionData(ip, port);
        Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(username);

        return Singleton.StartClient();
    }

    public void StartGame()
    {
        if (!Singleton.IsServer)
            return;

        GameState = GameState.InGame;
        LoadManager.Instance.StartMultiplayerGame(multiplayerDebugMode);
    }

    public void DisconnectFromLobby()
    {
        ExecuteDisconnectFromLobby(true);
    }

    private void ExecuteDisconnectFromLobby(bool blockDisconnectReason = false)
    {
        Singleton.Shutdown();
        blockDisconnectHandlers = blockDisconnectReason;
    }

    public void DisconnectFromGame()
    {
        ExecuteDisconnectFromGame(true);
    }

    private void ExecuteDisconnectFromGame(bool blockDisconnectReason = false)
    {
        if (!Singleton.IsConnectedClient)
            return;

        Singleton.Shutdown();
        blockDisconnectHandlers = blockDisconnectReason;
    }

    #endregion

    private void DetermineExitBehaviour()
    {
        Singleton.Shutdown();

        Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        if (activeScene.buildIndex == GameScenes.MAIN_MENU)
        {
            MainMenu.Instance.SetMainMenuWindow(MainMenuWindowType.MultiplayerGame);
        }
        else
        {
            LoadManager.Instance.ExitToMenu();
        }
    }
}
