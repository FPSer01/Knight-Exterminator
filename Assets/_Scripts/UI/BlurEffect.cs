using DG.Tweening;
using UnityEngine;

public class BlurEffect : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private void Start()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void SetBlur(bool active, float fadeTime = 0.1f)
    {
        canvasGroup.DOFade(active ? 1 : 0, fadeTime).SetUpdate(true);
    }
}
