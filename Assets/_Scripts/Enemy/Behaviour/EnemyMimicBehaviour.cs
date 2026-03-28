using System.Collections;
using UnityEngine;
using static PamukAI.PAI;

public class EnemyMimicBehaviour : BaseEnemyBehaviour
{
    private const string MIMIC_ROOM_TAG = "Mimic Room";

    [Header("Mimic Behaviour: General")]
    [SerializeField] private float attackDistance;
    [SerializeField] private float wakeUpTime;

    [Header("Animation")]
    [SerializeField] private ParticleSystem groundSlamVFX;

    [Header("Components")]
    [SerializeField] private MimicObject mimicObject;
    [SerializeField] private MimicAnimationEvents animEvents;
    [SerializeField] private RoomBehaviour boundRoom;
    [SerializeField] private EnemyComponents components;

    private EnemyHealth mimicHealth => components.Health as EnemyHealth;
    private Animator animator => components.Animator;
    private EnemyMimicAttack mimicAttack => components.Attack as EnemyMimicAttack;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            agent.enabled = true;
            mimicObject.OnWakeUp += Mimic_OnWakeUp;
            mimicHealth.OnDeath += Mimic_OnDeath;

            TryFindBoundRoom();
        }

        mimicHealth.SetIgnoreDamage(true);
        mimicHealth.SetCanGetLockPoint(false);
        animEvents.OnGroundTouch += () => groundSlamVFX.Play();
    }

    public override void OnNetworkDespawn()
    {
        mimicObject.OnWakeUp -= Mimic_OnWakeUp;
        mimicHealth.OnDeath -= Mimic_OnDeath;

        base.OnNetworkDespawn();
    }

    private void TryFindBoundRoom()
    {
        if (!IsServer || boundRoom != null)
            return;

        var mimicRooms = GameObject.FindGameObjectsWithTag(MIMIC_ROOM_TAG);

        if (mimicRooms.Length <= 0)
        {
            Debug.LogError("Объект мимика не нашел комнату для привязки");
            return;
        }

        GameObject nearestRoomObj = null;
        float minDistance = float.MaxValue;

        foreach (var room in mimicRooms)
        {
            float distance = Vector3.Distance(transform.position, room.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestRoomObj = room;
            }
        }

        if (!nearestRoomObj.TryGetComponent(out RoomBehaviour roomBehaviour))
        {
            Debug.LogError("Объект мимика не нашел комнату для привязки");
            return;
        }

        boundRoom = roomBehaviour;
        Debug.Log("Мимик привязан к комнате");
    }

    private void Mimic_OnWakeUp(ulong senderId)
    {
        if (boundRoom != null)
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(senderId, out var client))
                return;

            var senderNetObj = client.PlayerObject;
            var components = senderNetObj.GetComponent<PlayerComponents>();

            LevelManager.Instance.StartRoomBattle_ServerRpc(boundRoom.RoomIndex, false);
            LevelManager.Instance.TeleportPlayers(senderId, components.Movement.transform.position, boundRoom.RoomIndex, false, true);

            Debug.Log($"Mimic Battle Start. NetObject Sender: {client.PlayerObject.name}");
        }
          
        animator.SetTrigger("Wake Up");
        Invoke(nameof(WakeUpEnd), wakeUpTime);
    }

    private void Mimic_OnDeath()
    {
        if (boundRoom != null)
        {
            LevelManager.Instance.EndRoomBattle_ServerRpc(boundRoom.RoomIndex, false);
            Debug.Log("Mimic Battle End");
        }
    }

    private void WakeUpEnd()
    {
        agent.enabled = true;
        mimicHealth.SetIgnoreDamage(false);
        mimicHealth.SetCanGetLockPoint(true);

        SwitchState(Move, ref state);
    }

    private bool Move()
    {
        if (DoOnce())
        {
            mimicAttack.Attack();
        }

        if (IsNearCurrentTarget(attackDistance))
        {
            PredictMoveTowardsTarget();
        }
        else
        {
            MoveTowardsTarget();
        }

        return true;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
