using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class MenuSingleplayerGameWindow : MainMenuWindow
{
    [Header("Buttons")]
    [SerializeField] private UIButton beginGameButton;
    [SerializeField] private UIButton exitButton;

    [Header("Settings")]
    [SerializeField] private Toggle oneLifeToggle;
    [Space]
    [SerializeField] private TMP_Text enemyHealthMultValue;
    [SerializeField] private Slider enemyHealthMult;
    [Space]
    [SerializeField] private TMP_Text enemyDamageMultValue;
    [SerializeField] private Slider enemyDamageMult;

    [Header("Content")]
    [SerializeField] private StanceCard stanceCard;
    [SerializeField] private Transform stanceButtonsRoot;
    [SerializeField] private GameObject stanceButtonPrefab;
    [SerializeField] private ToggleGroup toggleGroup;
    [Space]
    [SerializeField] private List<StanceInfo> stanceInfos;

    private StanceType currentStance = StanceType.None;

    private void Start()
    {
        beginGameButton.onClick.AddListener(DoBeginGame);
        exitButton.onClick.AddListener(Exit);

        enemyHealthMult.onValueChanged.AddListener(EnemyHealthMultChanged);
        enemyDamageMult.onValueChanged.AddListener(EnemyDamageMultChanged);

        oneLifeToggle.onValueChanged.AddListener(OneLifeToggleChanged);

        ResetGameSettings();
    }


    #region UI Events

    private void OneLifeToggleChanged(bool value)
    {
        SetStanceButtons();
    }

    private void EnemyDamageMultChanged(float value)
    {
        enemyDamageMultValue.text = $"{MathF.Round(value, 2)} X";
    }

    private void EnemyHealthMultChanged(float value)
    {
        enemyHealthMultValue.text = $"{MathF.Round(value, 2)} X";
    }

    private void DoBeginGame()
    {
        if (currentStance == StanceType.None)
        {
            Debug.LogError("Одиночная игра: не выбрана стойка!");
            return;
        }

        var listener = FindAnyObjectByType<AudioListener>();
        listener.enabled = false;

        StartGameData.Stance = currentStance;
        StartGameData.SingleplayerOneLife = oneLifeToggle.isOn;
        EnemyManager.Instance.SetSingleplayerHealthMult(enemyHealthMult.value);
        EnemyManager.Instance.SetSingleplayerDamageMult(enemyDamageMult.value);

        LoadManager.Instance.StartSingleplayerGame();
    }

    private void Exit()
    {
        mainMenu.SetMainMenuWindow(MainMenuWindowType.Gamemode);
    }

    #endregion

    public override void SetWindowActive(bool active, float timeToSwitch = 0.1f)
    {
        if (!active)
        {
            SetStanceButtons();
        }
        else
        {
            ResetGameSettings();
        }

        base.SetWindowActive(active, timeToSwitch);
    }

    private void SetStanceButtons()
    {
        currentStance = StanceType.None;
        StartGameData.Stance = currentStance;

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

        stanceCard.SetupCard(info, !oneLifeToggle.isOn);
        currentStance = info.Type;
    }

    private void ResetGameSettings()
    {
        currentStance = StanceType.None;
        StartGameData.Stance = currentStance;

        oneLifeToggle.isOn = false;
        enemyHealthMult.value = 1f;
        enemyDamageMult.value = 1f;
    }
}
