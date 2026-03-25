using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSettingsWindow : PlayerUIWindow
{
    [Header("References")]
    [SerializeField] private SettingsController settings;

    [Header("UI")]
    [SerializeField] private TMP_Text sensitivityValue;
    [SerializeField] private Slider sensitivitySlider;
    [Space]
    [SerializeField] private TMP_Text masterVolumeValue;
    [SerializeField] private Slider masterVolumeSlider;
    [Space]
    [SerializeField] private TMP_Text musicVolumeValue;
    [SerializeField] private Slider musicVolumeSlider;
    [Space]
    [SerializeField] private TMP_Text sfxVolumeValue;
    [SerializeField] private Slider sfxVolumeSlider;
    [Space]
    [SerializeField] private UIButton saveAndCloseButton;

    private void Start()
    {
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChange);
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChange);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChange);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChange);

        saveAndCloseButton.onClick.AddListener(SaveAndCloseWindow);
    }

    private void OnSFXVolumeChange(float value)
    {
        SetValueText(sfxVolumeValue, value);
        settings.SetSFXVolume(value);
    }

    private void OnMusicVolumeChange(float value)
    {
        SetValueText(musicVolumeValue, value);
        settings.SetMusicVolume(value);
    }

    private void OnMasterVolumeChange(float value)
    {
        SetValueText(masterVolumeValue, value);
        settings.SetMasterVolume(value);
    }

    private void OnSensitivityChange(float value)
    {
        sensitivityValue.text = $"{MathF.Round(value, 2)}";
        settings.SetCameraSensitivity(value);
    }

    // Подавать значения value от 0 до 1
    private void SetValueText(TMP_Text textElement, float value)
    {
        value = Mathf.Clamp01(value);
        textElement.text = $"{Mathf.Round(value * 100)}";
    }

    private void UpdateUI()
    {
        sensitivityValue.text = $"{settings.SettingsData.Sensitivity}";
        sensitivitySlider.value = settings.SettingsData.Sensitivity;

        SetValueText(masterVolumeValue, settings.SettingsData.MasterVolume);
        masterVolumeSlider.value = settings.SettingsData.MasterVolume;

        SetValueText(musicVolumeValue, settings.SettingsData.MusicVolume);
        musicVolumeSlider.value = settings.SettingsData.MusicVolume;

        SetValueText(sfxVolumeValue, settings.SettingsData.SFXVolume);
        sfxVolumeSlider.value = settings.SettingsData.SFXVolume;
    }

    private void SaveAndCloseWindow()
    {
        playerUI.SetWindow(GameUIWindowType.Menu);
    }

    public override void SetWindowActive(bool active, float timeToSwitch = 0.25f)
    {
        if (active)
        {
            settings.LoadSettingsData();
            UpdateUI();
        }
        else
        {
            settings.SaveSettingsData();
        }

        base.SetWindowActive(active, timeToSwitch);
    }
}
