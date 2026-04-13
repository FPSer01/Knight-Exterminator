using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerProjectile : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    [SerializeField] private float lifeTime = 10f;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [Range(0f, 1f)] [SerializeField] private float onHitSFXVolume = 1f;
    [SerializeField] private List<AudioClip> onHitSFX;

    [Header("VFX")]
    [SerializeField] private GameObject hitEffect;

    public event Action<EntityHealth, HitTransform> OnHit;

    private Transform target;
    private float speed;
    private float aimSpeed;
    private bool isHoming;

    private void Start()
    {
        col.isTrigger = true;

        DestroyProjectile(lifeTime);
    }

    public void SetupProjectile(float speed, Vector3 startDirection, float aimSpeed, Transform target)
    {
        this.speed = speed;
        this.aimSpeed = aimSpeed;
        this.target = target;
        isHoming = target != null && aimSpeed > 0f;

        rb.linearVelocity = startDirection.normalized * speed;
    }

    private void FixedUpdate()
    {
        if (isHoming && target != null)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            Vector3 currentDirection = rb.linearVelocity.normalized;

            // Плавно поворачиваем направление к цели
            Vector3 newDirection = Vector3.RotateTowards(
                currentDirection,
                directionToTarget,
                aimSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime,
                0f
            );

            rb.linearVelocity = newDirection * speed;
        }

        // Поворачиваем forward по направлению движения
        if (rb.linearVelocity.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out EntityHealth hitTarget))
        {
            Vector3 hitPos = other.ClosestPoint(hitTarget.transform.position);
            OnHit?.Invoke(hitTarget, new HitTransform(hitPos, transform.rotation));
        }

        DestroyProjectile();
    }

    private void DestroyProjectile(float delay = 0f)
    {
        StartCoroutine(DestroyProjectileSequence(delay));
    }

    private IEnumerator DestroyProjectileSequence(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);

        PlayOnHitSFX();
        Destroy(gameObject);
    }

    private void PlayOnHitSFX()
    {
        GameObject sfxObjectPrefab = sfxSource.gameObject;
        GameObject sfxObject = Instantiate(sfxObjectPrefab, transform.position, Quaternion.identity);

        AudioSource source = sfxObject.GetComponent<AudioSource>();

        int index = Random.Range(0, onHitSFX.Count);
        AudioClip clip = onHitSFX[index];

        source.PlayOneShot(clip, onHitSFXVolume);

        Destroy(sfxObject, clip.length);
    }
}
