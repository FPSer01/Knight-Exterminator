using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseEnemyAttack : NetworkBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected AttackDamageType attackDamage;
    [SerializeField] protected float attackCooldown;

    protected bool canAttack = true;

    public AttackDamageType AttackDamage { get => attackDamage; set => attackDamage = value; }
    public float AttackCooldown { get => attackCooldown; set => attackCooldown = value; }
    public bool CanAttack { get => canAttack; }

    public virtual void Attack()
    {
        StartCoroutine(StartAttackCooldown());
    }

    public virtual void RangeAttack(Vector3 target)
    {
        StartCoroutine(StartAttackCooldown());
    }

    public virtual void SummonAttack(int summonAmount)
    {
        StartCoroutine(StartAttackCooldown());
    }

    protected IEnumerator StartAttackCooldown()
    {
        canAttack = false;

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }
}
