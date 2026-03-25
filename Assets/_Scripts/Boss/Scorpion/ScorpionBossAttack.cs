using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorpionBossAttack : MonoBehaviour
{
    [Header("Claws Attack")]
    [SerializeField] private AttackDamageType clawsDamage;
    [SerializeField] private List<EnemyMeleeAttackCollider> clawsColliders;
    [SerializeField] private float clawsBetweenTime;

    [Header("Tail Attack")]
    [SerializeField] private AttackDamageType tailDamage;
    [SerializeField] private EnemyMeleeAttackCollider tailCollider;
    [SerializeField] private List<AudioClip> tailSFX;

    [Header("Wind Attack")]
    [SerializeField] private AttackDamageType windMeleeDamage;
    [SerializeField] private EnemyMeleeAttackCollider windMeleeCollider;
    [SerializeField] private List<AudioClip> windMeleeSFX;
    [Space]
    [SerializeField] private AttackDamageType windProjectileDamage;
    [SerializeField] private GameObject windProjectilePrefab;
    [SerializeField] private List<Transform> windProjectileSpawnPoints;
    [SerializeField] private float windProjectileSpeed;

    [Header("Dig Out")]
    [SerializeField] private AttackDamageType digOutDamage;
    [SerializeField] private EnemyMeleeAttackCollider digOutCollider;
    [SerializeField] private List<AudioClip> digOutSFX;
    [SerializeField] private float digOutForce;
    [SerializeField] private float digOutRadius;

    [Header("Components")]
    [SerializeField] private EnemySFXController sfxController;

    private void Start()
    {
        clawsColliders.ForEach(c => c.OnHit += ClawsCollider_OnHit);
        tailCollider.OnHit += TailCollider_OnHit;
        windMeleeCollider.OnHit += WindMeleeCollider_OnHit;
        digOutCollider.OnHit += DigOutCollider_OnHit;
    }

    private void DigOutCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        DoDamage(digOutDamage, player, hitPos);

        if (player.TryGetComponent(out Rigidbody playerRB))
        {
            playerRB.AddExplosionForce(digOutForce, digOutCollider.transform.position, digOutRadius, 10f, ForceMode.VelocityChange);
        }
    }

    private void WindMeleeCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        DoDamage(windMeleeDamage, player, hitPos);
    }

    private void TailCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        DoDamage(tailDamage, player, hitPos);
    }

    private void ClawsCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        DoDamage(clawsDamage, player, hitPos);
    }

    private void DoDamage(AttackDamageType damage, PlayerHealth target, HitTransform hitPos)
    {
        target.TakeDamage(damage, GetComponent<BossHealth>());
        target.CreateHitEffect(hitPos);
    }

    public void ClawsAttack()
    {
        StartCoroutine(ClawsAttackSequence());
    }

    private IEnumerator ClawsAttackSequence()
    {
        foreach (var col in clawsColliders)
        {
            sfxController.PlayAttackSFX();
            col.StartAttackCheck();

            yield return new WaitForSeconds(clawsBetweenTime);
        }
    }

    public void TailAttack()
    {
        var clip = sfxController.GetRandomClip(tailSFX);
        sfxController.PlayOneShot(clip);
        tailCollider.StartAttackCheck();
    }

    public void WindAttack()
    {
        // Melee
        var clip = sfxController.GetRandomClip(windMeleeSFX);
        sfxController.PlayOneShot(clip);
        windMeleeCollider.StartAttackCheck();

        // Projectiles
        SpawnWindProjectiles();
    }

    private void SpawnWindProjectiles()
    {
        foreach (var point in windProjectileSpawnPoints)
        {
            GameObject projectileObj = Instantiate(windProjectilePrefab, point.position, point.rotation);
            EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();

            projectile.SetupProjectile(windProjectileDamage);
            projectile.SetSpeed(windProjectileSpeed, projectileObj.transform.forward);

            projectile.OnHit += Projectile_OnHit;
        }
    }

    private void Projectile_OnHit(PlayerHealth player, AttackDamageType damage, HitTransform hitPos)
    {
        DoDamage(damage, player, hitPos);
    }

    public void DigOutExplosion()
    {
        digOutCollider.StartAttackCheck();

        var clip = sfxController.GetRandomClip(digOutSFX);
        sfxController.PlayOneShot(clip, 0.66f);
    }
}
