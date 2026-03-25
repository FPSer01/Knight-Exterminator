using UnityEngine;
using UnityEngine.AI;

public class AgentTest : MonoBehaviour
{
    [SerializeField] private Transform chaseTarget;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (chaseTarget != null)
            agent.SetDestination(chaseTarget.position);
    }
}
