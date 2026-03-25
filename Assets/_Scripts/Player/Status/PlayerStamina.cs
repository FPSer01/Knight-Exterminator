using DG.Tweening;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : NetworkBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina;
    [SerializeField] private float currentStamina;
    [Space]
    [SerializeField] private float staminaRecoveryDelay;
    [SerializeField] private float staminaTimeRecovery;

    [Header("VFX")]
    [SerializeField] private ParticleSystem infiniteStaminaVFX;
    [SerializeField] private ParticleSystem infiniteStaminaBurstVFX;

    [Header("Components")]
    [SerializeField] private PlayerComponents playerComponents;
    private PlayerMovement playerMovement => playerComponents.Movement;
    private PlayerStateUI playerStateUI => playerComponents.UI.PlayerStateUI;

    public float CurrentStamina { get => currentStamina; set => currentStamina = value; }
    public float MaxStamina { get => maxStamina; set => ChangeMaxStamina(value); }

    private bool staminaConsumingContinuously = false;
    private IEnumerator recoverStaminaCor;
    private IEnumerator continuouslyStaminaCor;

    private bool blockStaminaConsumage;

    private void Start()
    {
        SetCurrentStamina(maxStamina);
    }

    /// <summary>
    /// Задать нынешнее значение выносливости и обновить UI
    /// </summary>
    /// <param name="newValue"></param>
    private void SetCurrentStamina(float newValue)
    {
        currentStamina = Mathf.Clamp(newValue, 0, maxStamina);
        UpdateStaminaBar();
    }

    public void ChangeMaxStamina(float newMaxStamina)
    {
        float oldMax = maxStamina;
        float staminaAdd = newMaxStamina - oldMax;
        maxStamina = newMaxStamina;

        SetCurrentStamina(currentStamina + staminaAdd);
    }

    public void ConsumeStamina(float value)
    {
        if (blockStaminaConsumage)
            return;

        if (recoverStaminaCor != null)
        {
            StopCoroutine(recoverStaminaCor);
            recoverStaminaCor = null;
        }

        SetCurrentStamina(currentStamina - value);

        if (currentStamina < maxStamina && !staminaConsumingContinuously)
        {
            recoverStaminaCor = RecoverStamina();
            StartCoroutine(recoverStaminaCor);
        }
    }

    public void ConsumeStaminaContinuously(bool consume, float valuePreSecond = 0f)
    {
        if (!blockStaminaConsumage)
            staminaConsumingContinuously = consume;

        if (recoverStaminaCor != null)
        {
            StopCoroutine(recoverStaminaCor);
            recoverStaminaCor = null;
        }

        if (currentStamina < maxStamina && !staminaConsumingContinuously)
        {
            recoverStaminaCor = RecoverStamina();
            StartCoroutine(recoverStaminaCor);
        }

        if (consume && !blockStaminaConsumage)
        {
            continuouslyStaminaCor = ConsumeContinuously(valuePreSecond);
            StartCoroutine(continuouslyStaminaCor);
        }
        else
        {
            if (continuouslyStaminaCor != null)
            {
                StopCoroutine(continuouslyStaminaCor);
                continuouslyStaminaCor = null;
            }
        }
    }

    private void UpdateStaminaBar()
    {
        playerStateUI.SetStaminaBarValue(currentStamina / maxStamina);
    }

    #region Coroutines

    private IEnumerator ConsumeContinuously(float valuePreSecond)
    {
        while (currentStamina > 0)
        {
            SetCurrentStamina(currentStamina - valuePreSecond * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        yield break;
    }

    private IEnumerator RecoverStamina()
    {
        yield return new WaitForSeconds(staminaRecoveryDelay);

        float recoveryAmount = maxStamina / staminaTimeRecovery;

        while (currentStamina < maxStamina)
        {
            SetCurrentStamina(currentStamina + recoveryAmount * Time.deltaTime);

            yield return new WaitForEndOfFrame();
        }
    }

    #endregion

    public void PlayBurstVFX()
    {
        if (infiniteStaminaBurstVFX == null)
            return;

        infiniteStaminaBurstVFX.Play();
    }

    public void BlockStaminaConsumage(bool block)
    {
        blockStaminaConsumage = block;

        if (block)
        {
            EnableInfiniteStaminaVFX(true);
        }
        else
        {
            if (playerMovement.Sprinting)
            {
                ConsumeStaminaContinuously(true, StaminaConsumage.SPRINT);
            }

            EnableInfiniteStaminaVFX(false);
        }
    }

    private void EnableInfiniteStaminaVFX(bool enable)
    {
        ExecuteEnableInfiniteStaminaVFX(enable);
        EnableInfiniteStaminaVFX_ToServerRpc(enable);
    }

    private void ExecuteEnableInfiniteStaminaVFX(bool enable)
    {
        if (enable)
        {
            infiniteStaminaVFX.Play();
        }
        else
        {
            infiniteStaminaVFX.Stop();
        }
    }

    [Rpc(SendTo.Server)]
    private void EnableInfiniteStaminaVFX_ToServerRpc(bool enable)
    {
        EnableInfiniteStaminaVFX_ToEveryoneRpc(enable);
    }

    [Rpc(SendTo.NotOwner)]
    private void EnableInfiniteStaminaVFX_ToEveryoneRpc(bool enable)
    {
        ExecuteEnableInfiniteStaminaVFX(enable);
    }

    [Rpc(SendTo.Owner)]
    public void ResetAll_OwnerRpc()
    {
        if (continuouslyStaminaCor != null)
        {
            StopCoroutine(continuouslyStaminaCor);
            continuouslyStaminaCor = null;
        }

        if (recoverStaminaCor != null)
        {
            StopCoroutine(recoverStaminaCor);
            recoverStaminaCor = null;
        }

        SetCurrentStamina(maxStamina);
    }
}

public class StaminaConsumage
{
    public const float SPRINT = 1.5f;
    public const float JUMP = 4.5f;
    public const float DODGE = 6f;
    public const float ATTACK = 3.5f;
}
