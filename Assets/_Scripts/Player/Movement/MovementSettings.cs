using System;
using UnityEngine;

[Serializable]
public class MovementSettings
{
    [Header("Move")]
    public float MoveSpeed;
    [SerializeField] private float acceleration;
    public float TurnSpeed;
    public float SprintMoveSpeedMult;
    [Space]
    public float OverallMoveSpeedMult;
    [SerializeField] private float moveSpeedMult;

    public float AirMoveMult;

    [Header("Drag")]
    public float GroundDrag;
    public float AirDrag;

    [Header("Jump")]
    public float JumpTime;
    public float JumpSpeed;
    public float JumpBufferTime;
    public float FallTimeThreshold;

    [Header("Slope")]
    public float MaxSlopeAngle;
    public float SlopeForceDown;

    [Header("Dodge")]
    public float DodgeTime;
    public float DodgeDistance;
    public float DodgeCooldownTime;
    public bool EnableDodge4Way;

    public float Acceleration
    {
        get
        {
            return acceleration * OverallMoveSpeedMult;
        }
        set
        {
            acceleration = value;
        }
    }

    public float MoveSpeedMult
    {
        get
        {
            return moveSpeedMult * OverallMoveSpeedMult;
        }
        set
        {
            moveSpeedMult = value;
        }
    }
}
