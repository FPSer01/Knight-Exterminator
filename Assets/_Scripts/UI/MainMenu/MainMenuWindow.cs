using DG.Tweening;
using UnityEngine;

public class MainMenuWindow : MonoBehaviour
{
    [Header("Main Menu Window")]
    [SerializeField] protected MainMenu mainMenu;
    [SerializeField] protected CanvasGroup canvasGroup;

    public virtual void SetWindowActive(bool active, float timeToSwitch = 0.1f)
    {
        canvasGroup.blocksRaycasts = active;
        canvasGroup.interactable = active;

        canvasGroup.DOFade(active ? 1 : 0, timeToSwitch).SetUpdate(true);
    }
}
