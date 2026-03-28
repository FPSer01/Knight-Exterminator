using TMPro;
using UnityEngine;

public class MenuMultiplayerGameWindow : MainMenuWindow
{
    private const string SAVED_USERNAME = "SavedUsername";
    private const string SAVED_HOST_PORT = "SavedHostPort";
    private const string SAVED_JOIN_IP = "SavedJoinIp";
    private const string SAVED_JOIN_PORT = "SavedJoinPort";

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField hostPortInput;
    [SerializeField] private TMP_InputField joinIpInput;
    [SerializeField] private TMP_InputField joinPortInput;

    [Header("Buttons")]
    [SerializeField] private UIButton hostButton;
    [SerializeField] private UIButton joinButton;
    [SerializeField] private UIButton exitButton;

    private void Start()
    {
        hostButton.onClick.AddListener(Host);
        joinButton.onClick.AddListener(Join);
        exitButton.onClick.AddListener(Exit);

        usernameInput.onEndEdit.AddListener(SaveUsername);
        hostPortInput.onEndEdit.AddListener(SaveHostPort);
        joinIpInput.onEndEdit.AddListener(SaveJoinIp);
        joinPortInput.onEndEdit.AddListener(SaveJoinPort);
    }

    #region Save and Load Data

    private void SaveUsername(string username)
    {
        PlayerPrefs.SetString(SAVED_USERNAME, username);
        PlayerPrefs.Save();
    }

    private void SaveHostPort(string hostPort)
    {
        PlayerPrefs.SetString(SAVED_HOST_PORT, hostPort);
        PlayerPrefs.Save();
    }

    private void SaveJoinIp(string joinIp)
    {
        PlayerPrefs.SetString(SAVED_JOIN_IP, joinIp);
        PlayerPrefs.Save();
    }

    private void SaveJoinPort(string joinPort)
    {
        PlayerPrefs.SetString(SAVED_JOIN_PORT, joinPort);
        PlayerPrefs.Save();
    }

    private void LoadAllData()
    {
        if (PlayerPrefs.HasKey(SAVED_USERNAME))
        {
            string username = PlayerPrefs.GetString(SAVED_USERNAME);
            usernameInput.text = username;
        }

        if (PlayerPrefs.HasKey(SAVED_HOST_PORT))
        {
            string hostPort = PlayerPrefs.GetString(SAVED_HOST_PORT);
            hostPortInput.text = hostPort;
        }

        if (PlayerPrefs.HasKey(SAVED_JOIN_IP))
        {
            string joinIp = PlayerPrefs.GetString(SAVED_JOIN_IP);
            joinIpInput.text = joinIp;
        }

        if (PlayerPrefs.HasKey(SAVED_JOIN_PORT))
        {
            string joinPort = PlayerPrefs.GetString(SAVED_JOIN_PORT);
            joinPortInput.text = joinPort;
        }
    }

    #endregion

    private void Exit()
    {
        mainMenu.SetMainMenuWindow(MainMenuWindowType.Gamemode);
    }

    private void Join()
    {
        if (IsUsernameEntered() && ushort.TryParse(joinPortInput.text, out ushort port))
        {
            bool success = GameNetworkManager.Instance.StartClient(usernameInput.text, joinIpInput.text, port);

            if (!success)
            {
                Debug.LogError("Ошибка при подключении.");
                return;
            }

            LoadManager.Instance.SetConnectionScreenActive(true);
        }
    }

    private void Host()
    {
        if (IsUsernameEntered() && ushort.TryParse(hostPortInput.text, out ushort port))
        {
            bool success = GameNetworkManager.Instance.StartHost(usernameInput.text, port);

            if (!success)
            {
                Debug.LogError("Ошибка при создании сервера.");
                return;
            }

            mainMenu.SetMainMenuWindow(MainMenuWindowType.Lobby);
        }
    }

    private bool IsUsernameEntered()
    {
        return usernameInput.text.Length > 0 && !string.IsNullOrWhiteSpace(usernameInput.text);
    }

    public override void SetWindowActive(bool active, float timeToSwitch = 0.1F)
    {
        if (active)
        {
            LoadAllData();
        }

        base.SetWindowActive(active, timeToSwitch);
    }
}
