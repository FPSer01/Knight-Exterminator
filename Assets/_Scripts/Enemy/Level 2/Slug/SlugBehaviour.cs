using UnityEngine;
using static PamukAI.PAI;

public class SlugBehaviour : EnemyBehaviour
{
    [Header("General")]
    [SerializeField] private float seeDistance;
    [Space]
    [SerializeField] private float attackDistance;
    [Space]
    [SerializeField] private float delayBeforeAttack;
    [SerializeField] private float delayAfterAttack;

    [Header("Components")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private SlugAttack enemyAttack;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemySFXController sfxController;

    private float currentAttackCooldown;

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

        if (currentAttackCooldown > 0)
            currentAttackCooldown -= Time.deltaTime;

        if (currentAttackCooldown <= 0)
        {
            SwitchState(Attack, ref state);
        }

        MoveTowardsTarget();

        return true;
    }

    private bool Attack()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            animator.SetTrigger("Attack");
            sfxController.PlayMoveSFX(false);

            agent.enabled = false;
        }

        if (Wait(delayBeforeAttack))
        {
            RotateTowardsTarget(10f);
            return true;
        }

        if (DoOnce())
        {
            enemyAttack.DoRangedAttack(mainTarget);
            currentAttackCooldown = enemyAttack.AttackCooldown;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, seeDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
