using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerStance : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private List<StanceInfo> avalableStances;
    [Space]
    [Tooltip("Debug Stuff")]
    [SerializeField] private bool debug = false;
    [SerializeField] private StanceType startStance;
    [SerializeField] private Stance currentStance;

    [Header("VFX")]
    [SerializeField] private ParticleSystem reloadStanceVFX;

    [Header("Attack Stance (Thrust)")]
    [SerializeField] private PlayerAttackCollider thrustAttackCollider;
    [SerializeField] private float thrustDistance;
    [SerializeField] private float thrustAttackMult;
    [SerializeField] private float thrustDelay;

    [Header("Defense Stance (Reflect)")]
    [SerializeField] private float defenseStanceDelay;
    [SerializeField] private float cutDamageMult;
    [SerializeField] private ParticleSystem shieldVFX;

    [Header("Dexterity Stance (Infinite Stamina)")]
    [SerializeField] private float staminaStanceDelay;
    [SerializeField] private float moveMultIncrease;

    [Header("Components")]
    [SerializeField] private PlayerComponents playerComponents;
    private PlayerStamina playerStamina => playerComponents.Stamina;
    private PlayerMovement playerMovement => playerComponents.Movement;
    private PlayerAttackBase playerAttack => playerComponents.Attack;
    private PlayerHealth playerHealth => playerComponents.Health;
    private Animator animator => playerComponents.Animator;
    private PlayerSFXController sfxController => playerComponents.SfxController;
    private PlayerStateUI playerState => playerComponents.UI.PlayerStateUI;

    public Stance CurrentStance { get => currentStance; set => currentStance = value; }
    public float ThrustAttackMult { get => thrustAttackMult; set => thrustAttackMult = value; }

    private bool skillActive = false;
    private bool skillCooldown = false;
    private bool blockSkill = false;

    private PlayerNetworkObject boundPlayer;

    private void Awake()
    {
        if (debug)
            ExecuteSetStance(startStance);
    }

    #region Network

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        ResetSkillState();
        InputManager.Input.Player.Skill.started += Skill_started;

        if (boundPlayer == null && StartGameData.GameMode == Gamemode.Multiplayer)
        {
            FindAndBindLocalData();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        InputManager.Input.Player.Skill.started -= Skill_started;

        if (StartGameData.GameMode == Gamemode.Multiplayer)
            UnboundPlayer();
    }

    #endregion

    private void FindAndBindLocalData()
    {
        var allDataObjects = FindObjectsByType<PlayerNetworkObject>(FindObjectsSortMode.None);
        foreach (var dataObj in allDataObjects)
        {
            if (dataObj.OwnerClientId == OwnerClientId)
            {
                BoundToPlayer(dataObj);
                break;
            }
        }
    }

    public void BoundToPlayer(PlayerNetworkObject player)
    {
        if (boundPlayer != null) UnboundPlayer();

        boundPlayer = player;
        boundPlayer.Stance.OnValueChanged += OnStanceChanged;
        
        ExecuteSetStance(boundPlayer.Stance.Value);
    }

    public void UnboundPlayer()
    {
        boundPlayer.Stance.OnValueChanged -= OnStanceChanged;
        boundPlayer = null;
    }

    private void OnStanceChanged(StanceType previousValue, StanceType newValue)
    {
        ExecuteSetStance(newValue);
    }

    #region Setup Stance

    private StanceInfo GetStanceByType(StanceType type)
    {
        foreach (var stance in avalableStances)
        {
            if (stance.Type == type)
            {
                return stance;
            }
        }

        return null;
    }

    public void ExecuteSetStance(StanceType type)
    {
        StanceInfo stanceInfo = GetStanceByType(type);

        if (stanceInfo == null)
            return;

        currentStance = stanceInfo.StanceData;

        if (type == StanceType.Default)
        {
            playerState.SetStanceBarValue(0);
        }
        else
        {
            playerState.SetStanceBarValue(1);
        }

        playerHealth.SetMaxReviveCount(currentStance.Revives);

        thrustAttackCollider.OnEnemyHit += OnThrustHit;
        thrustAttackCollider.SetCollider(false);
        thrustAttackCollider.SetTriggerOnEnter(true);
    }

    public void ResetSkillState(bool displayMessage = true)
    {
        StopAllCoroutines();

        skillActive = false;
        playerState.SetStanceBarValue(1);

        // Общее
        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);
        ActivateStanceAnimation(false);
        playerComponents.ActivateRig(true);
        StopStanceSFX();
        BlockStance(false);

        // ATTACK: выпад
        EnableThrustVFX(false);

        // DEFENSE: щит
        EnableShieldVFX(false);
        playerHealth.SetReflectStance(false);

        // DEXTERITY: стамина
        playerStamina.BlockStaminaConsumage(false);
        playerHealth.SetAvoidence(false);
        playerMovement.OverallMoveSpeedMult = 1f;

        if (displayMessage)
            Debug.Log("<color=#0000FF>[PLAYER SKILL]</color> Skill State Reset");
    }

    [Rpc(SendTo.Owner)]
    public void ResetSkillState_OwnerRpc()
    {
        ResetSkillState();
    }

    #endregion

    public void BlockStance(bool block)
    {
        blockSkill = block;
    }

    private void Skill_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (skillActive || skillCooldown || blockSkill || playerHealth.Healing)
            return;

        ActivateStanceSkill();
    }

    public Sprite GetStanceSprite()
    {
        StanceInfo stanceInfo = GetStanceByType(currentStance.Type);

        if (stanceInfo == null)
            return null;

        return stanceInfo.StanceIcon;
    }

    private void ActivateStanceAnimation(bool active)
    {
        animator.SetBool("Skill Active", active);
        animator.SetInteger("Skill Index", currentStance.Index);
    }

    private void ActivateStanceSkill()
    {
        switch (currentStance.Type)
        {
            case StanceType.Attack:
                StartCoroutine(DoThrustAttack());
                break;

            case StanceType.Defense:
                StartCoroutine(DoReflectAction());
                break;

            case StanceType.Dexterity:
                StartCoroutine(DoInfiniteStamina());
                break;

            default:
                break;
        }
    }

    #region General Stance Methods

    private IEnumerator SkillCooldown(float cooldown)
    {
        skillCooldown = true;
        playerState.DoStanceBarAnimation(1, cooldown);

        yield return new WaitForSeconds(cooldown);
        skillCooldown = false;

        PlayReloadStanceVFX();
        playerState.DoStanceBarEffect();    
    }

    #endregion

    private IEnumerator DoThrustAttack()
    {
        if (!playerMovement.OnGround || playerMovement.MoveInput == Vector2.zero)
        {
            yield break;
        }

        skillActive = true;
        ActivateStanceAnimation(true);
        playerComponents.ActivateRig(false);
        playerMovement.BlockMovement(true);
        playerMovement.BlockTurn(true);

        PlayStanceSFX(StanceType.Attack);

        yield return new WaitForSeconds(thrustDelay);

        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);

        playerState.DoStanceBarAnimation(0, currentStance.Duration);

        thrustAttackCollider.SetCollider(true);
        EnableThrustVFX(true);
        playerMovement.Thrust(thrustDistance, currentStance.Duration, 0.25f);

        yield return new WaitForSeconds(currentStance.Duration);

        playerComponents.ActivateRig(true);
        EnableThrustVFX(false, true);
        thrustAttackCollider.SetCollider(false);

        skillActive = false;
        ActivateStanceAnimation(false);
        StartCoroutine(SkillCooldown(currentStance.Cooldown));
    }

    private IEnumerator DoReflectAction()
    {
        if (!playerMovement.OnGround)
        {
            yield break;
        }

        skillActive = true;
        ActivateStanceAnimation(true);
        playerMovement.BlockMovement(true);
        playerMovement.BlockTurn(true);

        EnableShieldVFX(true);
        PlayStanceSFX(StanceType.Defense);

        yield return new WaitForSeconds(defenseStanceDelay);

        ActivateStanceAnimation(false);

        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);
        playerState.DoStanceBarAnimation(0, currentStance.Duration);
        playerHealth.SetReflectStance(true, cutDamageMult);

        yield return new WaitForSeconds(currentStance.Duration);

        playerHealth.SetReflectStance(false);

        EnableShieldVFX(false);
        StopStanceSFX();
        
        StartCoroutine(SkillCooldown(currentStance.Cooldown));
        skillActive = false;
    }

    private IEnumerator DoInfiniteStamina()
    {
        if (!playerMovement.OnGround)
        {
            yield break;
        }

        skillActive = true;
        ActivateStanceAnimation(true);
        playerMovement.BlockMovement(true);
        playerMovement.BlockTurn(true);

        PlayStanceSFX(StanceType.Dexterity);
        PlayStaminaBurstVFX();

        yield return new WaitForSeconds(staminaStanceDelay);

        ActivateStanceAnimation(false);
        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);

        playerState.DoStanceBarAnimation(0, currentStance.Duration);
        playerStamina.BlockStaminaConsumage(true);
        playerHealth.SetAvoidence(true, 0.5f);
        playerMovement.OverallMoveSpeedMult = 1.25f;

        yield return new WaitForSeconds(currentStance.Duration);

        playerStamina.BlockStaminaConsumage(false);
        playerHealth.SetAvoidence(false);
        playerMovement.OverallMoveSpeedMult = 1f;

        skillActive = false;
        StartCoroutine(SkillCooldown(currentStance.Cooldown));
    }

    private void OnThrustHit(EntityHealth enemy, HitTransform transform)
    {
        float enemyDamageTaken = enemy.TakeDamage(playerAttack.AttackDamage.GetMultDamage(thrustAttackMult), GetComponent<PlayerHealth>());
        enemy.CreateHitEffect(transform);

        playerAttack.TryVampireHeal(enemyDamageTaken);
    }

    #region Effects and Visuals

    private void PlayStanceSFX(StanceType stanceType)
    {
        sfxController.PlayStanceSFX(stanceType);
    }

    private void StopStanceSFX()
    {
        sfxController.StopStanceSFX();
    }

    #region Thrust VFX

    private void EnableThrustVFX(bool active, bool clear = false)
    {
        ExecuteEnableThrustVFX(active, clear);
        EnableThrustVFX_ToServerRpc(active, clear);
    }

    private void ExecuteEnableThrustVFX(bool active, bool clear = false)
    {
        thrustAttackCollider.SlashEffectActive(active, clear);
    }

    [Rpc(SendTo.Server)]
    private void EnableThrustVFX_ToServerRpc(bool active, bool clear = false)
    {
        EnableThrustVFX_ToEveryoneRpc(active, clear);
    }

    [Rpc(SendTo.NotOwner)]
    private void EnableThrustVFX_ToEveryoneRpc(bool active, bool clear = false)
    {
        thrustAttackCollider.SlashEffectActive(active, clear);
    }

    #endregion

    #region Shield VFX

    private void EnableShieldVFX(bool enable)
    {
        ExecuteEnableShieldVFX(enable);
        EnableShieldVFX_ToServerRpc(enable);
    }

    private void ExecuteEnableShieldVFX(bool enable)
    {
        if (enable)
        {
            shieldVFX.Play();
        }
        else
        {
            shieldVFX.Stop();
            shieldVFX.Clear(true);
        }
    }

    [Rpc(SendTo.Server)]
    private void EnableShieldVFX_ToServerRpc(bool enable)
    {
        EnableShieldVFX_ToEveryoneRpc(enable);
    }

    [Rpc(SendTo.NotOwner)]
    private void EnableShieldVFX_ToEveryoneRpc(bool enable)
    {
        ExecuteEnableShieldVFX(enable);
    }

    #endregion

    #region Stamina Burst VFX

    private void PlayStaminaBurstVFX()
    {
        ExecutePlayStaminaBurstVFX();
        PlayStaminaBurstVFX_ToServerRpc();
    }

    private void ExecutePlayStaminaBurstVFX()
    {
        playerStamina.PlayBurstVFX();
    }

    [Rpc(SendTo.Server)]
    private void PlayStaminaBurstVFX_ToServerRpc()
    {
        PlayStaminaBurstVFX_ToEveryoneRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayStaminaBurstVFX_ToEveryoneRpc()
    {
        ExecutePlayStaminaBurstVFX();
    }

    #endregion

    #region Reload Stance VFX

    private void PlayReloadStanceVFX()
    {
        ExecutePlayReloadStanceVFX();
        PlayReloadStanceVFX_ToServerRpc();
    }

    private void ExecutePlayReloadStanceVFX()
    {
        reloadStanceVFX.Play();
    }

    [Rpc(SendTo.Server)]
    private void PlayReloadStanceVFX_ToServerRpc()
    {
        PlayReloadStanceVFX_ToEveryoneRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayReloadStanceVFX_ToEveryoneRpc()
    {
        ExecutePlayReloadStanceVFX();
    }

    #endregion

    #endregion
}
