using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBehaviour : NetworkBehaviour
{
    protected Transform mainTarget;
    protected Rigidbody mainTargetRB;
    protected List<Transform> possibleTargets = new();
    protected NavMeshAgent agent;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwnedByServer)
            return;

        StartCoroutine(WaitForTargets());
    }

    private IEnumerator WaitForTargets()
    {
        while (possibleTargets.Count < NetworkManager.ConnectedClientsIds.Count)
        {
            SeekTarget();
            yield return new WaitForEndOfFrame();
        }
    }

    protected virtual void Update()
    {
        if (!IsOwnedByServer || possibleTargets.Count == 0)
            return;

        ResetTarget();

    }

    protected void SeekTarget()
    {
        possibleTargets = new();
        var targets = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        if (targets.Length == 0)
        {
            Debug.Log("No Targets in Scene is Found", this);
            return;
        }

        if (targets.Length == 1)
        {
            mainTarget = targets[0].transform;
            Debug.Log("1 Target in Scene is Found", this);
            return;
        }

        float distance = float.MaxValue;

        foreach (var target in targets)
        {
            float distanceToTarget = (target.transform.position - transform.position).magnitude;

            if (distanceToTarget < distance)
            {
                distance = distanceToTarget;
                mainTarget = target.transform;
            }

            possibleTargets.Add(target.transform);
        }

        SetTargetRigidbody();
    }

    protected void ResetTarget()
    {
        float distance = float.MaxValue;

        foreach (var target in possibleTargets)
        {
            float distanceToTarget = (target.transform.position - transform.position).magnitude;

            if (distanceToTarget < distance)
            {
                distance = distanceToTarget;
                mainTarget = target.transform;
                SetTargetRigidbody();
            }
        }
    }

    private void SetTargetRigidbody()
    {
        mainTargetRB = mainTarget.GetComponent<Rigidbody>();
    }

    protected void RotateTowardsTarget(float turnSpeed)
    {
        Vector3 targetPlanePosition = new Vector3(mainTarget.position.x, transform.position.y, mainTarget.position.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetPlanePosition - transform.position), turnSpeed * Time.deltaTime);
    }

    protected void RotateBackwardsTarget(float turnSpeed)
    {
        Vector3 targetPlanePosition = new Vector3(mainTarget.position.x, transform.position.y, mainTarget.position.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(transform.position - targetPlanePosition), turnSpeed * Time.deltaTime);
    }

    protected void PredictMoveTowardsTarget()
    {
        var destination = CalculatePredictedPosition(mainTargetRB, agent.speed * 2);
        agent.SetDestination(destination);
    }

    protected void MoveTowardsTarget(Vector3 offset = default)
    {
        agent.SetDestination(mainTarget.position + offset);
    }

    protected Vector3 CalculatePredictedPosition(Rigidbody target, float moveSpeed)
    {
        Vector3 targetDirection = target.position - transform.position;

        float timeToTarget = targetDirection.magnitude / moveSpeed;

        Vector3 predictedPos = target.position + target.linearVelocity * timeToTarget;

        return predictedPos;
    }
}
