using Unity.Netcode;
using UnityEngine;

public class PlayerAttackBase : NetworkBehaviour
{
    [Header("General: Settings")]
    [SerializeField] protected AttackDamageType attackDamage;
    [SerializeField] protected float attackSpeedMult;

    [Header("General: Components")]
    [SerializeField] protected PlayerComponents playerComponents;
    protected PlayerSFXController sfxController => playerComponents.SfxController;
    protected PlayerStamina playerStamina => playerComponents.Stamina;
    protected PlayerHealth playerHealth => playerComponents.Health;
    protected Animator animator => playerComponents.Animator;

    protected bool attackInput = false;
    protected bool blockAttack = false;
    protected bool vampirism = false;
    protected float percentHealFromAttack = 0f;

    protected bool infiniteDamageCheatEnabled = false;

    public AttackDamageType AttackDamage
    {
        get => attackDamage;
        set
        {
            attackDamage = value;
            UpdateAttackType();
        }
    }
    public float AttackSpeedMult { get => attackSpeedMult; set => ChangeAttackSpeedMult(value); }
    public bool EnableInfiniteDamage { get => infiniteDamageCheatEnabled; set => infiniteDamageCheatEnabled = value; }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        SubToAttackInputs(true);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        SubToAttackInputs(false);
    }

    protected virtual void Start()
    {
        UpdateAttackType();
        ChangeAttackSpeedMult(attackSpeedMult);
    }

    protected void SubToAttackInputs(bool subscribe)
    {
        if (subscribe)
        {
            InputManager.Input.Player.Attack.started += StartAttackInput;
            InputManager.Input.Player.Attack.canceled += EndAttackInput;
            PlayerUI.OnUIChange += UI_OnWindowChange;
        }
        else
        {
            InputManager.Input.Player.Attack.started -= StartAttackInput;
            InputManager.Input.Player.Attack.canceled -= EndAttackInput;
            PlayerUI.OnUIChange -= UI_OnWindowChange;
        }
    }

    protected void StartAttackInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (playerStamina.CurrentStamina < playerStamina.AttackConsumage || blockAttack)
            return;

        SetAttackInput(true);
    }

    protected void EndAttackInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        SetAttackInput(false);
    }

    protected void UI_OnWindowChange(GameUIWindowType type)
    {
        SetAttackInput(false);
    }

    protected void SetAttackInput(bool attack)
    {
        attackInput = attack;
        animator.SetBool("Attacking", attackInput);
    }

    protected void ChangeAttackSpeedMult(float newMult)
    {
        attackSpeedMult = newMult;
        animator.SetFloat("Attack Speed Mult", attackSpeedMult);
    }

    protected virtual void UpdateAttackType()
    {
        
    }

    public virtual void BlockAttack(bool block)
    {
        if (block)
            SetAttackInput(false);
    }

    #region Effects

    public void EnableVampirism(bool enable, float percentHealFromAttack = 0f)
    {
        vampirism = enable;

        if (enable)
        {
            this.percentHealFromAttack += percentHealFromAttack;
        }
        else
        {
            this.percentHealFromAttack = 0;
        }
    }

    public void TryVampireHeal(float damage)
    {
        if (!vampirism)
            return;

        playerHealth.Heal(damage * percentHealFromAttack);
    }

    #endregion
}
