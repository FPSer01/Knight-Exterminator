using System;
using UnityEngine;

public class MenuGamemodeWindow : MainMenuWindow
{
    [Header("Buttons")]
    [SerializeField] private UIButton singleplayerButton;
    [SerializeField] private UIButton multiplayerButton;
    [SerializeField] private UIButton exitButton;

    private Gamemode currentGamemode = Gamemode.None;

    private void Start()
    {
        singleplayerButton.onClick.AddListener(SetSingleplayer);
        multiplayerButton.onClick.AddListener(SetMultiplayer);
        exitButton.onClick.AddListener(ExitToMenu);
    }

    private void ExitToMenu()
    {
        SetGamemode(Gamemode.None);
        mainMenu.SetMainMenuWindow(MainMenuWindowType.Main);
    }

    private void SetMultiplayer()
    {
        SetGamemode(Gamemode.Multiplayer);
        mainMenu.SetMainMenuWindow(MainMenuWindowType.MultiplayerGame);
    }

    private void SetSingleplayer()
    {
        SetGamemode(Gamemode.Singleplayer);
        mainMenu.SetMainMenuWindow(MainMenuWindowType.SingleplayerGame);
    }

    private void SetGamemode(Gamemode gamemode)
    {
        currentGamemode = gamemode;
        StartGameData.GameMode = currentGamemode;
    }
}
