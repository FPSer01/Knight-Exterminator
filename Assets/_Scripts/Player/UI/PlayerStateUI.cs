using Coffee.UIExtensions;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStateUI : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider damageBar;
    [SerializeField] private float damageKeepUpDelay;
    [SerializeField] private float damageKeepUpDuration;

    [Header("Other")]
    [SerializeField] private Slider staminaBar;
    [SerializeField] private TMP_Text levelValueText;
    [Space]
    [SerializeField] private Slider stanceBar;
    [SerializeField] private UIParticle UI_reloadStanceVFX;
    [SerializeField] private Graphic stanceFillBar;
    [SerializeField] private Color barColorOnReady;
    [SerializeField] private float onReadyFadeOut = 0.5f;
    private Color barColorOriginal;

    private float previousHealthValue = 0f;
    private bool damageBarProcessing = false;

    public Slider HealthBar { get => healthBar; }
    public Slider StaminaBar { get => staminaBar; }
    public Slider StanceBar { get => stanceBar; }

    private void Start()
    {
        barColorOriginal = stanceFillBar.color;
    }

    public void SetHealthBarValue(float value, bool updateDamageBarInstantly = false)
    {
        previousHealthValue = value;
        healthBar.value = value;

        if (!updateDamageBarInstantly || previousHealthValue > value)
        {
            UpdateDamageBar(value);
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

    public void SetStaminaBarValue(float value)
    {
        staminaBar.value = value;
    }

    public void SetLevelValue(int value)
    {
        levelValueText.text = value.ToString();
    }

    #region Stance UI

    public void SetStanceBarValue(float value)
    {
        stanceBar.DOKill();
        stanceBar.value = value;
    }

    public void DoStanceBarAnimation(float endValue, float time)
    {
        stanceBar.DOKill();
        stanceBar.DOValue(endValue, time);
    }

    public void DoStanceBarEffect()
    {
        UI_reloadStanceVFX.Play();
        stanceFillBar.color = barColorOnReady;
        stanceFillBar.DOColor(barColorOriginal, onReadyFadeOut);
    }

    #endregion
}
