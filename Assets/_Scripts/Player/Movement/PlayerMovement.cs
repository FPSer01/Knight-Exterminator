using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>
/// Класс передвижения пользователя от третьего лица
/// </summary>
public class PlayerMovement : NetworkBehaviour
{
    // Нужное
    private Vector2 moveInput;
    private Vector3 moveDir;
    private Vector3 planeMoveDir;

    [Header("General")]
    [SerializeField] private Transform cameraDirection;

    [Header("Settings")]
    [SerializeField] private MovementSettings movementSettings;
    [SerializeField] private MovementInfo movementInfo;

    [Header("Checks")]
    [SerializeField] private Transform groundPoint;
    [SerializeField] private Vector3 groundCheckSize;
    [SerializeField] private float groundCheckLength;
    [SerializeField] private LayerMask groundLayerMask;

    [Header("Components")]
    [SerializeField] private PlayerComponents playerComponents;
    private Rigidbody rb => playerComponents.Rigidbody;
    private Collider col => playerComponents.CapsuleCollider;
    private Animator animator => playerComponents.Animator;
    private KE.CameraController cameraController => playerComponents.CameraController;
    private PlayerTargetLock targetLockController => playerComponents.TargetLockSystem;
    private PlayerStamina playerStamina => playerComponents.Stamina;
    private PlayerSFXController sfxController => playerComponents.SfxController;

    [Header("Effects")]
    [SerializeField] private ParticleSystem groundSlamVFX;
    [SerializeField] private float groundSlamMinVelocity;
    [SerializeField] private List<ParticleSystem> jumpUpVFX;

    [Header("Other")]
    [SerializeField] private LayerMask dodgeExcludeMask;

    private Coroutine fallThresholdCoroutine;

    private RaycastHit slopeHit;

    private float moveSpeedNorm = 0f;
    private Vector2 moveInputSpeed;

    private bool blockMoveSFXChange = false;
    private bool blockMovement = false;
    private bool blockTurn = false;
    private bool blockDodging = false;

    public bool Dodging { get => movementInfo.Dodging; }
    public bool Sprinting { get => movementInfo.Sprinting; }
    public bool OnGround { get => movementInfo.OnGround; }
    public Vector2 MoveInput { get => moveInput; }

    public float MoveSpeed { get => movementSettings.MoveSpeed; set => movementSettings.MoveSpeed = value; }
    public float OverallMoveSpeedMult { get => movementSettings.OverallMoveSpeedMult; set => ChangeOverallMoveSpeedMult(value); }

