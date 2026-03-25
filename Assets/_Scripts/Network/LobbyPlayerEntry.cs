using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerEntry : MonoBehaviour
{
    [Header("General Info")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text readyStatusText;

    [Header("Stance")]
    [SerializeField] private Image stanceIcon;
    [SerializeField] private GameObject defaultIcon;
    [SerializeField] private List<StanceInfo> stanceInfos;

    private ulong clientId;
    private PlayerNetworkObject trackedPlayer;

    public ulong ClientId { get => clientId; }

    public event Action OnPlayerStateChange;

    public void Setup(ulong id)
    {
        clientId = id;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ClientId, out NetworkClient client))
        {
            trackedPlayer = client.PlayerObject.GetComponent<PlayerNetworkObject>();

            if (trackedPlayer != null)
            {
                trackedPlayer.PlayerName.OnValueChanged += OnPlayerNameChanged;
                trackedPlayer.IsReady.OnValueChanged += OnReadyStateChanged;
                trackedPlayer.Stance.OnValueChanged += OnStanceChanged;

                OnPlayerNameChanged("", trackedPlayer.PlayerName.Value);
                UpdateReadyStatus(trackedPlayer.IsReady.Value);
                OnStanceChanged(StanceType.None, trackedPlayer.Stance.Value);
            }
        }
    }

    private void OnStanceChanged(StanceType previousValue, StanceType newValue)
    {
        if (newValue == StanceType.None)
        {
            SetDefaultIcon();
        }
        else
        {
            SetStanceIcon(newValue);
        }

        OnPlayerStateChange?.Invoke();
    }

    private void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        string newName = newValue.ToString();
        playerNameText.text = string.IsNullOrEmpty(newName) ? "Connecting..." : newName;

        OnPlayerStateChange?.Invoke();
    }

    private void OnReadyStateChanged(bool oldValue, bool newValue)
    {
        UpdateReadyStatus(newValue);

        OnPlayerStateChange?.Invoke();
    }

    private void SetDefaultIcon()
    {
        defaultIcon.SetActive(true);
        stanceIcon.color = new Color(0, 0, 0, 0);
        stanceIcon.sprite = null;
    }

    private void SetStanceIcon(StanceType type)
    {
        var foundInfo = stanceInfos.Find(info => info.Type == type);

        if (foundInfo == null)
        {
            SetDefaultIcon();
            return;
        }

        defaultIcon.SetActive(false);
        stanceIcon.color = new Color(255, 255, 255, 1);
        stanceIcon.sprite = foundInfo.StanceIcon;
    }

    private void UpdateReadyStatus(bool ready)
    {
        readyStatusText.text = ready ? "Готов" : "Не готов";
        readyStatusText.color = ready ? Color.green : Color.gray;
    }

    private void OnDestroy()
    {
        if (trackedPlayer != null)
        {
            trackedPlayer.PlayerName.OnValueChanged -= OnPlayerNameChanged;
            trackedPlayer.IsReady.OnValueChanged -= OnReadyStateChanged;
            trackedPlayer.Stance.OnValueChanged -= OnStanceChanged;
        }
    }
}
