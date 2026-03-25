using System;
using UnityEngine;

public class PlayerMenuWindow : PlayerUIWindow
{
    [Header("UI")]
    [SerializeField] private UIButton resumeButton;
    [SerializeField] private UIButton settingsButton;
    [SerializeField] private UIButton retryButton;
    [SerializeField] private UIButton exitToMenuButton;
    [SerializeField] private UIButton exitGameButton;

    private void Start()
    {
        resumeButton.onClick.AddListener(ResumeGame);
        settingsButton.onClick.AddListener(OpenSettings);
        retryButton.onClick.AddListener(Retry);
        exitToMenuButton.onClick.AddListener(ExitToMenu);
        exitGameButton.onClick.AddListener(ExitGame);
    }

    private void Retry()
    {
        LoadManager.Instance.RetryGame();
    }

    private void ExitGame()
    {
        Application.Quit();
    }

    private void ExitToMenu()
    {
        GameNetworkManager.Instance.DisconnectFromGame();
    }

    private void OpenSettings()
    {
        playerUI.SetWindow(GameUIWindowType.Settings);
    }

    private void ResumeGame()
    {
        playerUI.SetWindow(GameUIWindowType.HUD);
    }
}
