using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class BossHealthUI : NetworkBehaviour
{
    public static BossHealthUI Instance { private set; get; }

    [Header("Main UI Elements")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Space]
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text bossDamageNumbers;
    [SerializeField] private Slider bossHealthBar;
    [SerializeField] private Slider damageBar;

    [Header("Status")]
    [SerializeField] private GameObject bloodloss;
    [SerializeField] private GameObject poison;
    [SerializeField] private GameObject slowness;
    [SerializeField] private GameObject stun;

    [Header("Boss Health UI Settings")]
    [SerializeField] private float timeToClearDamageNumbers;
    [SerializeField] private float damageKeepUpDelay;
    [SerializeField] private float damageKeepUpDuration;

    private Coroutine clearDamageTextCoroutine;
    private float storedDamage;

    private bool damageBarProcessing = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    public override void OnNetworkSpawn()
    {
        ExecuteSetActive(false, 0);
        storedDamage = 0;
        bossDamageNumbers.text = "";

        bloodloss.SetActive(false);
        poison.SetActive(false);
        slowness.SetActive(false);
        stun.SetActive(false);
    }

    #region Set Active

    public void SetActive(bool active, float timeToSwitch = 0.1f)
    {
        SetActive_ServerRpc(active, timeToSwitch);
    }

    [Rpc(SendTo.Server)]
    private void SetActive_ServerRpc(bool active, float timeToSwitch = 0.1f)
    {
        SetActive_EveryoneRpc(active, timeToSwitch);
    }

    [Rpc(SendTo.Everyone)]
    private void SetActive_EveryoneRpc(bool active, float timeToSwitch = 0.1f)
    {
        ExecuteSetActive(active, timeToSwitch);
    }

    private void ExecuteSetActive(bool active, float timeToSwitch = 0.1f)
    {
        StartCoroutine(QueueSetActive(active, timeToSwitch));
    }

    private IEnumerator QueueSetActive(bool active, float timeToSwitch = 0.1f)
    {
        if (!active)
        {
            yield return new WaitUntil(() => !damageBarProcessing);
        }

        canvasGroup.DOFade(active ? 1 : 0, timeToSwitch).SetUpdate(true);
    }

    #endregion

    #region Set Boss Name

    public void SetBossName(string bossName)
    {
        SetBossName_ServerRpc(bossName);
    }

    [Rpc(SendTo.Server)]
    private void SetBossName_ServerRpc(string bossName)
    {
        SetBossName_EveryoneRpc(bossName);
    }

    [Rpc(SendTo.Everyone)]
    private void SetBossName_EveryoneRpc(string bossName)
    {
        ExecuteSetBossName(bossName);
    }

    private void ExecuteSetBossName(string bossName)
    {
        bossNameText.text = bossName;
    }

    #endregion

    #region Update Health Bar

    public void UpdateHealthBar(float sliderValue, bool updateDamageBarInstantly = false)
    {
        UpdateHealthBar_ServerRpc(sliderValue, updateDamageBarInstantly);
    }

    [Rpc(SendTo.Server)]
    private void UpdateHealthBar_ServerRpc(float sliderValue, bool updateDamageBarInstantly = false)
    {
        UpdateHealthBar_EveryoneRpc(sliderValue, updateDamageBarInstantly);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateHealthBar_EveryoneRpc(float sliderValue, bool updateDamageBarInstantly = false)
    {
        ExecuteUpdateHealthBar(sliderValue, updateDamageBarInstantly);
    }

    private void ExecuteUpdateHealthBar(float sliderValue, bool updateDamageBarInstantly = false)
    {
        bossHealthBar.value = sliderValue;

        if (!updateDamageBarInstantly)
        {
            UpdateDamageBar(sliderValue);
        }
        else
        {
            damageBar.value = 1;
        }
    }

    private void UpdateDamageBar(float newValue)
    {
        damageBar.DOKill();
        
        if (!damageBarProcessing)
        {
            damageBarProcessing = true;
            damageBar.DOValue(newValue, damageKeepUpDuration)
                         .SetDelay(damageKeepUpDelay)
                         .SetUpdate(true)
                         .OnComplete(() => damageBarProcessing = false);
        }
        else
        {
            damageBar.DOValue(newValue, damageKeepUpDuration)
                         .SetUpdate(true)
                         .OnComplete(() => damageBarProcessing = false);
        }
    }

    #endregion

    #region Update Damage Numbers

    public void UpdateDamageNumber(float value)
    {
        UpdateDamageNumber_ServerRpc(value);
    }

    [Rpc(SendTo.Server)]
    private void UpdateDamageNumber_ServerRpc(float value)
    {
        UpdateDamageNumber_EveryoneRpc(value);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateDamageNumber_EveryoneRpc(float value)
    {
        ExecuteUpdateDamageNumber(value);
    }

    private void ExecuteUpdateDamageNumber(float value)
    {
        if (clearDamageTextCoroutine != null)
        {
            StopCoroutine(clearDamageTextCoroutine);
            clearDamageTextCoroutine = null;
        }

        storedDamage += Mathf.Round(value);

        if (storedDamage <= 0f || value <= 0f)
        {
            bossDamageNumbers.text = "";
        }
        else
        {
            bossDamageNumbers.text = $"- {storedDamage}";
            clearDamageTextCoroutine = StartCoroutine(ClearDamageNumbers(timeToClearDamageNumbers / 2));
        }
    }

    private IEnumerator ClearDamageNumbers(float delay)
    {
        yield return new WaitForSeconds(delay);

        storedDamage = 0;
        bossDamageNumbers.text = "";
    }

    #endregion

    #region Status Icons

    public void SetStatusIcon(StatusType type, bool active)
    {
        SetStatusIcon_ServerRpc(type, active);
    }

    [Rpc(SendTo.Server)]
    private void SetStatusIcon_ServerRpc(StatusType type, bool active)
    {
        SetStatusIcon_EveryoneRpc(type, active);
    }

    [Rpc(SendTo.Everyone)]
    private void SetStatusIcon_EveryoneRpc(StatusType type, bool active)
    {
        ExecuteSetStatusIcon(type, active);
    }

    private void ExecuteSetStatusIcon(StatusType type, bool active)
    {
        switch (type)
        {
            case StatusType.Bloodloss:
                bloodloss.SetActive(active);
                break;

            case StatusType.Poison:
                poison.SetActive(active);
                break;

            case StatusType.Slowness:
                slowness.SetActive(active);
                break;

            case StatusType.Stun:
                stun.SetActive(active);
                break;
        }
    }

    #endregion
}
