using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StanceCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject outline;
    [SerializeField] private float animationTime;
    [Space]
    [SerializeField] private StanceInfo stanceInfo;

    [Header("UI")]
    [SerializeField] private TMP_Text cardName;
    [SerializeField] private TMP_Text cardDescription;
    [SerializeField] private Image cardIcon;

    private bool cardStatic = false;

    private Vector3 originalScale;

    public event Action<StanceCard, StanceType> OnCardPick;

    private void Awake()
    {
        SetupCard(stanceInfo);
    }

    private void Start()
    {
        originalScale = transform.localScale;
    }

    #region Pointer Events

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardStatic)
            return;

        OnCardPick?.Invoke(this, stanceInfo.Type);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cardStatic)
            return;

        transform.DOScale(Vector3.one * 1.1f, animationTime);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (cardStatic)
            return;

        transform.DOScale(originalScale, animationTime);
    }

    #endregion

    public void SetOutline(bool active)
    {
        outline.SetActive(active);
    }

    public void SetupCard(StanceInfo info, bool showRevives = true)
    {
        if (info == null)
        {
            SetupDefaultCard();
            return;
        }

        if (info.Type == StanceType.None)
        {
            SetupDefaultCard();
            return;
        }

        cardName.text = info.StanceName;

        if (showRevives)
            cardDescription.text = info.FullDescription;
        else
            cardDescription.text = info.Description;

        cardIcon.color = new Color(255, 255, 255, 1);
        cardIcon.sprite = info.StanceIcon;
    }

    private void SetupDefaultCard()
    {
        cardName.text = "";
        cardDescription.text = "";
        cardIcon.sprite = null;
        cardIcon.color = new Color(0, 0, 0, 0);
    }

    /// <summary>
    /// Сделать карточку статичной, т.е. без анимаций
    /// </summary>
    /// <param name="isStatic"></param>
    public void SetStaticState(bool isStatic)
    {
        cardStatic = isStatic;
    }
}
