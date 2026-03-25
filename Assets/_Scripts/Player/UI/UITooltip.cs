using DG.Tweening;
using TMPro;
using UnityEngine;

public class UITooltip : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private TMP_Text textObject;
    [Space]
    [SerializeField] private Vector2 offset;

    private RectTransform rect;
    private bool active = false;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void SetVisible(bool active, float timeToSwitch = 0.1f)
    {
        this.active = active;

        canvasGroup.DOFade(active ? 1 : 0, timeToSwitch).SetUpdate(true);
    }

    public void SetInfo(string text)
    {
        textObject.text = text;
    }

    private void Update()
    {
        FollowPointer();
    }

    private void FollowPointer()
    {
        if (!active)
            return;

        var point = InputManager.Input.UI.Point.ReadValue<Vector2>();

        rect.position = point + offset;
    }
}
