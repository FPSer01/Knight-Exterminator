using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMapWindow : PlayerUIWindow
{
    [Header("Map Camera")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private float cameraMoveSpeed;
    [SerializeField] private float zoomSpeed;
    [SerializeField] private float zoomMin;
    [SerializeField] private float zoomMax;
    private float originalZoom;
    [Space]
    [SerializeField] private LayerMask UILayerMask;
    [SerializeField] private LayerMask mapLayerMask;

    [Header("UI")]
    [SerializeField] private UIButton closeButton;

    private bool active;

    private bool canGrabMove = false;
    private bool grabMoving = false;

    private Vector2 mousePos;
    private Vector3 grabMovePoint;

    private IMapTeleportProvider currentTPProvider;

    private void Start()
    {
        originalZoom = mapCamera.orthographicSize;
        closeButton.onClick.AddListener(CloseMap);
    }

    public override void SetWindowActive(bool active, float timeToSwitch = 0.1f)
    {
        ResetGrabMove();

        if (active)
        { 
            ResetMapCamera();
            InputManager.Input.Map.Enable();
        }
        else
        {
            currentTPProvider?.Highlight(false);
            currentTPProvider = null;

            InputManager.Input.Map.Disable();
        }

        mapCamera.gameObject.SetActive(active);
        this.active = active;

        base.SetWindowActive(active, timeToSwitch);
    }

    private void OnEnable()
    {
        InputManager.Input.Map.LeftClick.started += LeftClick_started;
        InputManager.Input.Map.LeftClick.canceled += LeftClick_canceled;
        InputManager.Input.Map.ScrollWheel.started += ChangeZoom;
    }

    private void OnDisable()
    {
        InputManager.Input.Map.LeftClick.started -= LeftClick_started;
        InputManager.Input.Map.LeftClick.canceled -= LeftClick_canceled;
        InputManager.Input.Map.ScrollWheel.started -= ChangeZoom;
    }

    private void Update()
    {
        if (!active)
            return;

        mousePos = InputManager.Input.Map.MousePos.ReadValue<Vector2>();

        CameraMove();
        CheckForMapRoomSelect();
    }

    private void CheckForMapRoomSelect()
    {
        if (grabMoving || !active)
            return;

        Ray ray = mapCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mapLayerMask)) // Телепорт в комнату
        {
            canGrabMove = false;

            if (hit.collider.TryGetComponent(out IMapTeleportProvider teleportProvider))
            {
                if (currentTPProvider != teleportProvider || currentTPProvider == null)
                {
                    currentTPProvider?.Highlight(false);

                    currentTPProvider = teleportProvider;
                    currentTPProvider?.Highlight(true);
                }
            }
        }
        else // Перемещение мышью
        {
            canGrabMove = true;       
            currentTPProvider?.Highlight(false);
            currentTPProvider = null;
        }

        //Debug.Log($"[Map] Can Grab Move: {canGrabMove}");
    }

    private void LeftClick_started(InputAction.CallbackContext obj)
    {
        if (IsPointerOverUIElement() || !active)
            return;

        if (!canGrabMove && currentTPProvider != null)
            TeleportPlayer();

        SetGrabMove();
    }

    private void LeftClick_canceled(InputAction.CallbackContext obj)
    {
        ResetGrabMove();
    }

    private void TeleportPlayer()
    {
        var point = currentTPProvider.GetTeleportPoint();

        var playerRB = playerUI.gameObject.GetComponent<Rigidbody>();
        playerRB.position = point;

        CloseMap();
    }

    #region Move and Zoom

    private void CameraMove()
    {
        if (grabMoving) // Перемещение мышью
        {
            Vector3 difference = grabMovePoint - mapCamera.ScreenToWorldPoint(mousePos);
            difference.y = 0;

            mapCamera.transform.position = mapCamera.transform.position + difference;
        }
        else // Управление с клавы
        {
            Vector2 moveInput = InputManager.Input.Map.MoveMap.ReadValue<Vector2>();
            Vector3 moveDir = Vector3.forward * moveInput.y + Vector3.right * moveInput.x;
            float zoomRatio = mapCamera.orthographicSize / originalZoom;

            mapCamera.transform.Translate(cameraMoveSpeed * zoomRatio * Time.deltaTime * moveDir.normalized, Space.World);
        }
    }

    private void SetGrabMove()
    {
        grabMoving = true;

        var point = mapCamera.ScreenToWorldPoint(mousePos);
        grabMovePoint = new Vector3(point.x, mapCamera.transform.position.y, point.z);
    }

    private void ResetGrabMove()
    {
        grabMoving = false;

        if (canGrabMove)
            canGrabMove = false;

        grabMovePoint = new Vector3(0, mapCamera.transform.position.y, 0);
    }

    private void ResetMapCamera()
    {
        mapCamera.orthographicSize = originalZoom;
        mapCamera.transform.position = new Vector3(playerUI.transform.position.x, mapCamera.transform.position.y, playerUI.transform.position.z);
    }

    private void ChangeZoom(InputAction.CallbackContext context)
    {
        float scrollWheelValue = context.ReadValue<float>();

        float zoomAmount = scrollWheelValue > 0 ? zoomSpeed : -zoomSpeed;
        zoomAmount = Mathf.Clamp(mapCamera.orthographicSize + zoomAmount, zoomMin, zoomMax);

        mapCamera.orthographicSize = zoomAmount;
    }

    #endregion

    #region Check UI Overlap Methods

    public bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }

    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if ((UILayerMask.value & (1 << curRaysastResult.gameObject.layer)) != 0)
                return true;
        }
        return false;
    }

    private List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = mousePos;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }

    #endregion

    private void CloseMap()
    {
        ResetGrabMove();
        playerUI.SetWindow(GameUIWindowType.HUD);
    }
}
