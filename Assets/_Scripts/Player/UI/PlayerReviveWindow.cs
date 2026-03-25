using DG.Tweening;
using TMPro;
using UnityEngine;

public class PlayerReviveWindow : PlayerUIWindow
{
    [Space]
    [SerializeField] private TMP_Text reviveSubText;
    [SerializeField] private UIButton reviveButton;
    [SerializeField] private UIButton exitToMenuButton;
    [SerializeField] private UIButton exitGameButton;
    [SerializeField] private bool showSubText = true;

    [Header("Player Health")]
    [SerializeField] private PlayerHealth playerHealth;

    private void Start()
    {
        reviveButton.onClick.AddListener(Revive);
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

    private void Revive()
    {
        playerUI.SetWindow(GameUIWindowType.HUD);
        playerHealth.RevivePlayer();
    }

    public override void SetWindowActive(bool active, float timeToSwitch = 0.1f)
    {
        if (active)
        {
            SetSubText();
        }

        base.SetWindowActive(active, timeToSwitch);
    }

    private void SetSubText()
    {
        if (!showSubText)
        {
            reviveSubText.text = "";
            return;
        }

        string color = "#FF0000";

        if (playerHealth.CurrentReviveAmount > 0)
        {
            color = "#00FF00";
        }

        reviveSubText.text = $"Осталось возрождений: <color={color}>{playerHealth.CurrentReviveAmount}</color>";
    }
}
