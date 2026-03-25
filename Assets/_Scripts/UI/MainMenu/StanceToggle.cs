using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StanceToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private TMP_Text text;

    private StanceInfo currentStanceInfo;

    public event Action<StanceInfo> OnStanceChoose;

    private void Start()
    {
        toggle.onValueChanged.AddListener(OnToggle);
    }

    private void OnToggle(bool value)
    {
        if (value == true)
        {
            OnStanceChoose?.Invoke(currentStanceInfo);
        }
    }

    public void Setup(StanceInfo info, ToggleGroup group = null)
    {
        if (group != null)
        {
            toggle.group = group;
        }

        if (info == null)
        {
            return;
        }

        if (info.Type == StanceType.None)
        {
            return;
        }

        text.text = info.StanceName;
        currentStanceInfo = info;
        toggle.isOn = false;
    }
}
