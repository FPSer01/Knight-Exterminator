using DG.Tweening;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class AntAttack : EnemyAttack_Old
{
    [Header("Melee")]
    [SerializeField] private EnemyMeleeAttackCollider attackCollider;

    [Header("Fire Cloud")]
    [SerializeField] private GameObject fireCloudPrefab;
    [SerializeField] private Transform fireCloudSpawnPoint;
    [SerializeField] private float fireCloudCooldown;
    [SerializeField] private float cloudSpawnDistanceUp;
    private bool canSpawnFireCloud = true;

    [Header("Components")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemySFXController sfxController;

    private void Start()
    {
        attackCollider.OnHit += AttackCollider_OnHit;
        enemyHealth.OnDamageTaken += Enemy_OnDamageTaken;
    }

    private void Enemy_OnDamageTaken(float damage)
    {
        if (!canSpawnFireCloud)
            return;

        SpawnFireCloud();
        StartCoroutine(FireCloudCooldown());
    }

    private void AttackCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(attackDamage, GetComponent<EnemyHealth>());
        player.CreateHitEffect(hitPos);
    }

    public override void Attack()
    {
        sfxController.PlayAttackSFX();
        attackCollider.StartAttackCheck();
    }

    private void SpawnFireCloud()
    {
        GameObject poisonCloudObject = Instantiate(fireCloudPrefab, fireCloudSpawnPoint.position, Quaternion.identity);
        Rigidbody poisonCloudRB = poisonCloudObject.GetComponent<Rigidbody>();

        Vector3 endPos = poisonCloudRB.position + new Vector3(0, cloudSpawnDistanceUp, 0);
        poisonCloudRB.DOMove(endPos, 1f);
    }

    private IEnumerator FireCloudCooldown()
    {
        canSpawnFireCloud = false;

        yield return new WaitForSeconds(fireCloudCooldown);

        canSpawnFireCloud = true;
    }
}
