using UnityEngine;
using static PamukAI.PAI;

public class EnemyChaseBehaviour : BaseEnemyBehaviour
{
    [Header("Chase Behaviour: General")]
    [SerializeField] private float attackDistance;
    [SerializeField] private float attackAngle;
    [SerializeField] private float delayBeforeAttack;
    [SerializeField] private float delayAfterAttack;
    [SerializeField] private float rotateSpeed;

    [Header("Chase Behaviour: Behaviour Options")]
    [SerializeField] private bool rotateToTargetBeforeAttack;
    [Space]
    [SerializeField] private bool predictMove;
    [SerializeField] private float predictMoveMult;
    [Space]
    [SerializeField] private bool predictBeforeAttack;
    [SerializeField] private float predictBeforeAttackMult;

    [Header("Components")]
    [SerializeField] private EnemyComponents enemyComponents;

    private EnemyMeleeAttack enemyAttack => enemyComponents.Attack as EnemyMeleeAttack;
    private Animator animator => enemyComponents.Animator;
    private EnemySFXController sfxController => enemyComponents.SFXController;

    protected override void OnSpawned()
    {
        base.OnSpawned();

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
            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);
        }

        if (IsNearCurrentTarget(attackDistance) && enemyAttack.CanAttack)
        {
            if (attackAngle >= GetHorizontalAngleToTarget()) 
            {
                SwitchState(Attack, ref state);
            }

            if (predictBeforeAttack)
            {
                PredictMoveTowardsTarget(predictBeforeAttackMult);
                return true;
            }
        }

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

    private bool Attack()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Attack");

            agent.enabled = false;
        }

        if (Wait(delayBeforeAttack))
        {
            if (rotateToTargetBeforeAttack)
                RotateBeforeAttack();

            return true;
        }

        if (DoOnce())
        {
            enemyAttack.Attack();
        }

        if (Wait(enemyAttack.TotalAttackTime))
        {
            return true;
        }

        if (Wait(delayAfterAttack))
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

    private void RotateBeforeAttack()
    {
        if (predictBeforeAttack)
        {
            PredictRotateTowardsTarget(rotateSpeed, predictBeforeAttackMult);
        }
        else
        {
            RotateTowardsTarget(rotateSpeed);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, attackAngle / 2, 0) * transform.forward * attackDistance + transform.position);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, -attackAngle / 2, 0) * transform.forward * attackDistance + transform.position);
    }
}
