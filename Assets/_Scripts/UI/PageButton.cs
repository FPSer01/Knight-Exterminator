using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PageButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GraphicTarget background;
    [SerializeField] private GraphicTarget text;
    [SerializeField] private float fadeTime = 0.3f;

    public event Action<PageButton> OnClick;
    private bool selected = false;

    private void Start()
    {
        background.SetNormalColor(0);
        text.SetNormalColor(0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selected)
            return;

        background.SetHighlightColor(fadeTime);
        text.SetHighlightColor(fadeTime);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selected)
            return;

        background.SetNormalColor(fadeTime);
        text.SetNormalColor(fadeTime);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selected)
            return;

        background.SetPressedColor(fadeTime);
        text.SetPressedColor(fadeTime);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (selected)
            return;

        background.SetSelectedColor(fadeTime);
        text.SetSelectedColor(fadeTime);
        selected = true;

        OnClick?.Invoke(this);
    }

    public void Unselect()
    {
        background.SetNormalColor(fadeTime);
        text.SetNormalColor(fadeTime);
        selected = false;
    }

    public void Select()
    {
        background.SetSelectedColor(fadeTime);
        text.SetSelectedColor(fadeTime);
        selected = true;
    }

    public enum ButtonState 
    {
        Normal,
        Highlighted,
        Pressed,
        Disabled
    }

    [Serializable]
    public struct GraphicTarget 
    {
        public Graphic targetGraphic;
        public Color normalColor;
        public Color highlightColor;
        public Color pressedColor;
        public Color selectedColor;
        public Color disabledColor;

        public void SetNormalColor(float time)
        {
            targetGraphic.DOColor(normalColor, time);
        }

        public void SetHighlightColor(float time)
        {
            targetGraphic.DOColor(highlightColor, time);
        }

        public void SetPressedColor(float time)
        {
            targetGraphic.DOColor(pressedColor, time);
        }

        public void SetSelectedColor(float time)
        {
            targetGraphic.DOColor(selectedColor, time);
        }

        public void SetDisabledColor(float time)
        {
            targetGraphic.DOColor(disabledColor, time);
        }

    }
}