    public bool Thrusting = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        SubToInputEvents(true);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        SubToInputEvents(false);
    }

    private void SubToInputEvents(bool subscribe)
    {
        if (subscribe)
        {
            InputManager.Input.Player.Sprint.started += BeginSprintInput;
            InputManager.Input.Player.Sprint.canceled += EndSprintInput;

            InputManager.Input.Player.Jump.started += JumpInput;
            InputManager.Input.Player.Dodge.started += DodgeInput;
        }
        else
        {
            EndSprint();

            InputManager.Input.Player.Sprint.started -= BeginSprintInput;
            InputManager.Input.Player.Sprint.canceled -= EndSprintInput;

            InputManager.Input.Player.Jump.started -= JumpInput;
            InputManager.Input.Player.Dodge.started -= DodgeInput;
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        CheckGroundAndSlope();
        HandleInputs();

        rb.linearDamping = GetDamping();

        rb.useGravity = !movementInfo.OnSlope && !movementInfo.StartJumping;

        // Лимит скорости если много кадров
        if (Time.deltaTime < Time.fixedDeltaTime)
        {
            LimitSpeed();
        }

        CheckMoveState();
        UpdateMoveSFX();

        // Инфа для дебага и не только (может быть)
        movementInfo.AxisVelocity = rb.linearVelocity;
        movementInfo.Velocity = movementInfo.AxisVelocity.magnitude;
        movementInfo.PlaneAxisVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        movementInfo.PlaneVelocity = movementInfo.PlaneAxisVelocity.magnitude;

        animator.SetFloat("Vertical Velocity", rb.linearVelocity.y);
        AnimationMoveInput();
        AnimationMoveSpeedNorm();
    }

    private void FixedUpdate()
    {
        if (!movementInfo.Dodging && !Thrusting && !blockMovement && IsOwner)
        {
            Move();
        }
    }

    #region Inputs

    // Прыжок
    public void JumpInput(InputAction.CallbackContext context)
    {
        movementInfo.JumpBuffered = !movementInfo.Dodging && !Thrusting && !blockMovement;
    }

    // Начать бег
    public void BeginSprintInput(InputAction.CallbackContext context)
    {
        if (Thrusting || blockMovement)
            return;

        if (movementInfo.OnGround && !movementInfo.Sprinting && playerStamina.CurrentStamina > 0)
        {
            StartSprint();
        }
    }

    // Закончить бег
    public void EndSprintInput(InputAction.CallbackContext context)
    {
        if (movementInfo.Sprinting)
        {
            EndSprint();
        }
    }

    // Перекат
    public void DodgeInput(InputAction.CallbackContext context)
    {
        if (movementInfo.Dodging || movementInfo.DodgeCooldown || blockMovement || blockDodging)
            return;

        if (movementInfo.OnGround && moveInput != Vector2.zero && playerStamina.CurrentStamina >= StaminaConsumage.DODGE)
        {
            StartCoroutine(InitiateDodge());
        }
    }

    // Управление вводом и связанными с ними процессами
    private void HandleInputs()
    {
        if (!IsOwner)
            return;

        // Движение
        if (!blockMovement)
        {
            moveInput = InputManager.Input.Player.Move.ReadValue<Vector2>();
            animator.SetBool("Move Input", moveInput != Vector2.zero);
            CalculateMoveDirection();
        }

        if (moveInput != Vector2.zero)
        {
            TurnPlayer();
        }

        // Баффер прыжка
        if (movementInfo.JumpBuffered)
        {
            movementInfo.CurrentJumpBufferTime += Time.deltaTime;

            if (movementInfo.CurrentJumpBufferTime <= movementSettings.JumpBufferTime && movementInfo.OnGround)
            {
                Jump();
            }
            else if (movementInfo.CurrentJumpBufferTime > movementSettings.JumpBufferTime)
            {
                ResetJumpBuffer();
            }
        }
    }

    private void TurnPlayer()
    {
        if (Thrusting || blockTurn)
            return;

        if (!targetLockController.LockedOn && !movementInfo.Dodging)
            rb.rotation = Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(planeMoveDir, Vector3.up), movementSettings.TurnSpeed * Time.deltaTime);
        else if (targetLockController.LockedOn)
        {
            if (movementInfo.Dodging)
            {
                rb.rotation = Quaternion.LookRotation(planeMoveDir, Vector3.up);
            }
            else
            {
                rb.rotation = Quaternion.Slerp(
                    rb.rotation,
                    Quaternion.LookRotation(Vector3.ProjectOnPlane(cameraController.PivotForward, Vector3.up), Vector3.up),
                    movementSettings.TurnSpeed * Time.deltaTime
                    );
            }
        }
    }

    #endregion

    #region Move

    private float GetDamping()
    {
        if (movementInfo.OnGround && (movementInfo.OnSlope || movementInfo.CurrentSlopeAngle == 0f) && !movementInfo.Dodging && !Thrusting)
            return movementSettings.GroundDrag;
        else
            return movementSettings.AirDrag;
    }

    private void Move()
    {
        float targetSpeedValue = movementSettings.MoveSpeed * movementSettings.MoveSpeedMult;
        float targetAcceleration = movementInfo.OnGround ?
            movementSettings.Acceleration * Time.fixedDeltaTime : 
            movementSettings.Acceleration * Time.fixedDeltaTime * movementSettings.AirMoveMult;

        if (movementInfo.OnGround && !movementInfo.StartJumping)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetSpeedValue * moveDir, targetAcceleration);
        }
        else
        {
            rb.linearVelocity = new Vector3(
                Mathf.Lerp(rb.linearVelocity.x, targetSpeedValue * moveDir.x, targetAcceleration),
                rb.linearVelocity.y,
                Mathf.Lerp(rb.linearVelocity.z, targetSpeedValue * moveDir.z, targetAcceleration)
                );
        }

        // Лимит скорости если мало кадров
        if (Time.deltaTime >= Time.fixedDeltaTime)
        {
            LimitSpeed();
        }
    }

    private void LimitSpeed()
    {
        if (movementInfo.Dodging || Thrusting)
            return;

        if (movementInfo.OnGround && !movementInfo.StartJumping)
        {
            Vector3 velocity = rb.linearVelocity;

            if (velocity.magnitude > movementSettings.MoveSpeed * movementSettings.MoveSpeedMult)
            {
                Vector3 limitedVelocity = movementSettings.MoveSpeed * movementSettings.MoveSpeedMult * velocity.normalized;
                rb.linearVelocity = limitedVelocity;
            }
        }
    }

    private void CalculateMoveDirection()
    {
        Vector3 forwardDirection = Vector3.ProjectOnPlane(cameraDirection.forward, Vector3.up);
        Vector3 rightDirection = Vector3.ProjectOnPlane(cameraDirection.right, Vector3.up);

        moveDir = (forwardDirection * moveInput.y + rightDirection * moveInput.x).normalized;
        planeMoveDir = moveDir;

        if (slopeHit.collider != null && movementInfo.OnSlope && !movementInfo.StartJumping)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, slopeHit.normal);
            rb.AddForce(slopeHit.normal * -movementSettings.SlopeForceDown, ForceMode.Acceleration);
        }
    }

    private void CheckMoveState()
    {
        if (movementInfo.Sprinting && moveInput != Vector2.zero)
        {
            if (playerStamina.CurrentStamina <= 0)
            {
                EndSprint();
            }
            else
            {
                movementSettings.MoveSpeedMult = movementSettings.SprintMoveSpeedMult;
            }
        }
        else if (movementInfo.OnGround)
        {
            movementSettings.MoveSpeedMult = 1;
        }
    }

    public void StartSprint()
    {
        playerStamina.ConsumeStaminaContinuously(true, StaminaConsumage.SPRINT);

        movementInfo.Sprinting = true;

        StopMoveSFX();
        PlayMoveSFX();
    }

    public void EndSprint()
    {
        playerStamina.ConsumeStaminaContinuously(false);

        movementInfo.Sprinting = false;

        StopMoveSFX();
    }

    #endregion

    #region Jump And Slope Movement

    private void CheckGroundAndSlope()
    {
        bool ground = Physics.BoxCast(
            groundPoint.position,
            groundCheckSize / 2,
            -transform.up,
            out slopeHit,
            transform.rotation,
            groundCheckLength,
            groundLayerMask);

        float slopeAngle = MathF.Round(Vector3.Angle(Vector3.up, slopeHit.normal), 2);
        bool slope = movementInfo.CurrentSlopeAngle <= movementSettings.MaxSlopeAngle && movementInfo.CurrentSlopeAngle != 0;

        // Если превышается угол ремпы, то не на земле
        if (!slope && slopeAngle > movementSettings.MaxSlopeAngle)
        {
            ground = false;
        }

        // Смена состояния OnGround
        if (movementInfo.OnGround != ground)
        {
            animator.SetBool("OnGround", ground);
            movementInfo.OnGround = ground;

            if (ground && movementInfo.Fall)
                PlayFallEffects();

            CheckForFall(!ground);
        }

        // Смена состояния OnSlope
        if (movementInfo.OnSlope != slope)
        {
            movementInfo.OnSlope = slope;
        }

        movementInfo.CurrentSlopeAngle = slopeAngle;
    }

    private void Jump()
    {
        if (playerStamina.CurrentStamina < StaminaConsumage.JUMP)
            return;

        ResetJumpBuffer();

        animator.SetTrigger("Jump Input");
        PlayJumpSFX();
        playerStamina.ConsumeStamina(StaminaConsumage.JUMP);

        StartCoroutine(InitiateJump());
    }

    private IEnumerator InitiateJump()
    {
        movementInfo.StartJumping = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        yield return null;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, movementSettings.JumpSpeed, rb.linearVelocity.z);

        EnableJumpVFX(true);

        while (movementInfo.OnGround)
            yield return null;

        yield return new WaitForSeconds(movementSettings.JumpTime);

        EnableJumpVFX(false);

        movementInfo.StartJumping = false;
    }

    private void ResetJumpBuffer()
    {
        movementInfo.JumpBuffered = false;
        movementInfo.CurrentJumpBufferTime = 0f;
    }

    private void CheckForFall(bool check)
    {
        if (check)
        {
            fallThresholdCoroutine = StartCoroutine(WaitForFallThreshold());
        }
        else
        {
            if (fallThresholdCoroutine != null)
            {
                StopCoroutine(fallThresholdCoroutine);
                fallThresholdCoroutine = null;
            }

            movementInfo.Fall = false;
        }
    }

    private IEnumerator WaitForFallThreshold()
    {
        yield return new WaitForSeconds(movementSettings.FallTimeThreshold);

        movementInfo.Fall = true;
    }

    #endregion

    #region Dodge

    private IEnumerator InitiateDodge()
    {
        playerStamina.ConsumeStamina(StaminaConsumage.DODGE);
        StopMoveSFX();
        PlayDodgeSFX();
        EnableJumpVFX(true);

        movementInfo.Dodging = true;
        animator.SetBool("Dodge", movementInfo.Dodging);
        playerComponents.AddExcludeLayers(dodgeExcludeMask);

        playerComponents.ActivateRig(false);
        BlockTurn(true);

        float speed = movementSettings.DodgeDistance / movementSettings.DodgeTime;

        rb.linearVelocity = moveDir * speed;

        yield return new WaitForSeconds(movementSettings.DodgeTime);

        movementInfo.Dodging = false;
        animator.SetBool("Dodge", movementInfo.Dodging);
        playerComponents.RemoveExcludeLayers(dodgeExcludeMask);
        EnableJumpVFX(false);

        playerComponents.ActivateRig(true);
        BlockTurn(false);

        movementInfo.DodgeCooldown = true;

        yield return new WaitForSeconds(movementSettings.DodgeCooldownTime);

        movementInfo.DodgeCooldown = false;
    }

    public void BlockDodging(bool active)
    {
        blockDodging = active;
    }

    #endregion

    #region Animation Stuff

    private void ChangeOverallMoveSpeedMult(float newMult)
    {
        movementSettings.OverallMoveSpeedMult = newMult;
        animator.SetFloat("Move Speed Mult", movementSettings.OverallMoveSpeedMult);
    }

    private void AnimationMoveSpeedNorm()
    {
        float target = 0;

        if (moveInput == Vector2.zero)
            target = 0;
        else
            if (!movementInfo.Sprinting)
                target = 0.5f;
            else
                target = 1f;

        moveSpeedNorm = Mathf.Lerp(moveSpeedNorm, target, movementSettings.MoveSpeed * movementSettings.SprintMoveSpeedMult * Time.deltaTime);

        animator.SetFloat("Move Speed Norm", moveSpeedNorm);
    }

    private void AnimationMoveInput()
    {
        Vector2 target = Vector2.zero;

        if (moveInput == Vector2.zero)
            target = Vector2.zero;
        else
            if (!movementInfo.Sprinting)
            target = moveInput * 0.5f;
        else
            target = moveInput;

        moveInputSpeed = Vector2.Lerp(moveInputSpeed, target, movementSettings.MoveSpeed * movementSettings.SprintMoveSpeedMult * Time.deltaTime);

        animator.SetFloat("Move Input X", moveInputSpeed.x);
        animator.SetFloat("Move Input Y", moveInputSpeed.y);
    }

    #endregion

    #region SFX, VFX and Other

    private void UpdateMoveSFX()
    {
        if (blockMovement || Dodging || Thrusting)
        {
            StopMoveSFX();
            return;
        }

        if (movementInfo.OnGround && moveInput != Vector2.zero)
        {
            PlayMoveSFX();
        }
        else
        {
            StopMoveSFX();
        }
    }

    private void PlayMoveSFX()
    {
        if (blockMoveSFXChange)
            return;

        if (movementInfo.Sprinting)
        {
            sfxController.PlayRunSFX();
        }
        else
        {
            sfxController.PlayWalkSFX();
        }

        blockMoveSFXChange = true;
    }

    private void StopMoveSFX()
    {
        if (!blockMoveSFXChange)
            return;

        sfxController.StopMovementSFX();
        blockMoveSFXChange = false;
    }

    #region Fall Effects

    private void PlayFallEffects()
    {
        sfxController.PlayFallSFX();

        ExecutePlayFallVFX();
        PlayFallVFX_ToServerRpc();
    }

    private void ExecutePlayFallVFX()
    {
        groundSlamVFX.Play();
    }

    [Rpc(SendTo.Server)]
    private void PlayFallVFX_ToServerRpc()
    {
        PlayFallVFX_ToEveryoneRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayFallVFX_ToEveryoneRpc()
    {
        ExecutePlayFallVFX();
    }

    #endregion

    private void PlayDodgeSFX()
    {
        sfxController.PlayDodgeSFX();
    }

    #region Jump VFX

    private void EnableJumpVFX(bool enable)
    {
        ExecuteEnableJumpVFX(enable);
        EnableJumpVFX_ToServerRpc(enable);
    }

    private void ExecuteEnableJumpVFX(bool enable)
    {
        jumpUpVFX.ForEach(vfx =>
        {
            if (enable)
                vfx.Play();
            else
                vfx.Stop();
        });
    }

    [Rpc(SendTo.Server)]
    private void EnableJumpVFX_ToServerRpc(bool enable)
    {
        EnableJumpVFX_ToEveryoneRpc(enable);
    }

    [Rpc(SendTo.NotOwner)]
    private void EnableJumpVFX_ToEveryoneRpc(bool enable)
    {
        ExecuteEnableJumpVFX(enable);
    }

    #endregion

    private void PlayJumpSFX()
    {
        sfxController.PlayJumpSFX();
    }

    #endregion

    #region Thrust

    public void Thrust(float distance, float time, float delay = 0)
    {
        StartCoroutine(InitiateThrust(distance, time, delay));
    }

    private IEnumerator InitiateThrust(float distance, float time, float delay)
    {
        Thrusting = true;
        col.excludeLayers = dodgeExcludeMask;

        float speed = distance / time;

        rb.useGravity = false;
        rb.linearVelocity = transform.forward * speed;

        yield return new WaitForSeconds(time);

        rb.useGravity = true;
        col.excludeLayers = 0;

        yield return new WaitForSeconds(delay);
        Thrusting = false;
    }

    #endregion

    #region Blockers

    public void BlockMovement(bool block)
    {
        blockMovement = block;
        rb.isKinematic = block;

        if (block)
        {
            EndSprint();
            StopMoveSFX();
            rb.linearVelocity = Vector3.zero;
        }
    }

    public void BlockTurn(bool block)
    {
        blockMovement = block;
    }

    #endregion

    [Rpc(SendTo.Owner)]
    public void RequestTeleport_OwnerRpc(Vector3 position)
    {
        rb.linearVelocity = Vector3.zero;
        rb.position = position;

        transform.position = position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one
        );

        if (groundPoint != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireCube(
                groundPoint.localPosition,
                groundCheckSize
                );

            Gizmos.DrawRay(groundPoint.localPosition, Vector3.down * groundCheckLength);
            Gizmos.DrawWireCube(
                new Vector3(groundPoint.localPosition.x, groundPoint.localPosition.y - groundCheckLength, groundPoint.localPosition.z),
                groundCheckSize
                );
        }
    }
}