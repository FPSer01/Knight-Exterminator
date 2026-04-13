using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class PlayerStanceBase : NetworkBehaviour
{
    protected readonly static string DEBUG_TAG = $"[{LogTags.BLUE_COLOR}Player Stance{LogTags.END_COLOR}]";

    [Header("Base Settings")]
    [SerializeField] protected List<StanceInfo> avalableStances;
    protected float stanceDamageMult = 1f;

    [Tooltip("Debug Stuff")]
    [SerializeField] protected bool debug = false;
    [SerializeField] protected StanceType startStance;
    [SerializeField] protected Stance currentStance;

    [Header("Base VFX")]
    [SerializeField] protected ParticleSystem reloadStanceVFX;

    [Header("Components")]
    [SerializeField] protected PlayerComponents playerComponents;

    protected PlayerStamina playerStamina => playerComponents.Stamina;
    protected PlayerMovement playerMovement => playerComponents.Movement;
    protected PlayerAttackBase playerAttack => playerComponents.Attack;
    protected PlayerHealth playerHealth => playerComponents.Health;
    protected Animator animator => playerComponents.Animator;
    protected PlayerSFXController sfxController => playerComponents.SfxController;
    protected PlayerStateUI playerState => playerComponents.UI.PlayerStateUI;

    public Stance CurrentStance { get => currentStance; set => currentStance = value; }
    public virtual float StanceDamageMult { get => stanceDamageMult; set => stanceDamageMult = value; }

    protected bool skillActive = false;
    protected bool skillCooldown = false;
    protected bool blockSkill = false;

    protected PlayerNetworkObject boundPlayer;

    public event Action<StanceType> OnStanceSet;
    protected void DoOnStanceSet(StanceType stance) => OnStanceSet?.Invoke(stance);

    [HideInInspector] public bool IsInitialized = false;

    protected virtual void Awake()
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

        IsInitialized = true;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        InputManager.Input.Player.Skill.started -= Skill_started;

        if (StartGameData.GameMode == Gamemode.Multiplayer)
            UnboundPlayer();
    }

    private void Skill_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (skillActive || skillCooldown || blockSkill || playerHealth.Healing)
            return;

        ActivateStanceSkill();
    }

    #endregion

    #region Bound Player

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
        SetStance_NotOwnerRpc(boundPlayer.Stance.Value);
    }

    public void UnboundPlayer()
    {
        if (boundPlayer == null)
            return;

        boundPlayer.Stance.OnValueChanged -= OnStanceChanged;
        boundPlayer = null;
    }

    private void OnStanceChanged(StanceType previousValue, StanceType newValue)
    {
        ExecuteSetStance(newValue);
        SetStance_NotOwnerRpc(newValue);
    }

    #endregion

    #region Setup Stance

    [Rpc(SendTo.NotOwner)]
    private void SetStance_NotOwnerRpc(StanceType type)
    {
        ExecuteSetStance(type);
    }

    protected StanceInfo GetStanceByType(StanceType type)
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

    public virtual void ExecuteSetStance(StanceType type)
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
        DoOnStanceSet(type);
    }

    public virtual void ResetSkillState(bool displayMessage = true)
    {
        StopAllCoroutines();

        skillActive = false;
        skillCooldown = false;
        playerState.SetStanceBarValue(1);

        // Общее
        playerMovement.BlockMovement(false);
        playerMovement.BlockTurn(false);
        ActivateStanceAnimation(false);
        playerComponents.ActivateRig(true);
        BlockStance(false);

        if (displayMessage)
            Debug.Log($"{DEBUG_TAG} Skill State Reset");
    }

    [Rpc(SendTo.Owner)]
    public void ResetSkillState_OwnerRpc()
    {
        ResetSkillState();
    }

    #endregion

    #region General Stance Methods

    protected IEnumerator SkillCooldown(float cooldown)
    {
        skillCooldown = true;
        playerState.DoStanceBarAnimation(1, cooldown);

        yield return new WaitForSeconds(cooldown);
        skillCooldown = false;

        PlayReloadStanceVFX();
        playerState.DoStanceBarEffect();
    }

    public void BlockStance(bool block)
    {
        blockSkill = block;
    }

    public Sprite GetStanceSprite()
    {
        StanceInfo stanceInfo = GetStanceByType(currentStance.Type);

        if (stanceInfo == null)
            return null;

        return stanceInfo.StanceIcon;
    }

    protected void ActivateStanceAnimation(bool active)
    {
        animator.SetBool("Skill Active", active);
        animator.SetInteger("Skill Index", currentStance.Index);
    }

    protected virtual void ActivateStanceSkill()
    {

    }

    #endregion

    #region Effects and Visuals

    protected void PlayStanceSFX(StanceType stanceType)
    {
        sfxController.PlayStanceSFX(stanceType);
    }

    protected void StopStanceSFX()
    {
        sfxController.StopStanceSFX();
    }

    #region Reload Stance VFX

    protected void PlayReloadStanceVFX()
    {
        ExecutePlayReloadStanceVFX();
        PlayReloadStanceVFX_ToServerRpc();
    }

    protected void ExecutePlayReloadStanceVFX()
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
