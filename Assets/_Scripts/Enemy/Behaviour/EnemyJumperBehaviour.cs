using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static PamukAI.PAI;

public class EnemyJumperBehaviour : BaseEnemyBehaviour
{
    [Header("Jumper Behaviour: General")]
    [SerializeField] private float attackDistance;
    [Space]
    [SerializeField] private float attackAngle;
    [SerializeField] private float rotateSpeed;
    [SerializeField] private float delayBeforeAttack;
    [SerializeField] private float delayAfterAttack;

    [Header("Jumper Behaviour: Flee")]
    [SerializeField] private bool canFlee;
    [Space]
    [SerializeField] private float fleeTriggerDistance;
    [SerializeField] private float fleeDistance;
    [Space]
    [SerializeField] private float fleeMoveSpeed;
    [SerializeField] private float fleeTurnSpeed;
    [SerializeField] private float fleeMoveAcceleration;

    [Header("Jumper Behaviour: Behaviour Options")]
    [SerializeField] private bool predictMove;
    [SerializeField] private float predictMoveMult;
    [Space]
    [SerializeField] private bool predictBeforeAttack;
    [SerializeField] private float predictBeforeAttackMult;

    [Header("VFX")]
    [SerializeField] private ParticleSystem vfxBeforeAttack;

    [Header("Components")]
    [SerializeField] private EnemyComponents components;

    private EnemySFXController sfxController => components.SFXController;
    private EnemyJumperAttack enemyAttack => components.Attack as EnemyJumperAttack;
    private Animator animator => components.Animator;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        EnableVFXBeforeAttack(false);
        SwitchState(Idle, ref state);
    }

    private bool Idle()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
        }

        if (IsNearCurrentTarget(seeDistance))
        {
            SwitchState(Move, ref state);
        }

        return true;
    }

    private bool Move()
    {
        if (DoOnce())
        {
            ResetMoveSpeed();
            ResetMoveAcceleration();
            ResetTurnSpeed();

            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);
        }

        // Отступление
        if (canFlee && IsNearCurrentTarget(fleeTriggerDistance))
        {
            SwitchState(Flee, ref state);
        }

        // Атака
        if (predictBeforeAttack && IsNearCurrentTarget(attackDistance) && enemyAttack.CanAttack)
        {
            PredictMoveTowardsTarget(predictBeforeAttackMult);

            if (attackAngle >= GetHorizontalAngleToTarget())
            {
                SwitchState(JumpAttack, ref state);
            }

            return true;
        }
        else if (IsNearCurrentTarget(attackDistance) && enemyAttack.CanAttack)
        {
            SwitchState(StopToPrepareAttack, ref state);
        }

        // Преследование
        if (predictMove)
        {
            PredictMoveTowardsTarget(predictMoveMult);
        }
        else
        {
            MoveTowardsTarget();
        }

        return true;
    }

    private bool Flee()
    {
        if (DoOnce())
        {
            ChangeMoveSpeed(fleeMoveSpeed > 0 ? fleeMoveSpeed : originalMoveSpeed);
            ChangeMoveAcceleration(fleeMoveAcceleration > 0 ? fleeMoveAcceleration : originalMoveAcceleration);
            ChangeTurnSpeed(fleeTurnSpeed > 0 ? fleeTurnSpeed : originalTurnSpeed);

            MoveFromTarget(fleeDistance);
        }

        if (agent.remainingDistance <= 0.1f)
        {
            SwitchState(Move, ref state);
        }

        return true;
    }

    private bool StopToPrepareAttack()
    {
        if (DoOnce())
        {
            agent.enabled = false;
            sfxController.PlayMoveSFX(false);
        }

        if (IsNearCurrentTarget(attackDistance / 2))
            PredictRotateTowardsTarget(rotateSpeed, 2f);
        else
            RotateTowardsTarget(rotateSpeed);

        if (attackAngle >= GetHorizontalAngleToTarget())
        {
            SwitchState(JumpAttack, ref state);
        }

        return true;
    }

    private bool JumpAttack()
    {
        if (DoOnce())
        {
            sfxController.PlayMoveSFX(false);
            animator.SetBool("Move", false);
            animator.SetTrigger("Attack");

            EnableVFXBeforeAttack(true);
        }

        if (Wait(delayBeforeAttack))
        {
            RotateTowardsTarget(rotateSpeed);

            return true;
        }

        if (DoOnce())
        {
            agent.enabled = false;
            enemyAttack.Attack();
            EnableVFXBeforeAttack(false);
        }

        if (Wait(enemyAttack.CurrentJumpTime + delayAfterAttack))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;
            SwitchState(Move, ref state);
        }

        return true;
    }

    private void EnableVFXBeforeAttack(bool play)
    {
        ExecuteEnableVFXBeforeAttack(play);
        EnableVFXBeforeAttack_NotOwnerRpc(play);
    }

    private void ExecuteEnableVFXBeforeAttack(bool play)
    {
        if (vfxBeforeAttack == null)
            return;

        if (play)
        {
            vfxBeforeAttack.Play();
        }
        else
        {
            vfxBeforeAttack.Stop();
        }
    }

    [Rpc(SendTo.NotOwner)]
    private void EnableVFXBeforeAttack_NotOwnerRpc(bool play)
    {
        ExecuteEnableVFXBeforeAttack(play);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fleeTriggerDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, attackAngle / 2, 0) * transform.forward * attackDistance + transform.position);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, -attackAngle / 2, 0) * transform.forward * attackDistance + transform.position);
    }
}
