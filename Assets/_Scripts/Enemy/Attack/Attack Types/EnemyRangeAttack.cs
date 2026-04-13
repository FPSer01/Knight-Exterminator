using UnityEngine;

public class EnemyRangeAttack : BaseEnemyAttack
{
    [Header("Range Settings")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private Vector3 targetOffset;

    [Header("SFX Settings")]
    [SerializeField] private bool useAttackSFX = true;
    [SerializeField] private string customSFXTag;
    [Range(0f, 1f)][SerializeField] private float attackSFXVolume = 1;

    [Header("Components")]
    [SerializeField] private EnemyComponents components;

    private EnemySFXController sfxController => components.SFXController;

    public Vector3 TargetOffset { get => targetOffset; set => targetOffset = value; }
    public float ProjectileSpeed { get => projectileSpeed; set => projectileSpeed = value; }

    private void PlayAttackSFX()
    {
        if (useAttackSFX)
        {
            sfxController.PlayAttackSFX(attackSFXVolume);
        }
        else if (!useAttackSFX)
        {
            sfxController.PlayCustomSFX(customSFXTag);
        }
    }

    public override void RangeAttack(Vector3 target)
    {
        if (!canAttack)
            return;

        PlayAttackSFX();

        Vector3 targetPosition = target + targetOffset;
        Vector3 shootDirection = (targetPosition - projectileSpawnPoint.position).normalized;

        GameObject projectileObj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(shootDirection));
        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();

        projectile.SetupProjectile(attackDamage);
        projectile.SetSpeed(projectileSpeed, projectileObj.transform.forward);
        projectile.OnHit += Projectile_OnHit;

        if (IsServer)
            projectile.NetworkObject.Spawn();

        base.RangeAttack(target);
    }

    private void Projectile_OnHit(PlayerHealth player, AttackDamageType type, HitTransform transform)
    {
        player.TakeDamage(attackDamage, GetComponent<EnemyHealth>());
        player.CreateHitEffect(transform);
    }
}
