using UnityEngine;

public class EnemyBossComponents : EnemyComponents
{
    [Header("Boss Components")]
    [SerializeField] private string bossName;

    public string BossName { get => bossName; }
}
