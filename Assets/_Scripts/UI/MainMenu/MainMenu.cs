using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private UIButton beginGameButton;
    [SerializeField] private UIButton settingsButton;
    [SerializeField] private UIButton exitGameButton;
    [SerializeField] private UIButton beginTutorialButton;

    [Header("Windows")]
    [SerializeField] private MainMenuWindow mainWindow;
    [SerializeField] private MenuGamemodeWindow gamemodeWindow;
    [SerializeField] private MenuSettingsWindow settingsWindow;
    [SerializeField] private MenuSingleplayerGameWindow singleplayerGameWindow;
    [SerializeField] private MenuMultiplayerGameWindow multiplayerGameWindow;
    [SerializeField] private LobbyWindow lobbyWindow;

    private MainMenuWindowType currentWindow;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    private void Start()
    {
        SetMainMenuWindow(MainMenuWindowType.Main);

        beginGameButton.onClick.AddListener(BeginGame);
        settingsButton.onClick.AddListener(SetSettingsWindow);
        exitGameButton.onClick.AddListener(ExitGame);
        beginTutorialButton.onClick.AddListener(LoadTutorial);
    }

    private void LoadTutorial()
    {
        if (LoadManager.Instance != null)
            LoadManager.Instance.LoadTutorial();
    }

    private void ExitGame()
    {
        Application.Quit();
    }

    public void BeginGame()
    {
        SetMainMenuWindow(MainMenuWindowType.Gamemode);
    }

    private void SetSettingsWindow()
    {
        SetMainMenuWindow(MainMenuWindowType.Settings);
    }

    public void SetMainMenuWindow(MainMenuWindowType type)
    {
        currentWindow = type;

        mainWindow.SetWindowActive(type == MainMenuWindowType.Main);
        gamemodeWindow.SetWindowActive(type == MainMenuWindowType.Gamemode);
        settingsWindow.SetWindowActive(type == MainMenuWindowType.Settings);

        singleplayerGameWindow.SetWindowActive(type == MainMenuWindowType.SingleplayerGame);
        multiplayerGameWindow.SetWindowActive(type == MainMenuWindowType.MultiplayerGame);
        lobbyWindow.SetWindowActive(type == MainMenuWindowType.Lobby);
    }

    private void OnDestroy()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
    }
}
