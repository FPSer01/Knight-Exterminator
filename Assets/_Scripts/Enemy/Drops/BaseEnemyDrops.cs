using Unity.Netcode;
using UnityEngine;

public class BaseEnemyDrops : NetworkBehaviour
{
    [Header("Base Drops Settings")]
    [SerializeField] protected EntityHealth enemyHealth;
    [SerializeField] protected int XPAmount;
    [Space]
    [SerializeField] protected Transform dropPoint;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        enemyHealth.OnDeath += GiveDrop;
    }

    public void SetDropPoint(Transform point)
    {
        dropPoint = point;
    }

    protected virtual void GiveDrop()
    {
        if (XPAmount != 0)
        {
            EnemyManager.Instance.DoOnEnemyXPDrop(XPAmount);
        }
    }
}
