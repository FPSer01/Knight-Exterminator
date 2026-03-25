using System;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.Audio;

public class SettingsController : MonoBehaviour
{
    public const float MIN_AUDIO_VALUE = -80f;
    public const float MAX_AUDIO_VALUE = 0f;

    [Header("Data Container")]
    [SerializeField] private SettingsData settingsData;

    [Header("References")]
    [SerializeField] private AudioMixer audioMixer;

    public SettingsData SettingsData { get => settingsData; }

    private void Start()
    {
        LoadSettingsData();
    }

    public void LoadSettingsData()
    {
        settingsData.LoadSettings();

        SetCameraSensitivity(settingsData.Sensitivity);
        SetMasterVolume(settingsData.MasterVolume);
        SetMusicVolume(settingsData.MusicVolume);
        SetSFXVolume(settingsData.SFXVolume);
    }

    public void SaveSettingsData()
    {
        settingsData.SaveSettings();
    }

    public void SetMasterVolume(float normalizedValue)
    {
        float volume = GetLogarithmicVolume(normalizedValue);
        audioMixer.SetFloat("MasterVolume", volume);
        settingsData.MasterVolume = normalizedValue;
    }

    public void SetMusicVolume(float normalizedValue)
    {
        float volume = GetLogarithmicVolume(normalizedValue);
        audioMixer.SetFloat("MusicVolume", volume);
        settingsData.MusicVolume = normalizedValue;
    }

    public void SetSFXVolume(float normalizedValue)
    {
        float volume = GetLogarithmicVolume(normalizedValue);
        audioMixer.SetFloat("SFXVolume", volume);
        settingsData.SFXVolume = normalizedValue;
    }

    public void SetCameraSensitivity(float value)
    {
        value = MathF.Round(value, 2);
        settingsData.Sensitivity = value;
    }

    private float GetLogarithmicVolume(float value)
    {
        if (value <= 0f)
            return MIN_AUDIO_VALUE;

        return Mathf.Log10(value) * 20f;
    }
}
