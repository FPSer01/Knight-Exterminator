using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class EnemyJumperAttack : BaseEnemyAttack
{
    [Header("Jumper Settings")]
    [SerializeField] private EnemyTouchAttackCollider attackCollider;
    [Space]
    [SerializeField] private float jumpTime = 1f;
    [SerializeField] private SplineContainer jumpSpline;
    [SerializeField] private int splineSampleCount = 20;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private float groundCheckDistance = 1f;

    private float currentJumpTime;

    [Header("SFX Settings")]
    [Range(0f, 1f)][SerializeField] private float attackSFXVolume = 1;

    [Header("Components")]
    [SerializeField] private EnemyComponents components;
    private EnemySFXController sfxController => components.SFXController;
    private Rigidbody rb => components.Rigidbody;

    public float JumpTime => jumpTime;
    public float CurrentJumpTime => currentJumpTime;

    private void Start()
    {
        attackCollider.OnHit += AttackCollider_OnHit;
    }

    private void AttackCollider_OnHit(PlayerHealth player, HitTransform hitPos)
    {
        player.TakeDamage(attackDamage, GetComponent<EnemyHealth>());
        player.CreateHitEffect(hitPos);
    }

    public override void Attack()
    {
        if (!canAttack)
            return;

        base.Attack();

        sfxController.PlayAttackSFX(attackSFXVolume);
        StartCoroutine(InitiateJumpAttack());
    }

    private IEnumerator InitiateJumpAttack()
    {
        attackCollider.SetCollider(true);

        // Семплируем сплайн в мировых координатах
        Vector3[] worldPath = SampleSplineWorldPoints();

        if (worldPath == null || worldPath.Length < 2)
        {
            attackCollider.SetCollider(false);
            yield break;
        }

        // Обрезаем путь по столкновению со стенами
        Vector3[] clampedPath = ClampPathByWalls(worldPath);

        // Вычисляем общее время движения
        float totalJumpTime = jumpTime * (clampedPath.Length / (float)worldPath.Length);

        rb.DOKill();
        rb.DOPath(clampedPath, jumpTime, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                attackCollider.SetCollider(false);
            });

        currentJumpTime = totalJumpTime;

        yield return new WaitForSeconds(totalJumpTime);
    }

    /// <summary>
    /// Семплирует сплайн равномерно и возвращает точки в мировом пространстве.
    /// </summary>
    private Vector3[] SampleSplineWorldPoints()
    {
        if (jumpSpline == null || jumpSpline.Splines.Count == 0) return null;

        var spline = jumpSpline.Splines[0];
        var points = new Vector3[splineSampleCount];

        for (int i = 0; i < splineSampleCount; i++)
        {
            float t = i / (float)(splineSampleCount - 1);

            // Получаем позицию в локальном пространстве SplineContainer
            Vector3 localPos = spline.EvaluatePosition(t);

            // Переводим в мировое пространство через трансформ контейнера
            points[i] = jumpSpline.transform.TransformPoint(localPos);
        }

        return points;
    }

    /// <summary>
    /// Обрезает путь при столкновении со стеной.
    /// </summary>
    private Vector3[] ClampPathByWalls(Vector3[] path)
    {
        var result = new List<Vector3>
        {
            path[0]
        };

        for (int i = 1; i < path.Length; i++)
        {
            Vector3 from = path[i - 1];
            Vector3 to = path[i];

            if (Physics.Linecast(from, to, out RaycastHit hit, wallLayerMask))
            {
                // Добавляем точку столкновения и обрываем путь
                result.Add(hit.point - (to - from).normalized * 0.05f);
                break;
            }

            result.Add(to);
        }

        return result.ToArray();
    }

    private void OnDrawGizmosSelected()
    {
        if (jumpSpline == null || jumpSpline.Splines.Count == 0) return;

        var spline = jumpSpline.Splines[0];
        int steps = 30;

        Gizmos.color = Color.yellow;
        Vector3 prev = jumpSpline.transform.TransformPoint(spline.EvaluatePosition(0f));

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 curr = jumpSpline.transform.TransformPoint(spline.EvaluatePosition(t));
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }

        // Визуализация проверки земли у конечной точки
        Vector3 endPoint = jumpSpline.transform.TransformPoint(spline.EvaluatePosition(1f));
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(endPoint, Vector3.down * groundCheckDistance);
    }
}
