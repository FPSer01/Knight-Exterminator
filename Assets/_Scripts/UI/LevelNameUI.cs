using DG.Tweening;
using TMPro;
using UnityEngine;

public class LevelNameUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text levelLabel;

    private void Start()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        canvasGroup.alpha = 0f;
    }

    public void ShowLevelLabel(string levelName, float delay, float stayDuration, float fadeDuration = 1.5f)
    {
        levelLabel.text = levelName;

        canvasGroup.DOFade(1, fadeDuration).SetDelay(delay);
        canvasGroup.DOFade(0, fadeDuration).SetDelay(stayDuration + delay);
    }
}
