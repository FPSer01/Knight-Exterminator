using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using static PamukAI.PAI;

/// <summary>
/// Базовый класс поведения врагов
/// </summary>
public abstract class BaseEnemyBehaviour : NetworkBehaviour
{
    [Header("Base Behaviour: General")]
    [SerializeField] protected float seeDistance;
    protected bool isTargetSeen = false;
    [Space]
    // Делать фокус только на первую попавшеюся цель
    [SerializeField] protected bool focusOnFirstSeenTarget;

    protected NavMeshAgent agent;

    protected EnemyTargetData currentTarget;
    protected List<EnemyTargetData> allTargets = new();

    protected float originalMoveSpeed;
    protected float originalMoveAcceleration;
    protected float originalTurnSpeed;

    protected Method state;

    #region Network API

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();

        if (IsServer)
        {
            OnSpawned();
        }
        else
        {
            agent.enabled = false;
        }
    }

    protected virtual void OnSpawned() 
    {
        originalMoveSpeed = agent.speed;
        originalMoveAcceleration = agent.acceleration;
        originalTurnSpeed = agent.angularSpeed;

        StartCoroutine(FindAllTargets());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        OnDespawned();
    }

    protected virtual void OnDespawned() { }

    #endregion

    #region Choose Target Logic

    private IEnumerator FindAllTargets()
    {
        if (!IsServer)
            yield break;

        allTargets = new();

        while (PlayerManager.Instance.SpawnedPlayers.Count < NetworkManager.ConnectedClientsIds.Count)
            yield return null;

        foreach (var clientId in NetworkManager.ConnectedClientsIds)
        {
            GameObject playerObj = PlayerManager.Instance.SpawnedPlayers[clientId];

            if (playerObj == null)
                continue;

            EnemyTargetData target = new(clientId, playerObj);
            allTargets.Add(target);
        }

        // Ждём пока все NetworkObject точно заспавнятся
        yield return new WaitUntil(() => allTargets.All(t => t.NetworkObject != null && t.NetworkObject.IsSpawned));

        if (allTargets.Count == 0)
        {
            Debug.LogWarning("FindAllTargets: список целей пуст, повтор...", this);
            StartCoroutine(FindAllTargets());
            yield break;
        }

        SetClosestTarget();
    }

    private void SetClosestTarget()
    {
        float distance = float.MaxValue;

        EnemyTargetData closestTarget = new();

        foreach (var target in allTargets)
        {
            if (target.IsDead)
                continue;

            float distanceToTarget = (target.Position - transform.position).sqrMagnitude;

            if (distanceToTarget < distance)
            {
                distance = distanceToTarget;
                closestTarget = target;
            }
        }

        if (closestTarget.IsValid)
        {
            SetCurrentTarget(closestTarget);
        }
    }

    private void SetCurrentTarget(EnemyTargetData newTarget)
    {
        if (currentTarget.IsValid && currentTarget == newTarget)
            return;

        if (currentTarget.IsValid)
        {
            PlayerHealth oldPlayerHealth = currentTarget.Components.Health;
            oldPlayerHealth.OnDeath -= CurrentTarget_OnDeath;
        }

        PlayerHealth newPlayerHealth = newTarget.Components.Health;
        newPlayerHealth.OnDeath += CurrentTarget_OnDeath;
        currentTarget = newTarget;
    }

    private void CurrentTarget_OnDeath()
    {
        isTargetSeen = false;
        SetClosestTarget();
    }

    #endregion

    protected virtual void Update()
    {
        if (!IsServer)
            return;

        // ОБНОВЛЕНИЕ ЦЕЛИ:
        if (!currentTarget.IsValid)
        {
            SetClosestTarget();
            return;
        }

        // Если включен режим преследолвания одной цели
        if (focusOnFirstSeenTarget && !IsNearCurrentTarget(seeDistance) && !isTargetSeen)
        {
            SetClosestTarget();
        }
        else if (focusOnFirstSeenTarget && !isTargetSeen)
        {
            isTargetSeen = true;
        }
        // Режим переключения к ближайшей цели
        else if (!focusOnFirstSeenTarget)
        {
            SetClosestTarget();
        }

        // Обновляем состояние
        if (state != null)
            Tick(state);
    }

    /// <summary>
    /// Идти к цели
    /// </summary>
    protected void MoveTowardsTarget()
    {
        agent.SetDestination(currentTarget.Position);
    }

    /// <summary>
    /// Отойти от цели в противоположном направлении на distance
    /// </summary>
    /// <param name="distance"></param>
    protected void MoveFromTarget(float distance = 1f)
    {
        agent.SetDestination(transform.position + (transform.position - currentTarget.Position).normalized * distance);
    }

    protected void PredictRotateTowardsTarget(float turnSpeed, float predictMoveSpeedMult = 1f)
    {
        var targetPosition = CalculatePredictedPosition(currentTarget, agent.speed * predictMoveSpeedMult);

        Vector3 targetPlanePosition = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetPlanePosition - transform.position), turnSpeed * Time.deltaTime);
    }

    protected void PredictMoveTowardsTarget(float predictMoveSpeedMult = 1f)
    {
        var destination = CalculatePredictedPosition(currentTarget, agent.speed * predictMoveSpeedMult);
        agent.SetDestination(destination);
    }

    protected Vector3 GetPredictedPositionCurrentTarget(float predictMoveSpeedMult = 1f)
    {
        return CalculatePredictedPosition(currentTarget, agent.speed * predictMoveSpeedMult);
    }

    protected Vector3 CalculatePredictedPosition(EnemyTargetData target, float moveSpeed)
    {
        NetworkRigidbody netRB = target.Components.NetworkRigidbody;

        Vector3 targetDirection = target.Position - transform.position;
        float timeToTarget = targetDirection.magnitude / moveSpeed;
        Vector3 predictedPos = target.Position + netRB.GetLinearVelocity() * timeToTarget;

        return predictedPos;
    }

    /// <summary>
    /// Плавно повернуть модель врага лицом к цели.
    /// </summary>
    protected void RotateTowardsTarget(float turnSpeed)
    {
        Vector3 targetPlanePosition = new Vector3(currentTarget.Position.x, transform.position.y, currentTarget.Position.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetPlanePosition - transform.position), turnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Проверить, находится ли <paramref name="target"/> в радиусе <paramref name="distance"/> от <paramref name="origin"/>.
    /// </summary>
    protected bool IsNear(Transform origin, Transform target, float distance)
    {
        return Vector3.Distance(origin.position, target.position) <= distance;
    }

    /// <summary>
    /// Находиться ли target в близи врага на distance
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    protected bool IsNearCurrentTarget(float distance)
    {
        if (!currentTarget.IsValid)
        {
            Debug.LogError("No Target or is invalid");
            return false;
        }

        return Vector3.Distance(transform.position, currentTarget.Position) <= distance;
    }

    /// <summary>
    /// Вычислить горизонтальный угол (в плоскости XZ) между направлением взгляда врага и целью.
    /// </summary>
    protected float GetHorizontalAngleToTarget()
    {
        Vector3 targetFlat = new(currentTarget.Position.x, transform.position.y, currentTarget.Position.z);
        return Mathf.Abs(Vector3.Angle(transform.forward, targetFlat - transform.position));
    }

    /// <summary>
    /// Вычислить горизонтальное расстояние до цели (по плоскости XZ).
    /// </summary>
    protected float GetHorizontalDistanceToTarget()
    {
        Vector3 targetFlat = new(currentTarget.Position.x, transform.position.y, currentTarget.Position.z);
        return Vector3.Distance(targetFlat, transform.position);
    }

    #region Agent Setters

    protected void ChangeMoveSpeed(float newValue)
    {
        agent.speed = newValue;
    }

    protected void ResetMoveSpeed()
    {
        agent.speed = originalMoveSpeed;
    }

    protected void ChangeMoveAcceleration(float newValue)
    {
        agent.acceleration = newValue;
    }

    protected void ResetMoveAcceleration()
    {
        agent.acceleration = originalMoveAcceleration;
    }

    protected void ChangeTurnSpeed(float newValue)
    {
        agent.angularSpeed = newValue;
    }

    protected void ResetTurnSpeed()
    {
        agent.angularSpeed = originalTurnSpeed;
    }

    #endregion

    protected virtual void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, seeDistance);
    }
}
