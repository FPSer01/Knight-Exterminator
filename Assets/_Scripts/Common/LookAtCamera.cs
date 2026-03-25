using Unity.Netcode;
using UnityEngine;

public class LookAtCamera : NetworkBehaviour
{
    private Camera trackedCamera;
    private Transform trackedCameraTransform;

    [SerializeField] private Vector3 rotationOffset;

    public override void OnNetworkSpawn()
    {
        FindMainClientCam();
    }

    private void FindMainClientCam()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        foreach (var camera in cameras)
        {
            if (camera.enabled && camera.gameObject.activeInHierarchy)
            {
                if (camera.gameObject.CompareTag("MainCamera"))
                {
                    trackedCamera = camera;
                    trackedCameraTransform = camera.transform;
                    return;
                }
            }
            else
            {
                continue;
            }
        }
    }

    private void Update()
    {
        if (trackedCameraTransform == null || trackedCamera == null || !trackedCamera.enabled)
        {
            FindMainClientCam();
        }
        else
        {
            Quaternion rotation = Quaternion.LookRotation(trackedCameraTransform.position - transform.position, Vector3.up) * Quaternion.Euler(rotationOffset);
            transform.rotation = rotation;
        }
    }
}
