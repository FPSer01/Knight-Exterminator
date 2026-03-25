using DG.Tweening;
using UnityEngine;

public class PlayerUIWindow : MonoBehaviour
{
    [Header("Player UI Window General")]
    [SerializeField] protected PlayerUI playerUI;
    [SerializeField] protected CanvasGroup canvasGroup;

    public virtual void SetWindowActive(bool active, float timeToSwitch = 0.1f)
    {
        canvasGroup.blocksRaycasts = active;
        canvasGroup.interactable = active;

        canvasGroup.DOFade(active ? 1 : 0, timeToSwitch).SetUpdate(true);
    }
}
