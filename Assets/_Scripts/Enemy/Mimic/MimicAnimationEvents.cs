using System;
using UnityEngine;

public class MimicAnimationEvents : MonoBehaviour
{
    public event Action OnGroundTouch;

    public void GroundTouchEvent()
    {
        OnGroundTouch?.Invoke();
    }
}
