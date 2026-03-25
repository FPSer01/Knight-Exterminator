using System;
using UnityEngine;
using static PamukAI.PAI;

public class MimicBehaviour : EnemyBehaviour
{
    [Header("General")]
    [SerializeField] private float attackDistance;
    [SerializeField] private float wakeUpTime;
    [SerializeField] private LayerMask roomMask;

    [Header("Animation")]
    [SerializeField] private MimicAnimationEvents mimicEvents;
    [SerializeField] private ParticleSystem groundSlamVFX;

    [Header("Components")]
    [SerializeField] private RoomBehaviour currentRoom;
    [SerializeField] private MimicObject mimicObject;
    [SerializeField] private EnemyHealth mimicHealth;
    [SerializeField] private EnemyMimicAttack mimicAttack;
    [SerializeField] private Animator animator;

    private Method state;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        agent.enabled = false;
        mimicHealth.enabled = false;

        mimicEvents.OnGroundTouch += Mimic_OnGroundTouch;
        mimicObject.OnWakeUp += Mimic_OnWakeUp;
        mimicHealth.OnDeath += Mimic_OnDeath;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        mimicEvents.OnGroundTouch -= Mimic_OnGroundTouch;
        mimicObject.OnWakeUp -= Mimic_OnWakeUp;
        mimicHealth.OnDeath -= Mimic_OnDeath;
    }

    private void Mimic_OnDeath()
    {
        currentRoom.OpenRoom(true);
        PlayerUI.BlockMap = false;
    }

    private void Mimic_OnGroundTouch()
    {
        groundSlamVFX.Play();
    }

    private void Mimic_OnWakeUp()
    {
        currentRoom.OpenRoom(false);
        PlayerUI.BlockMap = true;

        animator.SetTrigger("Wake Up");
        Invoke(nameof(WakeUpEnd), wakeUpTime);
    }

    protected override void Update()
    {
        base.Update();

        if (state == null)
            return;

        Tick(state);
    }

    private void WakeUpEnd()
    {
        agent.enabled = true;
        mimicHealth.enabled = true;

        SwitchState(Move, ref state);
    }

    private bool Move()
    {
        if (DoOnce())
        {
            mimicAttack.Attack();
        }

        if (IsNear(transform, mainTarget, attackDistance))
            PredictMoveTowardsTarget();
        else
            MoveTowardsTarget();

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
