using UnityEngine;

public class PlayerMiniMap : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    public void SetActive(bool active)
    {
        canvasGroup.alpha = active ? 1 : 0;
    }
}
