using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadScreen : MonoBehaviour
{
    private const string LOAD_TEXT = "Загрузка{0}";

    [SerializeField] private CanvasGroup canvasGroup;
    [Space]
    [SerializeField] private TMP_Text mainText;
    [SerializeField] private float animationDuration = 1;
    [Space]
    [SerializeField] private Slider progressBar;

    private IEnumerator animationCoroutine;

    public Slider ProgressBar { get => progressBar; }

    public bool ScreenActive { get; private set; }

    public void SetScreenActive(bool active)
    {
        ScreenActive = active;

        canvasGroup.alpha = active ? 1 : 0;
        canvasGroup.blocksRaycasts = active;
        canvasGroup.interactable = active;

        if (active)
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            animationCoroutine = DoConnectAnimation();
            StartCoroutine(animationCoroutine);

            progressBar.value = 0f;
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
            mainText.text = string.Format(LOAD_TEXT, dots);

            yield return new WaitForSecondsRealtime(animationDuration);
        }
    }
}
