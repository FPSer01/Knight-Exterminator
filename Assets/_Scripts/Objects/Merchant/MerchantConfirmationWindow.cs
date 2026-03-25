using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

public class MerchantConfirmationWindow : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [Space]
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private UIButton yesButton;
    [SerializeField] private UIButton noButton;

    public event Action OnConfirm;
    public event Action OnCancel;

    public void SetActive(bool active, float timeToSwitch = 0.1f)
    {
        if (active)
        {
            InputManager.Input.UI.Submit.started += Submit_started;
        }
        else
        {
            InputManager.Input.UI.Submit.started -= Submit_started;
        }

        canvasGroup.interactable = active;
        canvasGroup.blocksRaycasts = active;
        canvasGroup.DOFade(active ? 1 : 0, timeToSwitch).SetUpdate(true);
    }

    private void Submit_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        YesAction();
    }

    private void Start()
    {
        yesButton.onClick.AddListener(YesAction);
        noButton.onClick.AddListener(NoAction);
    }

    public void SetWindowType(UpgradeItem item, MerchantOperation operation)
    {
        switch (operation)
        {
            case MerchantOperation.Buy:
                label.text = "Вы точно хотите купить";
                break;
            case MerchantOperation.Sell:
                label.text = "Вы точно хотите продать";
                break;
        }

        itemName.text = item.ItemName;
    }

    private void NoAction()
    {
        OnCancel?.Invoke();
        SetActive(false);
    }

    private void YesAction()
    {
        OnConfirm?.Invoke();
        SetActive(false);
    }
}
