using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentipedeBossAttack : MonoBehaviour
{
    [Header("Bite")]
    [SerializeField] private EnemyMeleeAttackCollider biteCollider;
    [SerializeField] private AttackDamageType biteDamage;

    [Header("Many Strikes")]
    [SerializeField] private List<EnemyMeleeAttackCollider> manyStrikesColliders;
    [SerializeField] private AttackDamageType manyStrikesDamage;
    [SerializeField] private AudioClip manyStrikesSFX;

    [Header("Whip")]
    [SerializeField] private EnemyMeleeAttackCollider rightWhipCollider;
    [SerializeField] private EnemyMeleeAttackCollider leftWhipCollider;
    [SerializeField] private AttackDamageType whipDamage;
    [SerializeField] private AudioClip whipSFX;

    [Header("Slime")]
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private Transform slimeSpawnPoint;
    [SerializeField] private float spawnCheckDistance;
    [SerializeField] private float distanceBetweenSlime;
    [SerializeField] private LayerMask spawnCheckLayer;
    private Transform lastSlime;
    private IEnumerator slimeCoroutine;

    [Header("Poison Clouds")]
    [SerializeField] private GameObject poisonCloudPrefab;
    [SerializeField] private Transform poisonCloudSpawnPoint;
    [SerializeField] private float timeBetweenClouds;
    [SerializeField] private float cloudSpawnDistanceUp;
    private IEnumerator poisonCloudCoroutine;

    [Header("Cooldowns")]
    [SerializeField] private float meleeCooldown;
    [SerializeField] private float whipCooldown;
    [SerializeField] private float circleAttackCooldown;
    
    [Header("Components")]
    [SerializeField] private EnemySFXController sfxController;

    public float MeleeCooldown { get => meleeCooldown; set => meleeCooldown = value; }
    public float WhipCooldown { get => whipCooldown; set => whipCooldown = value; }
    public float CircleAttackCooldown { get => circleAttackCooldown; set => circleAttackCooldown = value; }

    private void Start()
    {
        biteCollider.OnHit += BiteCollider_OnHit;
        manyStrikesColliders.ForEach(c => c.OnHit += ManyStrikesCollider_OnHit);

        rightWhipCollider.OnHit += WhipCollider_OnHit;
        leftWhipCollider.OnHit += WhipCollider_OnHit;
    }

    #region Bite

    public void Bite()
    {
        sfxController.PlayAttackSFX();
        biteCollider.StartAttackCheck();
    }

    private void BiteCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(biteDamage, GetComponent<BossHealth>());
        player.CreateHitEffect(hitPos);
    }

    #endregion

    #region Many Strikes
    public void ManyStrikes(float duration)
    {
        sfxController.PlayOneShot(manyStrikesSFX);
        StartCoroutine(DoManyStrikes(duration));
    }

    private IEnumerator DoManyStrikes(float duration)
    {
        float oneStrike = duration / manyStrikesColliders.Count;

        Debug.Log(oneStrike);

        foreach (var col in manyStrikesColliders)
        {
            col.StartAttackCheck();

            yield return new WaitForSeconds(oneStrike);
        }
    }

    private void ManyStrikesCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(manyStrikesDamage.GetMultDamage(1f / manyStrikesColliders.Count, true), GetComponent<BossHealth>());
        player.CreateHitEffect(hitPos);
    }

    #endregion

    #region Whip
    public void WhipAttack(bool right)
    {
        if (right)
        {
            rightWhipCollider.StartAttackCheck();
        }
        else
        {
            leftWhipCollider.StartAttackCheck();
        }

        sfxController.PlayOneShot(whipSFX);
    }

    private void WhipCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(whipDamage, GetComponent<BossHealth>());
        player.CreateHitEffect(hitPos);
    }

    #endregion

    #region Slime (Circle Attack)

    public void SetSpawnSlime(bool active)
    {
        if (active)
        {
            sfxController.PlayAttackSFX();
            slimeCoroutine = CheckSlime();
            StartCoroutine(slimeCoroutine);
        }
        else
        {
            if (slimeCoroutine != null)
            {
                StopCoroutine(slimeCoroutine);
                slimeCoroutine = null;
                lastSlime = null;
            }
        }
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

    #endregion

    #region Poison Clouds

    public void ActivatePoisonClouds(bool activate)
    {
        if (activate)
        {
            poisonCloudCoroutine = SpawnPoisonCloud();
            StartCoroutine(poisonCloudCoroutine);
        }
        else
        {
            if (poisonCloudCoroutine != null)
            {
                StopCoroutine(poisonCloudCoroutine);
                poisonCloudCoroutine = null;
            }
        }
    }

    private IEnumerator SpawnPoisonCloud()
    {
        while (true)
        {
            GameObject poisonCloudObject = Instantiate(poisonCloudPrefab, poisonCloudSpawnPoint.position, Quaternion.identity);
            Rigidbody poisonCloudRB = poisonCloudObject.GetComponent<Rigidbody>();

            Vector3 endPos = poisonCloudRB.position + new Vector3(0, cloudSpawnDistanceUp, 0);
            poisonCloudRB.DOMove(endPos, 1f);

            yield return new WaitForSeconds(timeBetweenClouds);
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(slimeSpawnPoint.position, Vector3.down * spawnCheckDistance);
    }
}
