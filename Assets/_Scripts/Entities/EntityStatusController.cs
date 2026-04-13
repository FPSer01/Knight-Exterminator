using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EntityStatusController : NetworkBehaviour
{
    [Header("MAIN")]
    [SerializeField] private EntityHealth healthComponent;

    [Header("General Settings")]
    [SerializeField] private float maxValueToTriggerStatus = 100f;
    [SerializeField] private float wearOffDelay = 0f;

    [Header("Status Settings")]
    [SerializeField] private TMP_Text statusWarning;
    [Space]
    [SerializeField] private StatusDataContainer bloodloss = new StatusDataContainer(StatusType.Bloodloss);
    [Space]
    [SerializeField] private StatusDataContainer poison = new StatusDataContainer(StatusType.Poison);
    [Space]
    [SerializeField] private StatusDataContainer slowness = new StatusDataContainer(StatusType.Slowness);
    [Space]
    [SerializeField] private StatusDataContainer stun = new StatusDataContainer(StatusType.Stun);

    private void Start()
    {
        SetStatusObjectActive(bloodloss, false, false);
        SetStatusObjectActive(poison, false, false);
        SetStatusObjectActive(slowness, false, false);
        SetStatusObjectActive(stun, false, false);

        if (statusWarning != null)
            statusWarning.DOFade(0, 0);

        healthComponent.OnStatusTaken += SetStatusValues;
    }

    public override void OnDestroy()
    {
        healthComponent.OnStatusTaken -= SetStatusValues;

        base.OnDestroy();
    }

    private void SetStatusObjectActive(StatusDataContainer statusData, bool active, bool sendRpc = true)
    {
        if (statusData.StatusObject == null)
            return;

        ExecuteSetStatusObjectActive(statusData.Type, active);

        if (sendRpc)
            SetStatusObjectActive_NotOwnerRpc(statusData.Type, active);
    }

    [Rpc(SendTo.NotOwner)]
    private void SetStatusObjectActive_NotOwnerRpc(StatusType status, bool active)
    {
        ExecuteSetStatusObjectActive(status, active);
    }

    private void ExecuteSetStatusObjectActive(StatusType status, bool active)
    {
        switch (status)
        {
            case StatusType.Bloodloss:
                if (bloodloss.StatusObject != null)
                    bloodloss.StatusObject.SetActive(active);
                break;
            case StatusType.Poison:
                if (poison.StatusObject != null)
                    poison.StatusObject.SetActive(active);
                break;
            case StatusType.Slowness:
                if (slowness.StatusObject != null)
                    slowness.StatusObject.SetActive(active);
                break;
            case StatusType.Stun:
                if (poison.StatusObject != null)
                    stun.StatusObject.SetActive(active);
                break;
        }
    }

    public void ClearStatus(StatusType type)
    {
        switch (type)
        {
            case StatusType.Bloodloss:
                StartCoroutine(WearOffStatus(bloodloss, 0, true));
                break;

            case StatusType.Poison:
                StartCoroutine(WearOffStatus(poison, 0, true));
                break;
        }
    }

    private void SetStatusValues(StatusInflictData data)
    {
        UpdateStatus(bloodloss, data, data.Bloodloss, data.BloodlossWearOffTime);
        UpdateStatus(poison, data, data.Poison, data.PoisonWearOffTime);
        UpdateRenewableStatus(slowness, data, data.SlownessAmount, data.SlownessWearOffTime); // Особик козел сраный
        UpdateStatus(stun, data, data.StunAmount, data.StunWearOffTime);
    }

    private void UpdateRenewableStatus(StatusDataContainer status, StatusInflictData data, float value, float wearOffTime)
    {
        if (value == 0)
            return;

        status.CurrentValue += value;
        status.CurrentValue = Mathf.Clamp(status.CurrentValue, 0, maxValueToTriggerStatus);

        if (status.Bar != null)
            status.Bar.value = status.CurrentValue / maxValueToTriggerStatus;

        if (status.CurrentValue > 0 && !status.ShowOnlyOnInflict)
            SetStatusObjectActive(status, true, false);

        // Активация возобновляемого статуса
        if (status.CurrentValue >= maxValueToTriggerStatus)
        {
            if (status.Active == false && statusWarning != null && !string.IsNullOrEmpty(status.Warning))
            {
                statusWarning.DOKill();
                statusWarning.text = $"!!! {status.Warning} !!!";
                statusWarning.DOFade(1, 0.75f);
                statusWarning.DOFade(0, 0.75f).SetDelay(2.75f);

                // Для отображения у других, только когда статус наложен
                SetStatusObjectActive(status, true);
                healthComponent.SetActiveStatus(status.Type, data, true);
            }

            if (status.WearOffCoroutine != null)
            {
                StopCoroutine(status.WearOffCoroutine);
                status.WearOffCoroutine = null;
            }

            status.Active = true;

            // Постоянное снижение статуса
            float time = (status.CurrentValue / maxValueToTriggerStatus) * wearOffTime;
            DOTween.Kill(status);
            DOTween.To(() => status.CurrentValue, (x) => status.CurrentValue = x, 0, time)
                .SetEase(Ease.Linear)
                .OnUpdate(() => {
                    status.Bar.value = status.CurrentValue / maxValueToTriggerStatus;
                })
                .SetTarget(status)
                .OnComplete(() => {
                    status.CurrentValue = 0;
                    status.Active = false;
                    healthComponent.SetActiveStatus(status.Type, data, false);

                    SetStatusObjectActive(status, false, false);
                });
        }
        else
        {
            status.WearOffCoroutine = StartCoroutine(WearOffStatus(status, wearOffTime));
        }
    }

    private void UpdateStatus(StatusDataContainer status, StatusInflictData data, float value, float wearOffTime)
    {
        if (value == 0 || status.Active)
            return;

        status.CurrentValue += value;
        status.CurrentValue = Mathf.Clamp(status.CurrentValue, 0, maxValueToTriggerStatus);

        if (status.Bar != null)
            status.Bar.value = status.CurrentValue / maxValueToTriggerStatus;

        if (status.CurrentValue > 0 && !status.ShowOnlyOnInflict)
            SetStatusObjectActive(status, true, false);

        if (status.WearOffCoroutine != null)
        {
            status.Bar.DOKill();
            StopCoroutine(status.WearOffCoroutine);
            status.WearOffCoroutine = null;
        }

        if (status.CurrentValue >= maxValueToTriggerStatus)
        {
            InflictStatus(status, data, wearOffTime);
        }
        else
        {
            status.WearOffCoroutine = StartCoroutine(WearOffStatus(status, wearOffTime));
        }
    }

    private void InflictStatus(StatusDataContainer status, StatusInflictData data, float wearOffTime)
    {
        status.Active = true;

        // Для отображения у других, только когда статус наложен
        SetStatusObjectActive(status, true);
        healthComponent.SetActiveStatus(status.Type, data, true);

        if (statusWarning != null && !string.IsNullOrEmpty(status.Warning))
        {
            statusWarning.DOKill();
            statusWarning.text = $"!!! {status.Warning} !!!";
            statusWarning.DOFade(1, 0.75f);
            statusWarning.DOFade(0, 0.75f).SetDelay(2.75f);
        }

        if (status.WearOffCoroutine != null)
        {
            StopCoroutine(status.WearOffCoroutine);
            status.WearOffCoroutine = null;
        }

        if (status.Bar != null)
        {
            status.Bar.DOKill();
            status.Bar.DOValue(0, wearOffTime)
                .SetEase(Ease.Linear)
                .OnUpdate(() => {
                    status.CurrentValue = status.Bar.value * maxValueToTriggerStatus;
                })
                .OnComplete(() => {
                    healthComponent.SetActiveStatus(status.Type, data, false);
                    status.Active = false;

                    status.CurrentValue = 0;
                    SetStatusObjectActive(status, false);
                });
        }
        else
        {
            DOTween.Kill(status);
            DOTween.To(() => status.CurrentValue, (x) => status.CurrentValue = x, 0, wearOffTime)
                .SetEase(Ease.Linear)
                .SetTarget(status)
                .OnComplete(() => {
                    healthComponent.SetActiveStatus(status.Type, data, false);
                    status.Active = false;

                    status.CurrentValue = 0;
                    SetStatusObjectActive(status, false);
                });
        }
    }

    private IEnumerator WearOffStatus(StatusDataContainer status, float wearOffTime, bool immediate = false)
    {
        float startValue = status.CurrentValue;
        float time = (startValue / maxValueToTriggerStatus) * wearOffTime;

        if (immediate)
            time = 0;

        DOTween.Kill(status);
        yield return new WaitForSeconds(wearOffDelay);

        if (status.Bar != null)
        {
            status.Bar.DOKill();
            status.Bar.DOValue(0, time)
                .SetEase(Ease.Linear)
                .OnUpdate(() => {
                    status.CurrentValue = status.Bar.value * maxValueToTriggerStatus;
                })
                .OnComplete(() => {
                    status.CurrentValue = 0;
                    SetStatusObjectActive(status, false, false);
                });
        }
        else
        {
            DOTween.To(() => status.CurrentValue, (x) => status.CurrentValue = x, 0, time)
                .SetEase(Ease.Linear)
                .SetTarget(status)
                .OnComplete(() => {
                    status.CurrentValue = 0;
                    status.Active = false;
                    SetStatusObjectActive(status, false, false);
                });
        }
    }
}

[Serializable]
public class StatusDataContainer
{
    [Header("General")]
    public StatusType Type;
    public string Warning;

    [Header("Objects or UI")]
    public GameObject StatusObject;
    public bool ShowOnlyOnInflict;
    public Slider Bar;

    [Header("Stats")]
    public float CurrentValue;
    public bool Active;

    [HideInInspector] public Coroutine WearOffCoroutine;

    public StatusDataContainer(StatusType type)
    {
        Type = type;
    }
}
