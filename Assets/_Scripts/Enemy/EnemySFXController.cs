using DG.Tweening;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemySFXController : NetworkBehaviour
{
    [Header("Sources")]
    [SerializeField] protected AudioSource oneShotSource;
    [SerializeField] protected AudioSource additionalSource;
    protected float additionalSourceVolume;

    [Header("Move")]
    [SerializeField] protected SFXMultisampleCollection moveSFX;

    [Header("Hurt")]
    [SerializeField] protected List<AudioClip> hurtSFX;

    [Header("Attack")]
    [SerializeField] protected List<AudioClip> attackSFX;

    [Header("Death")]
    [Range(0f, 1f)]
    [SerializeField] private float deathSFXChance = 0.2f;
    [SerializeField] protected List<AudioClip> deathSFX;

    [Header("Custom")]
    [SerializeField] private List<SFXCollection> customSFX;

    public override void OnNetworkSpawn()
    {
        additionalSourceVolume = additionalSource.volume;

        SetRandomMoveSFX();
    }

    public void PlayOneShot(AudioClip clip)
    {
        oneShotSource.PlayOneShot(clip);
    }

    public void PlayOneShot(AudioClip clip, float volume)
    {
        oneShotSource.PlayOneShot(clip, volume);
    }

    #region General

    protected void StartPlayMultisampleCollection(SFXMultisampleCollection sfxSettings, bool loop = false)
    {
        AudioSource source = sfxSettings.Source;
        List<AudioClip> listSFX = sfxSettings.ListSFX;
        AudioClip parallelSFX = sfxSettings.AdditionalSFX;
        float inDuration = sfxSettings.inDuration;

        AudioClip sfxClip = GetRandomClip(listSFX);

        DOTween.Kill(source);
        source.volume = 0;
        source.DOFade(1, inDuration);

        source.clip = sfxClip;
        source.loop = loop;

        source.Play();

        if (additionalSource != null && parallelSFX != null)
        {
            DOTween.Kill(additionalSource);
            additionalSource.volume = 0;
            additionalSource.DOFade(additionalSourceVolume, inDuration);

            additionalSource.clip = parallelSFX;
            additionalSource.loop = loop;
            additionalSource.Play();
        }
    }

    protected void StopPlayMultisampleCollection(SFXMultisampleCollection sfxSettings)
    {
        AudioSource source = sfxSettings.Source;
        float outDuration = sfxSettings.outDuration;

        DOTween.Kill(source);
        source.DOFade(0, outDuration);

        source.loop = false;
        source.Stop();

        if (additionalSource != null)
        {
            DOTween.Kill(additionalSource);
            additionalSource.DOFade(0, outDuration);

            additionalSource.loop = false;
            additionalSource.Stop();
        }
    }

    public AudioClip GetRandomClip(List<AudioClip> list)
    {
        int index = Random.Range(0, list.Count);
        return list[index];
    }

    #endregion

    #region Move SFX

    public void SetRandomMoveSFX()
    {
        if (moveSFX.Source != null)
        {
            moveSFX.Source.clip = GetRandomClip(moveSFX.ListSFX);
        }
    }

    public void PlayMoveSFX(bool play)
    {
        ExecutePlayMoveSFX(play);
        PlayMoveSFX_NotOwnerRpc(play);
    }

    private void ExecutePlayMoveSFX(bool play)
    {
        if (moveSFX.Source == null)
            return;

        if (play)
        {
            StartPlayMultisampleCollection(moveSFX, true);
        }
        else
        {
            StopPlayMultisampleCollection(moveSFX);
        }
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayMoveSFX_NotOwnerRpc(bool play)
    {
        ExecutePlayMoveSFX(play);
    }

    #endregion

    #region Hurt SFX

    public void PlayHurtSFX(float volume = 1f)
    {
        ExecutePlayHurtSFX(volume);
        PlayHurtSFX_NotOwnerRpc(volume);
    }

    private void ExecutePlayHurtSFX(float volume = 1f)
    {
        var clip = GetRandomClip(hurtSFX);

        oneShotSource.PlayOneShot(clip, volume);
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayHurtSFX_NotOwnerRpc(float volume = 1f)
    {
        ExecutePlayHurtSFX(volume);
    }

    #endregion

    #region Attack SFX

    public void PlayAttackSFX(float volume = 1f)
    {
        ExecutePlayAttackSFX(volume);
        PlayAttackSFX_NotOwnerRpc(volume);
    }

    private void ExecutePlayAttackSFX(float volume = 1f)
    {
        var clip = GetRandomClip(attackSFX);

        oneShotSource.PlayOneShot(clip, volume);
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayAttackSFX_NotOwnerRpc(float volume = 1f)
    {
        ExecutePlayAttackSFX(volume);
    }

    #endregion

    #region Death SFX

    public void PlayDeathSFX(float volume = 1f)
    {
        float randomChance = Random.value;

        if (randomChance <= deathSFXChance)
        {
            var deathClip = GetRandomClip(deathSFX);
            CreateAudioObject(deathClip, volume);
        }

        var hurtClip = GetRandomClip(hurtSFX);
        CreateAudioObject(hurtClip, volume);
    }

    private void CreateAudioObject(AudioClip clip, float volume = 1f)
    {
        GameObject audioSourceObj = Instantiate(oneShotSource.gameObject, transform.position, Quaternion.identity);
        var audioSource = audioSourceObj.GetComponent<AudioSource>();

        float clipTime = clip.length;
        audioSource.PlayOneShot(clip, volume);

        Destroy(audioSourceObj, clipTime);
    }

    #endregion

    #region Custom SFX

    public void PlayCustomSFX(string tag)
    {
        PlayCustomSFX_EveryoneRpc(tag);
    }

    [Rpc(SendTo.Everyone)]
    public void PlayCustomSFX_EveryoneRpc(string tag)
    {
        ExecutePlayCustomSFX(tag);
    }

    private void ExecutePlayCustomSFX(string tag)
    {
        SFXCollection collection = customSFX.Find(c => c.Tag == tag);

        var randomValue = Random.value;

        if (randomValue <= collection.Chance)
        {
            if (collection.OneSFX != null)
            {
                PlayOneShot(collection.OneSFX, collection.Volume);
            }
            else if (collection.ListSFX.Count > 0)
            {
                var clip = GetRandomClip(collection.ListSFX);

                PlayOneShot(clip, collection.Volume);
            }
        }
    }

    #endregion
}
