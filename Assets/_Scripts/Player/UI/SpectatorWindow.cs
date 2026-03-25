using TMPro;
using UnityEngine;

public class SpectatorWindow : PlayerUIWindow
{
    [Header("Spectator UI")]
    [SerializeField] private TMP_Text playerNameText;

    public override void SetWindowActive(bool active, float timeToSwitch = 0.1F)
    {
        base.SetWindowActive(active, timeToSwitch);


    }

    public void SetTargetName(string targetName)
    {
        playerNameText.text = $"Наблюдение за: {targetName}";
    }
}
