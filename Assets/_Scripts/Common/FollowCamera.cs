using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    private Camera trackedCamera;
    private Transform trackedCameraTransform;

    [SerializeField] private Vector3 followOffset;

    private void Start()
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
            transform.position = trackedCameraTransform.position + followOffset;
        }
    }
}
