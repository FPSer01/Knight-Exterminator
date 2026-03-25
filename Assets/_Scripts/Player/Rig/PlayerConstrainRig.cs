using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerConstrainRig : MonoBehaviour
{
    [SerializeField] private Rig rig;
    [SerializeField] private bool useRig;

    [Header("Points and Directions")]
    [SerializeField] private Transform aimPoint;
    private Transform aimPointParent;
    [SerializeField] private Transform aimPointOriginal;
    [Space]
    [SerializeField] private Transform cameraDirection;
    [SerializeField] private Transform playerDirection;

    [Header("Settings")]
    [SerializeField] private bool constrainRotation;
    [SerializeField] private float dampen;
    [SerializeField] private float minAngle;
    [SerializeField] private float maxAngle;

    private Vector3 localOriginalAimPoint;

    private void Awake()
    {
        aimPointParent = aimPoint.parent;
        localOriginalAimPoint = aimPoint.localPosition;
    }

    private void Update()
    {
        if (constrainRotation)
        {
            ConstrainRotation();
        }
        else
        {
            ConstrainPosition();
        }
    }

    private void ConstrainRotation()
    {
        float angle = Quaternion.Angle(cameraDirection.rotation, playerDirection.rotation);

        if (angle >= minAngle && angle <= maxAngle)
        {
            if (useRig)
            {
                rig.weight = Mathf.Lerp(rig.weight, 1, dampen * Time.deltaTime);
            }

            aimPoint.rotation = Quaternion.Slerp(aimPoint.rotation, aimPointOriginal.rotation, dampen * Time.deltaTime);
        }
        else
        {
            if (useRig)
            {
                rig.weight = Mathf.Lerp(rig.weight, 0, dampen * Time.deltaTime);
            }
            else
            {
                aimPoint.rotation = Quaternion.Slerp(aimPoint.rotation, playerDirection.rotation, dampen * Time.deltaTime);
            }
        }
    }

    private void ConstrainPosition()
    {
        float angle = Quaternion.Angle(cameraDirection.rotation, playerDirection.rotation);

        if (angle >= minAngle && angle <= maxAngle)
        {
            if (useRig)
            {
                rig.weight = Mathf.Lerp(rig.weight, 1, dampen * Time.deltaTime);
            }

            aimPoint.position = Vector3.Lerp(aimPoint.position, aimPointOriginal.position, dampen * Time.deltaTime);
        }
        else
        {
            if (useRig)
            {
                rig.weight = Mathf.Lerp(rig.weight, 0, dampen * Time.deltaTime);
            }
            else
            {
                aimPoint.position = Vector3.Lerp(aimPoint.position, aimPointParent.TransformPoint(localOriginalAimPoint), dampen * Time.deltaTime);
            }
        }
    }
}
