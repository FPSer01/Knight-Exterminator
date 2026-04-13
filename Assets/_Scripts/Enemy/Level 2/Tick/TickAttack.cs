using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class TickAttack : EnemyAttack_Old
{
    [Space]
    [SerializeField] private float healPerAttack;
    [SerializeField] private float jumpTime;
    [SerializeField] private SplineContainer jumpSpline;
    [SerializeField] private bool rotateBeforeJump = true;
    [SerializeField] private float rotateTime;
    [SerializeField] private bool useCooldown = true;
    [Space]
    [SerializeField] private EnemySFXController sfxController;
    [SerializeField] private EnemyTouchAttackCollider attackCollider;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private float groundCheckDistance = 1f;

    private bool attacking = false;
    private Vector3 targetDirection;

    public float JumpTime { get => rotateBeforeJump ? jumpTime + rotateTime : jumpTime; set => jumpTime = value; }

    private void Start()
    {
        attackCollider.OnHit += AttackCollider_OnHit;
    }

    private void AttackCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        float damage = player.TakeDamage(attackDamage, GetComponent<EnemyHealth>());
        player.CreateHitEffect(hitPos);

        enemyHealth.Heal(damage * healPerAttack);
    }

    public void SetJumpTarget(Transform target)
    {
        targetDirection = target.position - transform.position;
    }

    public override void Attack()
    {
        if (attacking)
            return;

        sfxController.PlayAttackSFX();
        StartCoroutine(InitiateJumpAttack());
    }

    private IEnumerator InitiateJumpAttack()
    {
        attacking = true;
        attackCollider.SetCollider(true);

        if (useCooldown)
            currentAttackCooldown = attackCooldown;

        if (rotateBeforeJump)
        {
            transform.DORotate(Quaternion.LookRotation(targetDirection, Vector3.up).eulerAngles, rotateTime);
            yield return new WaitForSeconds(rotateTime);
        }

        Spline spline = jumpSpline.Splines[0];
        var knots = (List<BezierKnot>)spline.Knots;
        var knotsArray = knots.ToArray();

        // Создаем список точек пути с проверкой столкновений
        List<Vector3> pathPoints = new List<Vector3>();
        Vector3 lastValidPoint = transform.position;

        for (int i = 0; i < knotsArray.Length; i++)
        {
            Vector3 potentialPoint = transform.TransformPoint(knotsArray[i].Position);

            // Проверяем столкновение между предыдущей и текущей точкой
            if (i > 0 && Physics.Linecast(lastValidPoint, potentialPoint, out RaycastHit hit, wallLayerMask))
            {
                // Если есть стена, добавляем точку столкновения и прерываем путь
                pathPoints.Add(hit.point);
                break;
            }

            pathPoints.Add(potentialPoint);
            lastValidPoint = potentialPoint;
        }

        // Проверяем конечную точку на наличие земли под ней
        Vector3 finalPoint = pathPoints[pathPoints.Count - 1];
        if (!Physics.Raycast(finalPoint, Vector3.down, groundCheckDistance, wallLayerMask))
        {
            // Если под конечной точкой нет земли, находим точку приземления
            if (Physics.Raycast(finalPoint, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, wallLayerMask))
            {
                // Добавляем точку приземления с небольшой кривой
                Vector3 landingPoint = groundHit.point;
                Vector3 controlPoint = finalPoint + (landingPoint - finalPoint) * 0.5f + Vector3.up * 2f;

                pathPoints.Add(controlPoint);
                pathPoints.Add(landingPoint);
            }
        }

        // Вычисляем общее время движения с учетом дополнительных точек
        float totalJumpTime = jumpTime * (pathPoints.Count / (float)knotsArray.Length);

        // Запускаем движение по модифицированному пути
        rb.DOPath(pathPoints.ToArray(), totalJumpTime, PathType.CatmullRom).SetEase(Ease.Linear);

        yield return new WaitForSeconds(totalJumpTime);

        attacking = false;
        attackCollider.SetCollider(false);
    }

    // Визуализация в редакторе для отладки
    private void OnDrawGizmosSelected()
    {
        if (jumpSpline == null) return;

        Spline spline = jumpSpline.Splines[0];
        var knots = (List<BezierKnot>)spline.Knots;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < knots.Count - 1; i++)
        {
            Vector3 start = transform.TransformPoint(knots[i].Position);
            Vector3 end = transform.TransformPoint(knots[i + 1].Position);
            Gizmos.DrawLine(start, end);
        }

        // Визуализация проверки земли
        Vector3 finalPoint = transform.TransformPoint(knots[knots.Count - 1].Position);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(finalPoint, Vector3.down * groundCheckDistance);
    }
}
