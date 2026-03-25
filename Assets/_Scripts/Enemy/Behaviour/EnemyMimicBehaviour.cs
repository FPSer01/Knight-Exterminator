using System.Collections;
using UnityEngine;
using static PamukAI.PAI;

public class EnemyMimicBehaviour : BaseEnemyBehaviour
{
    [Header("Mimic Behaviour: General")]
    [SerializeField] private float attackDistance;
    [SerializeField] private float wakeUpTime;

    [Header("Animation")]
    [SerializeField] private ParticleSystem groundSlamVFX;

    [Header("Components")]
    [SerializeField] private MimicObject mimicObject;
    [SerializeField] private MimicAnimationEvents animEvents;
    [SerializeField] private RoomBehaviour boundRoom;
    [SerializeField] private EnemyComponents components;

    private EnemyHealth mimicHealth => components.Health as EnemyHealth;
    private Animator animator => components.Animator;
    private EnemyMimicAttack mimicAttack => components.Attack as EnemyMimicAttack;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            agent.enabled = true;
            mimicObject.OnWakeUp += Mimic_OnWakeUp;
            mimicHealth.OnDeath += Mimic_OnDeath;
        }

        mimicHealth.SetIgnoreDamage(true);
        mimicHealth.SetCanGetLockPoint(false);
        animEvents.OnGroundTouch += () => groundSlamVFX.Play();
    }

    public override void OnNetworkDespawn()
    {
        mimicObject.OnWakeUp -= Mimic_OnWakeUp;
        mimicHealth.OnDeath -= Mimic_OnDeath;

        base.OnNetworkDespawn();
    }

    private void Mimic_OnWakeUp()
    {
        if (boundRoom != null)
            boundRoom.StartBattle(false);

        animator.SetTrigger("Wake Up");
        Invoke(nameof(WakeUpEnd), wakeUpTime);
    }

    private void Mimic_OnDeath()
    {
        if (boundRoom != null)
            boundRoom.EndBattle();
    }

    private void WakeUpEnd()
    {
        agent.enabled = true;
        mimicHealth.SetIgnoreDamage(false);
        mimicHealth.SetCanGetLockPoint(true);

        SwitchState(Move, ref state);
    }

    private bool Move()
    {
        if (DoOnce())
        {
            mimicAttack.Attack();
        }

        if (IsNearCurrentTarget(attackDistance))
        {
            PredictMoveTowardsTarget();
        }
        else
        {
            MoveTowardsTarget();
        }

        return true;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
