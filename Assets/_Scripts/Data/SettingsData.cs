using UnityEngine;

[CreateAssetMenu(fileName = "SettingsData", menuName = "Data/SettingsData")]
public class SettingsData : ScriptableObject
{
    public const string SENSITIVITY = "Sensitivity";
    public const string MASTER_VOLUME = "MasterVolume";
    public const string MUSIC_VOLUME = "MusicVolume";
    public const string SFX_VOLUME = "SFXVolume";

    [Range(0.1f, 10f)]
    [SerializeField] private float sensitivity = 5f;

    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f; // От 0 до 1
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f; // От 0 до 1
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f; // От 0 до 1

    public float Sensitivity { get => sensitivity; set => sensitivity = value; }
    public float MasterVolume { get => masterVolume; set => masterVolume = value; }
    public float MusicVolume { get => musicVolume; set => musicVolume = value; }
    public float SFXVolume { get => sfxVolume; set => sfxVolume = value; }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(SENSITIVITY, Sensitivity);
        PlayerPrefs.SetFloat(MASTER_VOLUME, MasterVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME, MusicVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME, SFXVolume);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey(SENSITIVITY)) Sensitivity = PlayerPrefs.GetFloat(SENSITIVITY);
        if (PlayerPrefs.HasKey(MASTER_VOLUME)) MasterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME);
        if (PlayerPrefs.HasKey(MUSIC_VOLUME)) MusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME);
        if (PlayerPrefs.HasKey(SFX_VOLUME)) SFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME);
    }
}
