using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class CentipedeHole : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private SplineContainer jumpOutSpline;
    [SerializeField] private ParticleSystem beforeJumpOutVFX;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private List<AudioClip> interactionSFX;
    private float originalVolume;

    [Header("Damage")]
    [SerializeField] private EnemyMeleeAttackCollider attackCollider;
    [SerializeField] private AttackDamageType damage;

    [Header("Checks")]
    [SerializeField] private Transform checkPos;
    [SerializeField] private float checkDistance;
    [SerializeField] private LayerMask checkMask;

    private void Start()
    {
        attackCollider.OnHit += AttackCollider_OnHit;
        originalVolume = sfxSource.volume;
    }

    private void AttackCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(damage, null);
    }

    public void PlaySFX()
    {
        var index = Random.Range(0, interactionSFX.Count);
        var clip = interactionSFX[index];
        sfxSource.clip = clip;

        sfxSource.DOFade(originalVolume, 0.25f);
        sfxSource.Play();
    }

    public void StopSFX()
    {
        sfxSource.DOFade(0, 0.25f).OnComplete(() => sfxSource.Stop());
    }

    public void PlayBeforeJumpOutVFX(bool play)
    {
        if (play)
            beforeJumpOutVFX.Play();
        else
            beforeJumpOutVFX.Stop();
    }

    public void DoExplosion()
    {
        attackCollider.StartAttackCheck();
    }

    public void TurnSplineTowardsTransform(Transform target)
    {
        if (target == null)
            return;

        Vector3 direction = Vector3.zero;

        if (Physics.Raycast(checkPos.position, checkPos.forward, checkDistance, checkMask))
        {
            direction = new Vector3(
                jumpOutSpline.transform.position.x - target.position.x,
                jumpOutSpline.transform.position.y,
                jumpOutSpline.transform.position.z - target.position.z
                ).normalized;
        }
        else
        {
            direction = new Vector3(
                target.position.x - jumpOutSpline.transform.position.x,
                jumpOutSpline.transform.position.y,
                target.position.z - jumpOutSpline.transform.position.z
                ).normalized;
        }

        float angle = Vector3.Angle(Vector3.forward, direction);
        jumpOutSpline.transform.eulerAngles = new Vector3(0, angle, 0);
        //jumpOutSpline.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public SplineContainer GetSpline()
    {
        return jumpOutSpline;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(checkPos.position, checkPos.forward * checkDistance);
    }
}
