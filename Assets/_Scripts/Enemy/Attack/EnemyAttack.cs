using Unity.Netcode;
using UnityEngine;

public abstract class EnemyAttack : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] protected AttackDamageType attackDamage;
    [SerializeField] protected float attackCooldown;
    protected float currentAttackCooldown;

    public AttackDamageType AttackDamage { get => attackDamage; set => attackDamage = value; }
    public float AttackCooldown { get => attackCooldown; set => attackCooldown = value; }
    public float CurrentAttackCooldown { get => currentAttackCooldown; set => currentAttackCooldown = value; }

    public abstract void Attack();
}
