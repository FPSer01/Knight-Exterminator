using UnityEngine;

public class BeeSwarmHealth : EntityHealth
{
    public override void CreateHitEffect(HitTransform hitTransform)
    {
        return;
    }

    public override void Heal(float healAmount)
    {
        return;
    }

    public override float TakeDamage(AttackDamageType damage, EntityHealth sender)
    {
        return 0;
    }

    protected override void UpdateHealthUI()
    {
        return;
    }

    public void ForceTriggerDeath()
    {
        dead.Value = true;
        Destroy(gameObject);
    }
}
