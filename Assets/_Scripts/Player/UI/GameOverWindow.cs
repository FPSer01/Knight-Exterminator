using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverWindow : PlayerUIWindow
{
    [Space]
    [SerializeField] private TMP_Text gameOverStatusText;
    [SerializeField] private TMP_Text gameOverStatusSubText;
    [SerializeField] private UIButton retryButton;
    [SerializeField] private UIButton exitToMenuButton;
    [SerializeField] private UIButton exitGameButton;

    private void Start()
    {
        retryButton.onClick.AddListener(RetryGame);
        exitToMenuButton.onClick.AddListener(ExitToMenu);
        exitGameButton.onClick.AddListener(ExitGame);
    }

    private void ExitGame()
    {
        Application.Quit();
    }

    private void ExitToMenu()
    {
        GameNetworkManager.Instance.DisconnectFromGame();
    }

    private void RetryGame()
    {
        LoadManager.Instance.RetryGame();
    }

    public void SetGameOverStatus(GameOverStatus status)
    {
        bool isTutorial = SceneManager.GetActiveScene().buildIndex == GameScenes.TUTORIAL;

        switch (status)
        {
            case GameOverStatus.Defeat:
                gameOverStatusText.text = "<color=#EE0000>Поражение!</color>";
                gameOverStatusSubText.text = isTutorial ? "Как это случилось?" : "Может получится в другой раз...";
                break;
            case GameOverStatus.Victory:
                gameOverStatusText.text = isTutorial ? "Вы прошли обучение!" : "<color=#00EE00>Победа!</color>";
                gameOverStatusSubText.text = isTutorial ? "Теперь можно играть на полную >:)" : "Вот теперь можно выпить чаю! (или же нет)";
                break;
        }
    }
}
