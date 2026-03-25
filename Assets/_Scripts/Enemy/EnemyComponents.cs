using Unity.Netcode;
using UnityEngine;

public class EnemyComponents : NetworkBehaviour
{
    [Header("Enemy Components")]
    [SerializeField] private Collider enemyCollider;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [Space]
    [SerializeField] private BaseEnemyBehaviour behaviour;
    [SerializeField] private BaseEnemyAttack attack;
    [SerializeField] private EntityHealth health;
    [SerializeField] private EnemySFXController sfxController;
    [SerializeField] private BaseEnemyDrops drops;

    public Collider EnemyCollider { get => enemyCollider; }
    public Rigidbody Rigidbody { get => rb; }
    public BaseEnemyBehaviour Behaviour { get => behaviour; }
    public BaseEnemyAttack Attack { get => attack; }
    public EntityHealth Health { get => health; }
    public EnemySFXController SFXController { get => sfxController; }
    public BaseEnemyDrops Drops { get => drops; }
    public Animator Animator { get => animator; }
}
