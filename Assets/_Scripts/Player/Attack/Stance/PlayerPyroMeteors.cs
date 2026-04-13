using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerPyroMeteors : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private ParticleSystem meteorsVFX;
    [SerializeField] private ParticleCollisionRelay collisionRelay;

    [Header("Settings")]
    [SerializeField] private float explosionRadius;
    [SerializeField] private LayerMask explosionMask;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private List<AudioClip> hitSFX;
    [Range(0f, 1f)] [SerializeField] private float hitSFXVolume = 1f;

    public event Action<EntityHealth, HitTransform> OnHit;

    private void Start()
    {
        collisionRelay.OnCollision += CollisionRelay_OnCollision;
    }

    /// <summary>
    /// Начать запуск анимации метеоров с проверкой хитов, после окончания которого объект удаляется
    /// </summary>
    /// <param name="time"></param>
    public void SetMeteorsTime(float time)
    {
        StartCoroutine(ExecutePlay(time));
    }

    private IEnumerator ExecutePlay(float time)
    {
        meteorsVFX.Play();

        yield return new WaitForSeconds(time);

        meteorsVFX.Stop();

        yield return new WaitForSeconds(1.5f);

        Destroy(gameObject);
    }

    private void CollisionRelay_OnCollision(GameObject other, List<ParticleCollisionEvent> events, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 hitPos = events[i].intersection;

            CheckForHit(hitPos);
            PlayOnHitSFX(hitPos);

            //Debug.Log($"Попали в {other.name} в точке {events[i].intersection}");
        }
    }

    private void CheckForHit(Vector3 position)
    {
        Collider[] colliders = new Collider[8];

        if (Physics.OverlapSphereNonAlloc(position, explosionRadius, colliders, explosionMask) == 0) 
        {
            return;
        }

        foreach (var collider in colliders)
        {
            if (collider == null)
                return;

            if (collider.TryGetComponent(out EntityHealth hitTarget))
            {
                Vector3 hitPos = collider.ClosestPoint(hitTarget.transform.position);
                OnHit?.Invoke(hitTarget, new HitTransform(hitPos, transform.rotation));
            }
        }
    }

    private void PlayOnHitSFX(Vector3 position)
    {
        GameObject sfxObjectPrefab = sfxSource.gameObject;
        GameObject sfxObject = Instantiate(sfxObjectPrefab, position, Quaternion.identity);

        AudioSource source = sfxObject.GetComponent<AudioSource>();

        int index = Random.Range(0, hitSFX.Count);
        AudioClip clip = hitSFX[index];

        source.PlayOneShot(clip, hitSFXVolume);

        Destroy(sfxObject, clip.length);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
