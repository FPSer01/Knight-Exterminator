using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using static PamukAI.PAI;
using Random = UnityEngine.Random;

public class EnemyMeleeBehaviour : BaseEnemyBehaviour
{
    [Header("Melee Behaviour: General")]
    [SerializeField] private float waitDistance;
    [SerializeField] private float waitTime;
    [SerializeField] private float rotateSpeed;
    [Space]
    [SerializeField] private float attackDistance;
    [SerializeField] private float attackAngle;
    [SerializeField] private float delayBeforeAttack;
    [SerializeField] private float delayAfterAttack;
    [Space]
    [SerializeField] private float rushMoveSpeed;
    [SerializeField] private float rushAcceleration;

    [Header("Melee Behaviour: Dodge")]
    [SerializeField] private float dodgeTime;
    [SerializeField] private SplineContainer dodgePaths;
    [SerializeField] private int splineSampleCount;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask wallLayerMask;

    private float currentDodgeTime;

    [Header("Melee Behaviour: Behaviour Options")]
    [SerializeField] private bool predictMove;
    [SerializeField] private float predictMoveMult;
    [Space]
    [SerializeField] private bool predictMoveRush;
    [SerializeField] private float predictMoveRushMult;
    [Space]
    [SerializeField] private bool jumpToCloseDistance;
    [SerializeField] private SplineContainer forwardPath;

    [Header("Components")]
    [SerializeField] private EnemyComponents enemyComponents;

    private EnemyMeleeAttack enemyAttack => enemyComponents.Attack as EnemyMeleeAttack;
    private Animator animator => enemyComponents.Animator;
    private EnemySFXController sfxController => enemyComponents.SFXController;
    private Rigidbody rb => enemyComponents.Rigidbody;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        SwitchState(Idle, ref state);
    }

    private bool Idle()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
        }

        if (IsNearCurrentTarget(seeDistance))
        {
            SwitchState(Move, ref state);
        }

        return true;
    }

    private bool Move()
    {
        if (DoOnce())
        {
            sfxController.PlayMoveSFX(true);
            animator.SetBool("Move", true);
        }

        if (CanMeleeStrike())
        {
            SwitchState(MeleeAttack, ref state);
        }

        if (IsNearCurrentTarget(waitDistance))
        {
            SwitchState(Stance, ref state);
        }

        if (predictMove)
        {
            PredictMoveTowardsTarget(predictMoveMult);
        }
        else
        {
            MoveTowardsTarget();
        }

        return true;
    }

    private bool Stance()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            agent.enabled = false;
        }

        if (Wait(waitTime))
        {
            if (IsNearCurrentTarget(attackDistance))
                PredictRotateTowardsTarget(rotateSpeed, 2f);
            else
                RotateTowardsTarget(rotateSpeed);

            if (CanMeleeStrike())
            {
                SwitchState(MeleeAttack, ref state);
            }

            return true;
        }

        if (Step())
        {
            if (jumpToCloseDistance)
            {
                SwitchState(DodgeForward, ref state);
            }
            else
            {
                SwitchState(RushForAttack, ref state);
            }
        }

        return true;
    }

    private bool RushForAttack()
    {
        if (DoOnce())
        {
            ChangeMoveSpeed(rushMoveSpeed);
            ChangeMoveAcceleration(rushAcceleration);
            agent.enabled = true;

            animator.SetBool("Move", true);
            sfxController.PlayMoveSFX(true);
        }

        if (predictMoveRush)
        {
            PredictMoveTowardsTarget(predictMoveRushMult);
        }
        else
        {
            MoveTowardsTarget();
        }

        if (CanMeleeStrike())
        {
            ResetMoveSpeed();
            ResetMoveAcceleration();

            SwitchState(MeleeAttack, ref state);
        }

        return true;
    }

    private bool MeleeAttack()
    {
        if (DoOnce())
        {
            animator.SetBool("Move", false);
            sfxController.PlayMoveSFX(false);
            animator.SetTrigger("Attack");

            agent.enabled = false;
        }

        if (Wait(delayBeforeAttack))
        {
            return true;
        }

        if (DoOnce())
        {
            enemyAttack.Attack();
        }

        if (Wait(enemyAttack.TotalAttackTime))
        {
            return true;
        }

        if (Wait(delayAfterAttack))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;

            SwitchState(Dodge, ref state);
        }

        return true;
    }

    private bool DodgeForward()
    {
        if (DoOnce())
        {
            StartCoroutine(InitiateDodge(forwardPath.Splines[0]));
        }

        if (Wait(currentDodgeTime))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;
            SwitchState(MeleeAttack, ref state);
        }

        return true;
    }

    private bool Dodge()
    {
        if (DoOnce())
        {
            int pathIndex = Random.Range(0, dodgePaths.Splines.Count);

            StartCoroutine(InitiateDodge(dodgePaths[pathIndex]));
        }

        if (Wait(currentDodgeTime))
        {
            return true;
        }

        if (Step())
        {
            agent.enabled = true;
            SwitchState(Move, ref state);
        }

        return true;
    }

    #region Support Methods

    private IEnumerator InitiateDodge(Spline dodgeSpline)
    {
        agent.enabled = false;

        // Семплируем сплайн в мировых координатах
        Vector3[] worldPath = SampleSplineWorldPoints(dodgeSpline);

        if (worldPath == null || worldPath.Length < 2)
        {
            yield break;
        }

        // Обрезаем путь по столкновению со стенами
        Vector3[] clampedPath = ClampPathByWalls(worldPath);

        // Вычисляем общее время движения
        float totalDodgeTime = dodgeTime * (clampedPath.Length / (float)worldPath.Length);

        rb.DOKill();
        rb.DOPath(clampedPath, dodgeTime, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                agent.enabled = true;
            });

        currentDodgeTime = totalDodgeTime;

        yield return new WaitForSeconds(totalDodgeTime);
    }

    /// <summary>
    /// Семплирует сплайн равномерно и возвращает точки в мировом пространстве.
    /// </summary>
    private Vector3[] SampleSplineWorldPoints(Spline spline)
    {
        var points = new Vector3[splineSampleCount];

        for (int i = 0; i < splineSampleCount; i++)
        {
            float t = i / (float)(splineSampleCount - 1);

            // Получаем позицию в локальном пространстве SplineContainer
            Vector3 localPos = spline.EvaluatePosition(t);

            // Переводим в мировое пространство через трансформ контейнера
            points[i] = transform.TransformPoint(localPos);
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

    private bool CanMeleeStrike()
    {
        return IsNearCurrentTarget(attackDistance) && enemyAttack.CanAttack && attackAngle >= GetHorizontalAngleToTarget();
    }

    #endregion

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, waitDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, attackAngle / 2, 0) * transform.forward * attackDistance + transform.position);
        Gizmos.DrawLine(transform.position, Quaternion.Euler(0, -attackAngle / 2, 0) * transform.forward * attackDistance + transform.position);

        foreach (var spline in dodgePaths.Splines)
        {
            int steps = 30;

            Gizmos.color = Color.yellow;
            Vector3 prev = dodgePaths.transform.TransformPoint(spline.EvaluatePosition(0f));

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 curr = dodgePaths.transform.TransformPoint(spline.EvaluatePosition(t));
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }

            // Визуализация проверки земли у конечной точки
            Vector3 endPoint = dodgePaths.transform.TransformPoint(spline.EvaluatePosition(1f));
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(endPoint, Vector3.down * groundCheckDistance);
        }
    }
}
