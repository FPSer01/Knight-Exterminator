using UnityEngine;
using static PamukAI.PAI;

public class TickBehaviour : EnemyBehaviour
{
    [Header("General")]
    [SerializeField] private float seeDistance;
    [SerializeField] private float getAwayDistance;
    [SerializeField] private float attackDistance;
    [SerializeField] private float fleeDistance;

    [Header("Components")]
    [SerializeField] private EnemySFXController sfxController;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private TickAttack enemyAttack;
    [SerializeField] private Animator animator;

    private Method state;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SwitchState(Idle, ref state);
    }

    protected override void Update()
    {
        base.Update();
        Tick(state);
    }

    private bool Idle()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
        }

        // Если видит цель - преследует ее
        if (IsNear(transform, mainTarget, seeDistance))
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

        if (IsNear(transform, mainTarget, attackDistance) && enemyAttack.CurrentAttackCooldown <= 0)
        {
            SwitchState(JumpAttack, ref state);
        }

        if (enemyAttack.CurrentAttackCooldown > 0)
            enemyAttack.CurrentAttackCooldown -= Time.deltaTime;

        if (IsNear(transform, mainTarget, getAwayDistance))
            agent.SetDestination(transform.position + (transform.position - mainTarget.position).normalized * fleeDistance);
        else
            agent.SetDestination(mainTarget.position);

        return true;
    }

    private bool JumpAttack()
    {
        if (DoOnce())
        {
            sfxController.PlayMoveSFX(false);
            animator.SetBool("Move", false);
            animator.SetTrigger("Attack");

            agent.enabled = false;
            enemyAttack.SetJumpTarget(mainTarget);
            enemyAttack.Attack();
        }

        if (Wait(enemyAttack.JumpTime + 0.05f))
            return true;

        if (Step())
        {
            agent.enabled = true;
            SwitchState(Move, ref state);
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, seeDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, getAwayDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
