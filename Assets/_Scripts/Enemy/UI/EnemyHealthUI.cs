using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Space]
    [SerializeField] private TMP_Text damageNumbers;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider damageBar;

    [Header("Status")]
    [SerializeField] private GameObject bloodloss;
    [SerializeField] private GameObject poison;
    [SerializeField] private GameObject slowness;
    [SerializeField] private GameObject stun;

    [Header("Settings")]
    [SerializeField] private float timeToHide = 2f;
    [SerializeField] private float damageKeepUpDelay;
    [SerializeField] private float damageKeepUpDuration;

    private Coroutine UIHideCoroutine;
    private Coroutine ClearDamageTextCoroutine;
    private float storedDamage;

    private bool damageBarProcessing = false;

    private void Start()
    {
        canvasGroup.alpha = 0f;
        damageNumbers.text = "";

        gameObject.SetActive(false);

        bloodloss.SetActive(false);
        poison.SetActive(false);
        slowness.SetActive(false);
        stun.SetActive(false);
    }

    public void UpdateHealthBar(float newValue, bool showHealthBar = true, bool updateDamageBarInstantly = false)
    {
        // 
        if (UIHideCoroutine != null)
        {
            StopCoroutine(UIHideCoroutine);
        }

        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1, 0.25f);
        }

        // Обновление визуала UI
        healthBar.value = newValue;

        if (!updateDamageBarInstantly)
        {
            UpdateDamageBar(newValue);
        }
        else
        {
            damageBar.value = 1;
        }

        // Показать/убрать UI здоровья
        if (showHealthBar)
        {
            UIHideCoroutine = StartCoroutine(HideHealthUI(timeToHide));
        } 
    }

    private IEnumerator HideHealthUI(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0, 1f).OnComplete(() => gameObject.SetActive(false));
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

    public void UpdateDamageNumber(float valueToAdd)
    {
        UpdateDamageUI_Rpc(valueToAdd);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateDamageUI_Rpc(float valueToAdd)
    {
        if (ClearDamageTextCoroutine != null)
        {
            StopCoroutine(ClearDamageTextCoroutine);
            ClearDamageTextCoroutine = null;
        }

        storedDamage += Mathf.Round(valueToAdd);

        if (storedDamage <= 0f || valueToAdd <= 0f)
        {
            damageNumbers.text = "";
        }
        else
        {
            damageNumbers.text = $"- {storedDamage}";
            ClearDamageTextCoroutine = StartCoroutine(ClearDamageUI(timeToHide / 2));
        }
    }

    private IEnumerator ClearDamageUI(float delay)
    {
        yield return new WaitForSeconds(delay);

        storedDamage = 0;
        damageNumbers.text = "";
    }

    public void StopAllProcesses()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }
    }
}
