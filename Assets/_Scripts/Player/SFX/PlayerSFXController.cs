using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerSFXController : NetworkBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource additionalSource;
    private float additionalSourceVolume;

    [Header("Walk and Run")]
    [SerializeField] private SFXMultisampleCollection walkSFXSettings;
    [Space]
    [SerializeField] private SFXMultisampleCollection runSFXSettings;

    [Header("Jump and Fall")]
    [SerializeField] private AudioClip mainJumpSFX;
    [SerializeField] private List<AudioClip> jumpSFX;
    [SerializeField] private AudioClip fallSFX;

    [Header("Attack")]
    [SerializeField] private List<AudioClip> attackSFX;

    [Header("Dodge")]
    [SerializeField] private List<AudioClip> dodgeSFX;

    [Header("Hurt")]
    [SerializeField] private List<AudioClip> hurtSFX;

    [Header("Death")]
    [SerializeField] private List<AudioClip> deathSFX;

    [Header("Heal")]
    [SerializeField] private List<AudioClip> healSFX;

    [Header("Stance")]
    [SerializeField] private AudioClip thrustSFX;
    [SerializeField] private AudioClip shieldSFX;
    [SerializeField] private AudioSource shieldSFXLoop;
    [SerializeField] private AudioClip dexteritySFX;

    private void Awake()
    {
        additionalSourceVolume = additionalSource.volume;
    }

    #region General

    private void StartPlayCollectionSFX(SFXMultisampleCollection sfxSettings, bool loop = false)
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

    private void StopPlayCollectionSFX(SFXMultisampleCollection sfxSettings)
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

    private AudioClip GetRandomClip(List<AudioClip> list)
    {
        int index = Random.Range(0, list.Count);
        return list[index];
    }

    #endregion

    #region Walk SFX

    public void PlayWalkSFX()
    {
        ExecutePlayWalkSFX();
        PlayWalkSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayWalkSFX_ServerRpc()
    {
        PlayWalkSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayWalkSFX_NotOwnerRpc()
    {
        ExecutePlayWalkSFX();
    }

    private void ExecutePlayWalkSFX()
    {
        StartPlayCollectionSFX(walkSFXSettings, true);
    }

    #endregion

    #region Run SFX

    public void PlayRunSFX()
    {
        ExecutePlayRunSFX();
        PlayRunSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayRunSFX_ServerRpc()
    {
        PlayRunSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayRunSFX_NotOwnerRpc()
    {
        ExecutePlayRunSFX();
    }

    private void ExecutePlayRunSFX()
    {
        StartPlayCollectionSFX(runSFXSettings, true);
    }

    #endregion

    #region Stop Movement SFX

    public void StopMovementSFX()
    {
        ExecuteStopMovementSFX();
        StopMovementSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void StopMovementSFX_ServerRpc()
    {
        StopMovementSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void StopMovementSFX_NotOwnerRpc()
    {
        ExecuteStopMovementSFX();
    }

    private void ExecuteStopMovementSFX()
    {
        StopPlayCollectionSFX(walkSFXSettings);
        StopPlayCollectionSFX(runSFXSettings);
    }

    #endregion

    #region Jump SFX

    public void PlayJumpSFX()
    {
        ExecutePlayJumpSFX();
        PlayJumpSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayJumpSFX_ServerRpc()
    {
        PlayJumpSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayJumpSFX_NotOwnerRpc()
    {
        ExecutePlayJumpSFX();
    }

    private void ExecutePlayJumpSFX()
    {
        var clip = GetRandomClip(jumpSFX);
        oneShotSource.PlayOneShot(clip);

        oneShotSource.PlayOneShot(mainJumpSFX);
    }

    #endregion

    #region Fall SFX

    public void PlayFallSFX()
    {
        ExecutePlayFallSFX();
        PlayFallSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayFallSFX_ServerRpc()
    {
        PlayFallSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayFallSFX_NotOwnerRpc()
    {
        ExecutePlayFallSFX();
    }

    private void ExecutePlayFallSFX()
    {
        oneShotSource.PlayOneShot(fallSFX);
    }

    #endregion

    #region Hurt SFX

    public void PlayHurtSFX()
    {
        ExecutePlayHurtSFX();
        PlayHurtSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayHurtSFX_ServerRpc()
    {
        PlayHurtSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayHurtSFX_NotOwnerRpc()
    {
        ExecutePlayHurtSFX();
    }

    private void ExecutePlayHurtSFX()
    {
        var clip = GetRandomClip(hurtSFX);

        oneShotSource.PlayOneShot(clip);
    }

    #endregion

    #region Death SFX

    public void PlayDeathSFX()
    {
        ExecutePlayDeathSFX();
        PlayDeathSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayDeathSFX_ServerRpc()
    {
        PlayDeathSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayDeathSFX_NotOwnerRpc()
    {
        ExecutePlayDeathSFX();
    }

    private void ExecutePlayDeathSFX()
    {
        var clip = GetRandomClip(deathSFX);

        oneShotSource.PlayOneShot(clip);
    }

    #endregion

    #region Attack SFX

    public void PlayAttackSFX(bool sendToServer = true)
    {
        ExecutePlayAttackSFX();

        if (sendToServer)
            PlayAttackSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayAttackSFX_ServerRpc()
    {
        PlayAttackSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayAttackSFX_NotOwnerRpc()
    {
        ExecutePlayAttackSFX();
    }

    private void ExecutePlayAttackSFX()
    {
        var clip = GetRandomClip(attackSFX);

        oneShotSource.PlayOneShot(clip);
    }

    #endregion

    #region Dodge SFX
    
    public void PlayDodgeSFX()
    {
        ExecutePlayDodgeSFX();
        PlayDodgeSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayDodgeSFX_ServerRpc()
    {
        PlayDodgeSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayDodgeSFX_NotOwnerRpc()
    {
        ExecutePlayDodgeSFX();
    }

    private void ExecutePlayDodgeSFX()
    {
        var addClip = GetRandomClip(jumpSFX);
        oneShotSource.PlayOneShot(addClip);

        var mainClip = GetRandomClip(dodgeSFX);
        oneShotSource.PlayOneShot(mainClip);
    }

    #endregion

    #region Play Stance SFX

    public void PlayStanceSFX(StanceType type)
    {
        ExecutePlayStanceSFX(type);
        PlayStanceSFX_ServerRpc(type);
    }

    [Rpc(SendTo.Server)]
    private void PlayStanceSFX_ServerRpc(StanceType type)
    {
        PlayStanceSFX_NotOwnerRpc(type);
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayStanceSFX_NotOwnerRpc(StanceType type)
    {
        ExecutePlayStanceSFX(type);
    }

    private void ExecutePlayStanceSFX(StanceType type)
    {
        switch (type)
        {
            case StanceType.Attack:
                oneShotSource.PlayOneShot(thrustSFX);
                break;

            case StanceType.Defense:
                oneShotSource.PlayOneShot(shieldSFX);
                shieldSFXLoop.Play();
                break;

            case StanceType.Dexterity:
                oneShotSource.PlayOneShot(dexteritySFX);
                break;

            default:
                break;
        }
    }

    #endregion

    #region Stop Stance SFX

    public void StopStanceSFX()
    {
        ExecuteStopStanceSFX();
        StopStanceSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void StopStanceSFX_ServerRpc()
    {
        StopStanceSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void StopStanceSFX_NotOwnerRpc()
    {
        ExecuteStopStanceSFX();
    }

    private void ExecuteStopStanceSFX()
    {
        shieldSFXLoop.Stop();
    }

    #endregion

    #region Heal SFX

    public void PlayHealSFX()
    {
        ExecutePlayHealSFX();
        PlayHealSFX_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void PlayHealSFX_ServerRpc()
    {
        PlayHealSFX_NotOwnerRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayHealSFX_NotOwnerRpc()
    {
        ExecutePlayHealSFX();
    }

    private void ExecutePlayHealSFX()
    {
        var clip = GetRandomClip(healSFX);
        oneShotSource.PlayOneShot(clip);
    }

    #endregion
}
