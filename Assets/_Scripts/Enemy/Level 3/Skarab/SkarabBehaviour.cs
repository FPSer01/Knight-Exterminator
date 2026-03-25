using UnityEngine;
using static PamukAI.PAI;

public class SkarabBehaviour : EnemyBehaviour
{
    [Header("General")]
    [SerializeField] private float seeDistance;
    [Space]
    [SerializeField] private float attackDistance;
    [SerializeField] private float attackAngle;
    [Space]
    [SerializeField] private float delayBeforeAttack;
    [SerializeField] private float delayAfterAttack;

    [Header("Components")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private SkarabAttack enemyAttack;
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

        if (CheckForStrike() && currentAttackCooldown <= 0)
        {
            SwitchState(Attack, ref state);
        }

        if (IsNear(transform, mainTarget, attackDistance))
            PredictMoveTowardsTarget();
        else
            MoveTowardsTarget();

        return true;
    }

    private bool Attack()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
        }

        if (Wait(delayBeforeAttack))
            return true;

        if (DoOnce())
        {
            agent.enabled = false;
            animator.SetTrigger("Attack");
            enemyAttack.Attack();
            currentAttackCooldown = enemyAttack.AttackCooldown;
        }

        if (Wait(delayAfterAttack))
            return true;

        if (Step())
        {
            agent.enabled = true;
            SwitchState(Move, ref state);
        }

        return true;
    }

    private bool CheckForStrike()
    {
        Vector3 targetPlanePosition = new(mainTarget.position.x, transform.position.y, mainTarget.position.z);

        float distance = Vector3.Distance(targetPlanePosition, transform.position);
        float angle = Vector3.Angle(transform.forward, targetPlanePosition - transform.position);

        if (distance <= attackDistance && Mathf.Abs(angle) <= attackAngle)
        {
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, seeDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        Gizmos.DrawWireSphere(transform.position, attackDistance);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, attackAngle / 2, 0) * transform.forward * attackDistance + transform.position);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, -attackAngle / 2, 0) * transform.forward * attackDistance + transform.position);
    }
}
