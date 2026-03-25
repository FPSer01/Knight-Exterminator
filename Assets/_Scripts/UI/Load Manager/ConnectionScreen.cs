using NUnit.Framework;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ConnectionScreen : MonoBehaviour
{
    private const string CONNECTION_TEXT = "Подключение{0}";
    private const string ERROR_TEXT = "Ошибка:\n{0}";

    [SerializeField] private CanvasGroup canvasGroup;
    [Space]
    [SerializeField] private TMP_Text mainText;
    [SerializeField] private float animationDuration = 1;
    [Space]
    [SerializeField] private UIButton okButton;

    private IEnumerator animationCoroutine;

    public bool ScreenActive { get; private set; }

    private void Start()
    {
        okButton.onClick.AddListener(OkButtonClick);
    }

    public void SetScreenActive(bool active)
    {
        EnableScreen(active);

        okButton.gameObject.SetActive(false);

        if (active)
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            animationCoroutine = DoConnectAnimation();
            StartCoroutine(animationCoroutine);
        }
        else
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
        } 
    }

    private void OkButtonClick()
    {
        SetScreenActive(false);
    }

    public void SetMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            SetScreenActive(false);
            return;
        }

        EnableScreen(true);

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        message = GetReadableMessage(message);

        okButton.gameObject.SetActive(true);
        mainText.text = string.Format(ERROR_TEXT, message);
    }

    private string GetReadableMessage(string rawMessage)
    {
        if (rawMessage.Contains("[ClosedByRemote]"))
            return "Хост прервал соединение.";

        if (rawMessage.Contains("[Timeout]"))
            return "Превышено время ожидания.";

        if (rawMessage.Contains("[VersionMismatch]"))
            return "Версия игры не совпадает.";

        if (rawMessage.Contains("[Full]"))
            return "Сервер переполнен.";

        if (rawMessage.Contains("[InvalidParameters]"))
            return "Ошибка данных подключения.";

        if (rawMessage.Contains("[MaxConnectionAttempts]"))
            return "Превышено время ожидания и количество попыток подключения.";

        return rawMessage;
    }

    private IEnumerator DoConnectAnimation()
    {
        string dots = "";

        while (true)
        {
            if (dots.Length >= 3)
            {
                dots = "";
            }

            dots += ".";
            mainText.text = string.Format(CONNECTION_TEXT, dots);

            yield return new WaitForSecondsRealtime(animationDuration);
        }
    }

    private void EnableScreen(bool enable)
    {
        ScreenActive = enable;

        canvasGroup.alpha = enable ? 1 : 0;
        canvasGroup.blocksRaycasts = enable;
        canvasGroup.interactable = enable;
    }
}
