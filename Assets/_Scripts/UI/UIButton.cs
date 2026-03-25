using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : Button, IPointerUpHandler
{
    private TMP_Text text;

    protected override void Start()
    {
        base.Start();

        text = GetComponentInChildren<TMP_Text>();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        EventSystem.current.SetSelectedGameObject(null);
    }
}
