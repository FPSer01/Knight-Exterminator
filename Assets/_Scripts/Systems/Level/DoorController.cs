using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Collider safeCollider;

    [Header("Simple door")]
    [SerializeField] private Transform rightDoor;
    [SerializeField] private Vector3 rightOpenAngle;
    [SerializeField] private Vector3 rightCloseAngle;
    [Space]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Vector3 leftOpenAngle;
    [SerializeField] private Vector3 leftCloseAngle;

    [Header("Gate door")]
    [SerializeField] private Transform gateDoor;
    [Space]
    [SerializeField] private Vector3 openPosition;
    [SerializeField] private Vector3 closePosition;

    private bool animating;

    public void SetSimpleDoors(bool open, float duration)
    {
        if (rightDoor == null || leftDoor == null || animating)
            return;

        animating = true;

        safeCollider.enabled = !open;

        DOTween.To(() => rightDoor.localEulerAngles, (angle) => rightDoor.localEulerAngles = angle, open ? rightOpenAngle : rightCloseAngle, duration).SetTarget(rightDoor);
        DOTween.To(() => leftDoor.localEulerAngles, (angle) => leftDoor.localEulerAngles = angle, open ? leftOpenAngle : leftCloseAngle, duration).SetTarget(leftDoor)
            .OnComplete(() => animating = false);
    }

    public void SetGateDoors(bool open, float duration)
    {
        if (gateDoor == null || animating)
            return;

        animating = true;

        safeCollider.enabled = !open;
        Vector3 endValue = open ? openPosition : closePosition;

        DOTween.To(() => gateDoor.localPosition, (localPos) => gateDoor.localPosition = localPos, endValue, duration)
            .SetTarget(gateDoor).OnComplete(() => animating = false);
    }
}
