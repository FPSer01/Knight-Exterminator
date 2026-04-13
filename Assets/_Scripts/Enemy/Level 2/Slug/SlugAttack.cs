using System;
using System.Collections;
using UnityEngine;

public class SlugAttack : EnemyAttack_Old
{
    [Header("Projectile")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private Vector3 attackTargetOffset;

    [Header("Projectile")]
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private Transform slimeSpawnPoint;
    [SerializeField] private float spawnCheckDistance;
    [SerializeField] private LayerMask spawnCheckLayer;
    [SerializeField] private float distanceBetweenSlime;
    private Transform lastSlime;

    [Space(20f)]
    [SerializeField] private EnemySFXController sfxController;

    private void Start()
    {
        StartCoroutine(CheckSlime());
    }

    public override void Attack()
    {
        sfxController.PlayAttackSFX();
    }

    private IEnumerator CheckSlime()
    {
        while (true)
        {
            if (lastSlime == null)
                SpawnSlime();
            else if (Vector3.Distance(lastSlime.position, slimeSpawnPoint.position) >= distanceBetweenSlime)
                SpawnSlime();

            yield return new WaitForFixedUpdate();
        }
    }

    private void SpawnSlime()
    {
        if (Physics.Raycast(slimeSpawnPoint.position, Vector3.down, out RaycastHit hit, spawnCheckDistance, spawnCheckLayer))
        {
            var slimeObject = Instantiate(slimePrefab, hit.point, Quaternion.LookRotation(hit.normal));
            lastSlime = slimeObject.transform;
        }
    }

    public void DoRangedAttack(Transform target)
    {
        Rigidbody targetRB = target.GetComponent<Rigidbody>();

        Vector3 targetPosition = CalculatePredictedPosition(targetRB, projectileSpeed) + attackTargetOffset;
        Vector3 shootDirection = (targetPosition - projectileSpawnPoint.position).normalized;

        GameObject projectileObj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(shootDirection));
        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();

        projectile.SetupProjectile(attackDamage);
        projectile.SetSpeed(projectileSpeed, projectileObj.transform.forward);
        projectile.OnHit += Projectile_OnHit;
    }

    private void Projectile_OnHit(PlayerHealth player, AttackDamageType type, HitTransform hitPos)
    {
        player.TakeDamage(attackDamage, GetComponent<EnemyHealth>());
        player.CreateHitEffect(hitPos);
    }

    private Vector3 CalculatePredictedPosition(Rigidbody target, float projectileSpeed)
    {
        Vector3 targetDirection = target.position - projectileSpawnPoint.position;

        float timeToTarget = targetDirection.magnitude / projectileSpeed;

        Vector3 predictedPos = target.position + target.linearVelocity * timeToTarget;

        return predictedPos;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(slimeSpawnPoint.position, Vector3.down * spawnCheckDistance);
    }
}
