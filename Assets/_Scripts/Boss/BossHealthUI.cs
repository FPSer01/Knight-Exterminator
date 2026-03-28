using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : NetworkBehaviour
{
    //public static BossHealthUI Instance { private set; get; }

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

    /*private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }*/

    public override void OnNetworkSpawn()
    {
        SetActive_OwnerRpc(false, 0);
        storedDamage = 0;
        bossDamageNumbers.text = "";

        bloodloss.SetActive(false);
        poison.SetActive(false);
        slowness.SetActive(false);
        stun.SetActive(false);
    }

    #region Set Active

    [Rpc(SendTo.Owner)]
    public void SetActive_OwnerRpc(bool active, float timeToSwitch = 0.1f)
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

    [Rpc(SendTo.Owner)]
    public void SetBossName_OwnerRpc(string bossName)
    {
        bossNameText.text = bossName;
    }

    #endregion

    #region Update Health Bar

    [Rpc(SendTo.Owner)]
    public void UpdateHealthBar_OwnerRpc(float sliderValue, bool updateDamageBarInstantly = false)
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

    [Rpc(SendTo.Owner)]
    public void UpdateDamageNumber_OwnerRpc(float value)
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

    [Rpc(SendTo.Owner)]
    public void SetStatusIcon_OwnerRpc(StatusType type, bool active)
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
