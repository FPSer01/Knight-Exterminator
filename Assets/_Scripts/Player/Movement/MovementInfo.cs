using System;
using UnityEngine;

[Serializable]
public class MovementInfo
{
    [Header("Velocity")]
    public float Velocity;
    public Vector3 AxisVelocity;
    public float PlaneVelocity;
    public Vector3 PlaneAxisVelocity;

    [Header("Move")]
    public bool Sprinting;
    public bool StartJumping;
    public bool Dodging;
    public bool DodgeCooldown;

    [Header("Ground")]
    public bool OnGround;
    public bool Fall;
    public bool JumpBuffered;
    public float CurrentJumpBufferTime;

    [Header("Slope")]
    public bool OnSlope;
    public float CurrentSlopeAngle;
}
