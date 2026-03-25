using System;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace KE
{
    /// <summary>
    /// Класс для контроля камеры игрока от третьего лица
    /// </summary>
    public class CameraController : NetworkBehaviour
    {
        [Header("Pivot")]
        [SerializeField] private Transform pivotPoint; // Точка поворота

        [Header("Settings")]
        [SerializeField] private SettingsData settingsData;
        [SerializeField] private float sensitivity => settingsData.Sensitivity;
        [SerializeField] private float sensitivityMult;
        [Space]
        [SerializeField] private CameraBoundsSettings AngleBounds_X;

        [Header("Follow Point")]
        [SerializeField] private Transform followPoint; // Точка слежения камерой
        [Space]
        [SerializeField] private float distanceToCollision;
        [SerializeField] private float radiusCheck;
        [SerializeField] private LayerMask collideMask;

        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Animator animator;

        [Header("Camera Components")]
        [SerializeField] private PlayerTargetLock targetLockController;
        [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;

        private Vector2 mouseInput;
        private Vector3 camRotation;

        private Vector3 originalFollowPoint;
        private float originalDistanceFollow;

        public Vector3 PivotForward { get => pivotPoint.forward; }
        public Vector3 CamRotation { get => camRotation; set => camRotation = value; }

        public static void LockCursor(bool isLocked)
        {
            if (isLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Start()
        {
            LockCursor(true);

            camRotation = pivotPoint.eulerAngles;
            originalFollowPoint = followPoint.localPosition;
            originalDistanceFollow = thirdPersonFollow.CameraDistance;
        }

        private void OnEnable()
        {
            targetLockController.OnLockOnTargetChange += TargetLockController_OnLockOnTargetChange;
        }

        private void OnDisable()
        {
            targetLockController.OnLockOnTargetChange -= TargetLockController_OnLockOnTargetChange;
        }

        private void TargetLockController_OnLockOnTargetChange(bool lockedOn)
        {
            if (!lockedOn)
            {
                //camRotation = targetLockController.LockRotation;
                pivotPoint.rotation = Quaternion.Euler(camRotation);
            }
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            mouseInput = InputManager.Input.Player.Look.ReadValue<Vector2>();

            if (!targetLockController.LockActive)
            {
                RotateCamera();
            }

        }

        private void LateUpdate()
        {
            CheckFollowCollision();
        }

        #region Rotate Camera

        private void RotateCamera()
        {
            camRotation.x -= mouseInput.y * sensitivity * sensitivityMult * Time.fixedDeltaTime;
            camRotation.y += mouseInput.x * sensitivity * sensitivityMult * Time.fixedDeltaTime;
            camRotation.x = Mathf.Clamp(camRotation.x, AngleBounds_X.Min, AngleBounds_X.Max);

            pivotPoint.rotation = Quaternion.Euler(camRotation.x, camRotation.y, camRotation.z); // Камера
            //targetLockController.LockRotation = pivotPoint.rotation.eulerAngles;
        }

        private void CheckFollowCollision()
        {
            RaycastHit hit;

            if (Physics.SphereCast(pivotPoint.position, radiusCheck, pivotPoint.forward, out hit, distanceToCollision, collideMask))
            {
                Vector3 localPos = pivotPoint.InverseTransformPoint(hit.point);
                (localPos.x, localPos.y) = (0, 0);

                float offset = radiusCheck * 1.1f;

                localPos.z -= offset;

                thirdPersonFollow.CameraDistance = originalDistanceFollow - (originalFollowPoint.z - localPos.z);

                followPoint.localPosition = localPos;
            }
            else
            {
                thirdPersonFollow.CameraDistance = originalDistanceFollow;
                followPoint.localPosition = originalFollowPoint;
            }
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pivotPoint.position, pivotPoint.forward * distanceToCollision);
            Gizmos.DrawWireSphere(followPoint.position, radiusCheck);
        }

        [Serializable]
        public struct CameraBoundsSettings
        {
            public float Min;
            public float Max;
        }
    }
}
