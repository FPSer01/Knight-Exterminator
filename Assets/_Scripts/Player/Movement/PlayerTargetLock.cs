using KE;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerTargetLock : NetworkBehaviour
{
    [Header("Camera Pivot")]
    [SerializeField] private Transform pivotPoint;

    [Header("Target Lock On Settings")]
    [SerializeField] private float lockOnDistance;
    [SerializeField] private float switchTargetPixelsThreshold;
    [SerializeField] private float resetSwitchTargetPixelsThreshold;
    [SerializeField] private float timeBetweenSwitch;
    [SerializeField] private float timeToResetLockOn;
    [SerializeField] private float cameraDampen;
    [SerializeField] private float switchDelayAfterKill;
    [Space]
    [SerializeField] private Vector3 lockOnShoulderOffset;
    [SerializeField] private LayerMask lockOnMask;
    [SerializeField] private LayerMask collideMask;

    [Header("UI")]
    [SerializeField] private Transform lockOnCursor;
    [SerializeField] private Transform mouseInputCursor;

    [Header("Components")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Animator animator;
    [SerializeField] private KE.CameraController cameraController;
    [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;

    private float currentResetLockTime;

    private Vector2 mouseInput;

    private Vector3 originalShoulderPoint;

    private Transform lockOnTarget = null;
    private Collider lockOnCollider = null;
    private ICameraLockable lockOnObject = null;
    private Vector2 targetPosScreenSpace;

    private bool lockedOn = false;
    private bool targetNotAbscured;
    private bool blockLockOn = false;

    public event Action<bool> OnLockOnTargetChange;
    public bool LockedOn { get => lockedOn; set => lockedOn = value; }
    public bool LockActive { get => lockedOn && targetNotAbscured; }
    public Transform Target { get => lockOnTarget; }

    private IEnumerator switchCooldownCoroutine;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        ResetLockOn();

        InputManager.Input.Player.LockOn.started += LockOnInput;
        PlayerUI.OnUIChange += PlayerUI_OnUIChange;

        OnLockOnTargetChange += CameraController_OnLockOnTargetChange;
        originalShoulderPoint = thirdPersonFollow.ShoulderOffset; 
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        InputManager.Input.Player.LockOn.started -= LockOnInput;
        PlayerUI.OnUIChange -= PlayerUI_OnUIChange;
    }

    private void Update()
    {
        mouseInput = InputManager.Input.Player.Look.ReadValue<Vector2>();

        if (lockOnTarget != null)
            mouseInputCursor.position = mouseInput + (Vector2)mainCamera.WorldToScreenPoint(lockOnTarget.position);
    }

    private void LateUpdate()
    {
        targetNotAbscured = CheckAbscureCurrentTarget();

        if (LockActive)
        {
            UpdateLockOn();
        }
    }

    private void PlayerUI_OnUIChange(GameUIWindowType type)
    {
        ResetLockOn();
    }

    private void LockOnInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!lockedOn)
            LockOnTarget();
        else
            ResetLockOn();
    }

    private void CameraController_OnLockOnTargetChange(bool lockedOn)
    {
        animator.SetBool("Camera Locked On", lockedOn);
    }

    private void LockOnTarget()
    {
        ICameraLockable closestTarget = null;
        Collider objCollider = null;

        float minDistance = float.MaxValue;

        Collider[] foundLockOnTargets = new Collider[64];

        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

        if (Physics.OverlapSphereNonAlloc(pivotPoint.position, lockOnDistance, foundLockOnTargets, lockOnMask) > 0)
        {
            foreach (var colTarget in foundLockOnTargets)
            {
                if (colTarget == null)
                    continue;

                if (!colTarget.TryGetComponent(out ICameraLockable target) || !CheckVisibility(colTarget))
                    continue;

                Transform targetLockPoint = target.GetLockOnPoint();

                if (targetLockPoint == null)
                {
                    Debug.LogError($"No Lock Point Found on {colTarget.name}!", colTarget);
                    continue;
                }

                Vector2 targetScreenPosition = mainCamera.WorldToScreenPoint(targetLockPoint.position);

                float distance = Vector2.Distance(targetScreenPosition, screenCenter);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    objCollider = colTarget;
                    closestTarget = target;
                }
            }

            if (closestTarget != null)
            {
                ActivateLockOn(closestTarget, objCollider);
            }
            else
            {
                ResetLockOn();
            }
        }
        else
        {
            ResetLockOn();
        }

        //Debug.Log($"Lock On Object: {lockOnCollider.name}, {Time.frameCount}");
    }

    private void SwitchLockOnTarget()
    {
        ICameraLockable closestTarget = null;
        float minScreenDistance = float.MaxValue;

        Collider[] foundLockOnTargets = new Collider[64];
        Collider objCollider = null;

        Vector2 oldTargetPoint = mainCamera.WorldToScreenPoint(lockOnTarget.position);
        Vector2 mousePoint = oldTargetPoint + mouseInput;

        if (Physics.OverlapSphereNonAlloc(pivotPoint.position, lockOnDistance, foundLockOnTargets, lockOnMask) > 0)
        {
            foreach (var colTarget in foundLockOnTargets)
            {
                if (colTarget == null)
                    continue;

                if (!colTarget.TryGetComponent(out ICameraLockable target) || !CheckVisibility(colTarget) || lockOnTarget == target.GetLockOnPoint())
                    continue;

                Transform targetLockPoint = target.GetLockOnPoint();

                if (targetLockPoint == null)
                {
                    Debug.LogError($"No Lock Point Found on {colTarget.name}!", colTarget);
                    continue;
                }

                Vector3 screenPos = mainCamera.WorldToScreenPoint(targetLockPoint.position);

                Vector2 targetScreenPos = new Vector2(screenPos.x, screenPos.y);

                float screenDistance = Vector2.Distance(targetScreenPos, mousePoint);

                if (screenDistance < minScreenDistance)
                {
                    minScreenDistance = screenDistance;
                    objCollider = colTarget;
                    closestTarget = target;
                }
            }

            if (closestTarget != null)
            {
                ActivateLockOn(closestTarget, objCollider);
                blockLockOn = true;
            }
        }
    }

    private bool CheckVisibility(Collider objectCollider)
    {
        Plane[] cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        Vector2 screenPoint = mainCamera.WorldToScreenPoint(objectCollider.transform.position);
        Ray rayToObj = mainCamera.ScreenPointToRay(screenPoint);
        float distanceToObj = Vector3.Distance(objectCollider.transform.position, mainCamera.transform.position);

        bool isVisible = GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, objectCollider.bounds) && !Physics.Raycast(rayToObj, distanceToObj, collideMask);

        //Debug.Log($"{objectCollider.name}: {isVisible}");
        return isVisible;
    }

    private void ResetLockOn()
    {
        lockedOn = false;

        lockOnObject?.DeleteDeathCallback(SwitchTargetOnKill);

        lockOnObject = null;
        lockOnTarget = null;
        lockOnCollider = null;

        thirdPersonFollow.ShoulderOffset = originalShoulderPoint;

        lockOnCursor.gameObject.SetActive(false);
        OnLockOnTargetChange?.Invoke(lockedOn);
    }

    private void ActivateLockOn(ICameraLockable lockObj, Collider lockCol)
    {
        if (lockOnObject == lockObj || lockOnCollider == lockCol || lockObj == null || lockCol == null)
        {
            ResetLockOn();
            return;
        }

        lockedOn = true;

        lockOnObject?.DeleteDeathCallback(SwitchTargetOnKill);

        // Задаем нужные данные о цели наведения
        lockOnObject = lockObj;
        lockOnCollider = lockCol;
        lockOnTarget = lockObj.GetLockOnPoint();

        lockOnObject?.SetDeathCallback(SwitchTargetOnKill);

        thirdPersonFollow.ShoulderOffset = lockOnShoulderOffset;
        lockOnCursor.gameObject.SetActive(true);

        OnLockOnTargetChange?.Invoke(lockedOn);
    }

    private void SwitchTargetOnKill()
    {
        StartCoroutine(DelaySwitchAfterKill(switchDelayAfterKill));
    }

    private IEnumerator DelaySwitchAfterKill(float delay)
    {
        yield return new WaitForSeconds(delay);

        LockOnTarget();
    }

    private void UpdateLockOn()
    {
        if (lockOnObject == null)
        {
            ResetLockOn();
            return;
        }

        Vector3 direction = (lockOnTarget.position - pivotPoint.position).normalized;

        pivotPoint.rotation = Quaternion.Slerp(pivotPoint.rotation, Quaternion.LookRotation(direction, Vector3.up), cameraDampen * Time.deltaTime);
        cameraController.CamRotation = pivotPoint.eulerAngles;

        targetPosScreenSpace = mainCamera.WorldToScreenPoint(lockOnTarget.position);
        lockOnCursor.position = new Vector3(targetPosScreenSpace.x, targetPosScreenSpace.y, 0);

        if (mouseInput.magnitude >= switchTargetPixelsThreshold && !blockLockOn)
        {
            SwitchLockOnTarget();
        }
        else if (mouseInput.magnitude < resetSwitchTargetPixelsThreshold && blockLockOn)
        {
            if (switchCooldownCoroutine == null)
            {
                switchCooldownCoroutine = SwitchCooldown();
                StartCoroutine(switchCooldownCoroutine);
            }
        }
    }

    private bool CheckAbscureCurrentTarget()
    {
        if (lockOnTarget == null || lockOnCollider == null)
            return false;

        if (!CheckVisibility(lockOnCollider))
        {
            currentResetLockTime -= Time.deltaTime;
        }
        else
        {
            currentResetLockTime = timeToResetLockOn;
        }

        if (currentResetLockTime <= 0)
        {
            currentResetLockTime = timeToResetLockOn;
            ResetLockOn();

            return false;
        }

        return true;
    }

    private IEnumerator SwitchCooldown()
    {
        yield return new WaitForSeconds(timeBetweenSwitch);

        blockLockOn = false;
        switchCooldownCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(pivotPoint.position, lockOnDistance);
    }
}
