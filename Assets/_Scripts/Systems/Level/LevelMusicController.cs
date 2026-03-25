using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class LevelMusicController : MonoBehaviour
{
    private static LevelMusicController instance;

    [Header("Music")]
    [SerializeField] private AudioMixer masterMixer;
    [Space]
    [SerializeField] private AudioSource outsideBattleMusicSource;
    [SerializeField] private List<AudioClip> outsideBattleMusic;
    [Space]
    [SerializeField] private AudioSource inBattleMusicSource;
    [SerializeField] private List<AudioClip> inBattleMusic;
    [Space]
    [Range(0f, 1f)] [SerializeField] private float musicMaxVolume;
    [SerializeField] private float musicFadeTime;

    public static LevelMusicController Instance { get => instance; }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);

        SetupMusic();
    }

    private void Start()
    {
        SetBattleMusic(false);

        PlayerUI.OnUIChange += PlayerUI_OnUIChange;
    }

    private void PlayerUI_OnUIChange(GameUIWindowType window)
    {
        MuffleAudio(window == GameUIWindowType.Menu || window == GameUIWindowType.Settings);
    }

    private void SetupMusic()
    {
        outsideBattleMusicSource.volume = 0;
        inBattleMusicSource.volume = 0;

        outsideBattleMusicSource.clip = GetRandomClip(outsideBattleMusic);
        inBattleMusicSource.clip = GetRandomClip(inBattleMusic);

        outsideBattleMusicSource.Play();
        inBattleMusicSource.Play();
    }

    public void SetBattleMusic(bool battleActive)
    {
        if (battleActive)
        {
            inBattleMusicSource.DOFade(musicMaxVolume, musicFadeTime);
            outsideBattleMusicSource.DOFade(0, musicFadeTime);
        }
        else
        {
            inBattleMusicSource.DOFade(0, musicFadeTime);
            outsideBattleMusicSource.DOFade(musicMaxVolume, musicFadeTime);
        }
    }

    public void MuffleAudio(bool muffle)
    {
        masterMixer.SetFloat("CutoffFreq", muffle ? 1000f : 22000f);
    }

    private AudioClip GetRandomClip(List<AudioClip> list)
    {
        int index = Random.Range(0, list.Count);
        return list[index];
    }

    private void OnDestroy()
    {
        PlayerUI.OnUIChange -= PlayerUI_OnUIChange;
        MuffleAudio(false);
    }
}
