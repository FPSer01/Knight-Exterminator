using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttackRange : PlayerAttackBase
{
    [Header("Range: Objects")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Range: Settings")]
    [SerializeField] private float projectileSpeed;
    [SerializeField] private float projectileAimSpeed;
    [Space]
    [SerializeField] private float timeToEndCombo;

    [Header("Range: Aim Settings")]
    [SerializeField] private float angleToSearchTargets;
    [SerializeField] private float distanceToSearchTargets;
    [SerializeField] private LayerMask targetsSearchLayer;

    [Header("Range: SFX")]
    [SerializeField] private string customAttackSFX;

    private PlayerTargetLock targetLock => playerComponents.TargetLockSystem;

    private int currentAttackIndex;
    private Coroutine comboTimerCoroutine;

    public string CustomAttackSFX { get => customAttackSFX; set => customAttackSFX = value; }
    public GameObject ProjectilePrefab { get => projectilePrefab; set => projectilePrefab = value; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        IsInitialized = true;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    public override void BlockAttack(bool block)
    {
        base.BlockAttack(block);

        if (blockAttack == true && block == false)
        {
            if (comboTimerCoroutine != null)
                StopCoroutine(comboTimerCoroutine);

            SetCurrentAttackIndex(0);
        }

        blockAttack = block;
    }

    private void SetCurrentAttackIndex(int index)
    {
        currentAttackIndex = index;
        animator.SetInteger("Attack Index", currentAttackIndex);
    }

    private IEnumerator EndComboTimer()
    {
        yield return new WaitForSeconds(timeToEndCombo);

        SetCurrentAttackIndex(0);
    }

    /// <summary>
    /// Вызов дальней атаки анимацией (через NetworkAnimator выполняется у всех клиентов)
    /// </summary>
    /// <param name="attackIndex"></param>
    public void RangeAttack(int attackIndex)
    {
        SetCurrentAttackIndex(attackIndex);

        if (currentAttackIndex > 0)
        {
            if (comboTimerCoroutine != null)
                StopCoroutine(comboTimerCoroutine);

            comboTimerCoroutine = StartCoroutine(EndComboTimer());
        }

        sfxController.PlayAttackSFX(false);
        sfxController.PlayCustomSFX(customAttackSFX, false);
        playerStamina.ConsumeStamina(playerStamina.AttackConsumage);

        if (IsOwner)
            ExecuteRangeAttack();

        if (playerStamina.CurrentStamina < playerStamina.AttackConsumage && attackInput)
        {
            SetAttackInput(false);
            SetCurrentAttackIndex(0);
        }
    }

    /// <summary>
    /// Выполнение атаки
    /// </summary>
    private void ExecuteRangeAttack()
    {
        Transform target = GetCurrentTarget();

        // Спавн реального снаряда
        PlayerProjectile projectile = SpawnProjectile();
        Vector3 projectileStartDirection = spawnPoint.forward;

        if (target != null)
        {
            projectileStartDirection = (target.position - spawnPoint.position).normalized;
        }

        projectile.SetupProjectile(projectileSpeed, projectileStartDirection, projectileAimSpeed, target);
        projectile.OnHit += Projectile_OnHit;

        // Спавн визуала (симуляцию снаряда) у всех остальных
        NetworkObjectReference targetRef = default;

        if (target != null)
        {
            if (target.TryGetComponent(out NetworkObject targetNetObj))
            {
                targetRef = new(targetNetObj);
            }
        }

        SpawnDummyProjectile_ServerRpc(projectileSpeed, projectileStartDirection, projectileAimSpeed, targetRef);
    }

    private void Projectile_OnHit(EntityHealth enemy, HitTransform hitPos)
    {
        var damage = attackDamage;

#if UNITY_EDITOR || DEBUG
        if (infiniteDamageCheatEnabled)
        {
            damage = attackDamage.GetMultDamage(99999);
            damage.IgnoreDefence = true;
        }
#endif

        float enemyDamagetaken = enemy.TakeDamage(damage, playerHealth);
        enemy.CreateHitEffect(hitPos);

        TryVampireHeal(enemyDamagetaken);
    }

    /// <summary>
    /// Получить самую актуальную цель
    /// </summary>
    /// <returns></returns>
    public Transform GetCurrentTarget()
    {
        if (targetLock.LockedOn)
        {
            return targetLock.Target;
        }
        else
        {
            return GetAutoSearchTarget();
        }
    }

    /// <summary>
    /// Попытка автоматически найти ближайшую цель в поле зрения перед игроком
    /// </summary>
    /// <returns></returns>
    private Transform GetAutoSearchTarget()
    {
        Transform target = null;
        Collider[] foundResults = new Collider[64];

        if (Physics.OverlapSphereNonAlloc(transform.position, distanceToSearchTargets, foundResults, targetsSearchLayer) == 0)
        {
            return null;
        }

        float minFoundDistance = float.MaxValue;
        foreach (var potentialTarget in foundResults)
        {
            if (potentialTarget == null)
                continue;

            if (!potentialTarget.TryGetComponent(out EntityHealth targetHealth))
                continue;

            Vector3 targetFlat = new(potentialTarget.transform.position.x, transform.position.y, potentialTarget.transform.position.z);
            float angle = Mathf.Abs(Vector3.Angle(transform.forward, targetFlat - transform.position));

            if (angle > angleToSearchTargets)
                continue;

            Transform foundTargetTransform = targetHealth.transform;
            float distance = Vector3.Distance(foundTargetTransform.position, transform.position);

            if (minFoundDistance > distance)
            {
                target = foundTargetTransform;
                minFoundDistance = distance;
            }
        }

        return target;
    }

    private PlayerProjectile SpawnProjectile()
    {
        var projectileObject = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
        PlayerProjectile projectile = projectileObject.GetComponent<PlayerProjectile>();

        return projectile;
    }

    [Rpc(SendTo.Server)]
    private void SpawnDummyProjectile_ServerRpc(float speed, Vector3 startDirection, float aimSpeed, NetworkObjectReference targetRef)
    {
        SpawnDummyProjectile_NotOwnerRpc(speed, startDirection, aimSpeed, targetRef);
    }

    [Rpc(SendTo.NotOwner)]
    private void SpawnDummyProjectile_NotOwnerRpc(float speed, Vector3 startDirection, float aimSpeed, NetworkObjectReference targetRef)
    {
        Transform target = null;

        if (targetRef.TryGet(out NetworkObject netObj))
            target = netObj.transform;

        PlayerProjectile projectile = SpawnProjectile();
        projectile.SetupProjectile(speed, startDirection, aimSpeed, target);
    }

    public void SetProjectilePrefab(GameObject newPrefab)
    {
        projectilePrefab = newPrefab;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanceToSearchTargets);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, angleToSearchTargets / 2, 0) * transform.forward * distanceToSearchTargets + transform.position);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, -angleToSearchTargets / 2, 0) * transform.forward * distanceToSearchTargets + transform.position);
    }
}
