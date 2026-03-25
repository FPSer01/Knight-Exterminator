using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class EntityHealth : NetworkBehaviour
{
    protected const float BLOODLOSS_DAMAGE = 4f;
    protected const float POISON_DAMAGE = 2f;

    [Header("Health Settings")]
    [SerializeField] protected float maxHealth;
    [Space]
    [SerializeField]
    protected NetworkVariable<float> currentHealth = new(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    [SerializeField] protected float clientSideHealth;
    [Space]
    [SerializeField] protected HealthDamageResist healthResist;

    [Header("VFX")]
    [SerializeField] protected GameObject hitEffectPrefab;
    [SerializeField] protected Vector3 hitEffectScale = Vector3.one;
    [SerializeField] protected ParticleSystem stunEffect;

    protected NetworkVariable<bool> dead = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    protected Coroutine bloodlossCoroutine;
    protected Coroutine poisonCoroutine;

    #region Public Interface

    public float CurrentHealth { get => currentHealth.Value; }
    public float MaxHealth { get => maxHealth; set => ChangeMaxHealth(value); }
    public HealthDamageResist ResistData { get => healthResist; set => healthResist = value; }
    public bool IsDead { get => dead.Value; }

    #endregion

    #region Events

    public event Action OnDeath;
    public event Action OnRevive;
    public event Action OnNetworkDespawnEvent;

    public event Action<float> OnDamageTaken;
    public event Action<StatusInflictData> OnStatusTaken;

    public event Action<StatusType> OnStatusInflicted;
    public event Action<StatusType> OnStatusWearOff;

    protected void DoOnDeath() => OnDeath?.Invoke();
    protected void DoOnRevive() => OnRevive?.Invoke();
    protected void DoOnNetworkDespawnEvent() => OnNetworkDespawnEvent?.Invoke();

    protected void DoOnDamageTaken(float damageTaken) => OnDamageTaken?.Invoke(damageTaken);
    protected void DoOnStatusTaken(StatusInflictData statusData) => OnStatusTaken?.Invoke(statusData);

    protected void DoOnStatusInflicted(StatusType type) => OnStatusInflicted?.Invoke(type);
    protected void DoOnStatusWearOff(StatusType type) => OnStatusWearOff?.Invoke(type);

    #endregion

    #region Network API

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += Health_OnValueChange;
        dead.OnValueChanged += Death_OnValueChange;

        SetHealthValues();
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= Health_OnValueChange;
        dead.OnValueChanged -= Death_OnValueChange;

        DoOnNetworkDespawnEvent();
    }

    protected virtual void SetHealthValues()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;

        clientSideHealth = maxHealth;
    }

    #endregion

    #region Abstract Methods

    public abstract float TakeDamage(AttackDamageType damage, EntityHealth sender);
    public abstract void Heal(float healAmount);

    public abstract void CreateHitEffect(HitTransform hitTransform);
    protected abstract void UpdateHealthUI();

    #endregion

    protected virtual void Health_OnValueChange(float previousValue, float newValue)
    {
        clientSideHealth = newValue;
        UpdateHealthUI();
        CheckForDeath_ServerSide();
    }

    protected void Death_OnValueChange(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            DoOnDeath();
        }
        else
        {
            DoOnRevive();
        }
    }

    #region Network and Prediction Methods 

    /// <summary>
    /// Поменять значение здоровья со стороны клиента, затем отправляет серверу
    /// </summary>
    /// <param name="newValue"></param>
    protected virtual void ChangeClientSideHealth(float newValue)
    {
        clientSideHealth = Mathf.Clamp(newValue, 0, maxHealth);
        UpdateHealthUI();

        ChangeServerHealth_Rpc(newValue);
    }

    [Rpc(SendTo.Server)]
    protected void ChangeServerHealth_Rpc(float newValue)
    {
        currentHealth.Value = Mathf.Clamp(newValue, 0, maxHealth);
    }

    [Rpc(SendTo.Server)]
    protected void SetDeathStatus_ServerRpc(bool isDead)
    {
        dead.Value = isDead;
    }

    [Rpc(SendTo.Server)]
    protected void RequestEnemyDespawn_ServerRpc(float delay = 0)
    {
        StartCoroutine(DespawnNetworkObject(delay));
    }

    private IEnumerator DespawnNetworkObject(float delay)
    {
        yield return new WaitForSeconds(delay);

        NetworkObject.Despawn();
    }

    #endregion

    #region Status Methods

    protected void TakeStatusDamage(float damage)
    {
        if (IsDead)
            return;

        if (IsServer)
        {
            currentHealth.Value = Mathf.Clamp(currentHealth.Value - damage, 0, maxHealth);
            CheckForDeath_ServerSide();
        }
        else
        {
            ChangeClientSideHealth(clientSideHealth - damage);
            CheckForDeath_ClientSide();
        }
    }

    public virtual void SetActiveStatus(StatusType type, StatusInflictData data, bool active)
    {
        switch (type)
        {
            case StatusType.Bloodloss:
                if (active)
                    bloodlossCoroutine = StartCoroutine(TickStatusEffect(BLOODLOSS_DAMAGE / 100f * maxHealth));
                else if (bloodlossCoroutine != null)
                    StopCoroutine(bloodlossCoroutine);
                break;

            case StatusType.Poison:
                if (active)
                    poisonCoroutine = StartCoroutine(TickStatusEffect(POISON_DAMAGE / 100f * maxHealth));
                else if (poisonCoroutine != null)
                    StopCoroutine(poisonCoroutine);
                break;
        }

        if (active)
            DoOnStatusInflicted(type);
        else
            DoOnStatusWearOff(type);
    }

    protected IEnumerator TickStatusEffect(float damage, float timeFrame = 0.1f)
    {
        while (true)
        {
            float timeframeDamage = damage * timeFrame;
            TakeStatusDamage(timeframeDamage);
            yield return new WaitForSeconds(timeFrame);
        }
    }

    #endregion

    #region Utility Methods

    protected virtual void CheckForDeath_ClientSide()
    {
        if (clientSideHealth <= 0 && !IsDead)
        {
            SetDeathStatus_ServerRpc(true);
        }
    }

    protected virtual void CheckForDeath_ServerSide()
    {
        if (currentHealth.Value <= 0 && !IsDead)
        {
            SetDeathStatus_ServerRpc(true);
        }
    }

    public void ChangeMaxHealth(float newMaxHealth)
    {
        float oldMax = maxHealth;
        float healAmount = newMaxHealth - oldMax;
        maxHealth = newMaxHealth;

        Heal(healAmount);
    }

    protected float GetFinalDamage(AttackDamageType damage)
    {
        float physicalDamage = damage.Physical * Mathf.Clamp01(1f - healthResist.Physical);
        float fireDamage = damage.Fire * Mathf.Clamp01(1f - healthResist.Fire);
        float electricalDamage = damage.Electrical * Mathf.Clamp01(1f - healthResist.Electrical);

        float finalDamage = physicalDamage + fireDamage + electricalDamage;
        finalDamage = damage.IgnoreDefence ? finalDamage : finalDamage - healthResist.FlatResistance;

        if (finalDamage < 0)
            finalDamage = 0;

        return finalDamage;
    }

    protected StatusInflictData GetFinalStatus(StatusInflictData statusData)
    {
        statusData.Bloodloss *= Mathf.Clamp01(1f - healthResist.Bloodloss);
        statusData.Poison *= Mathf.Clamp01(1f - healthResist.Poison);
        statusData.SlownessAmount *= Mathf.Clamp01(1f - healthResist.Slowness);
        statusData.StunAmount *= Mathf.Clamp01(1f - healthResist.Stun);

        return statusData;
    }

    #endregion
}