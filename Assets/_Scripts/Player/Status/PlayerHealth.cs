using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerHealth : EntityHealth
{  
    [Header("UI")]
    [SerializeField] private float gameOverDelay;
    [SerializeField] private TMP_Text healAmountText;

    [Header("Heal")]
    [SerializeField] private int healAmount = 1;
    private int currentHealAmount;
    [SerializeField] private float healSpeedMult = 1f;
    [SerializeField] private GameObject healObjectModel;
    [Space]
    [SerializeField] private float healVFXTime;
    [SerializeField] private ParticleSystem healingVFX;

    [Header("Revive")]
    [SerializeField] private int reviveAmount;
    [SerializeField] private int currentReviveAmount;
    [SerializeField] private float reviveInvincibilityTime;
    [SerializeField] private LayerMask deathIgnoreMask;

    [Header("Components")]
    [SerializeField] private PlayerComponents playerComponents;

    private PlayerUI playerUI => playerComponents.UI;
    private Collider playerCollider => playerComponents.CapsuleCollider;
    private PlayerMovement playerMovement => playerComponents.Movement;
    private PlayerAttackBase playerAttack => playerComponents.Attack;
    private PlayerStanceBase playerStance => playerComponents.Stance;
    private Animator animator => playerComponents.Animator;
    private PlayerSFXController sfxController => playerComponents.SfxController;
    private HurtEffect hurtEffect => playerComponents.UI.HurtEffect;

    private bool reflectActive = false;
    private float cutDamageMult;
    private bool avoidActive = false;
    private float avoidChance;

    private bool canTakeDamage = true;
    private bool stunned = false;
    private bool slowed = false;
    private bool healing = false;

    private bool infiniteHealthCheatEnabled = false;

    private IEnumerator invincibilityCoroutine;
 
    public bool Stunned { get => stunned; }
    public float HealSpeedMult { get => healSpeedMult; set => healSpeedMult = value; }
    public int HealAmount { get => healAmount; }
    public int CurrentHealAmount { get => currentHealAmount; }
    public bool Healing { get => healing; }
    public int CurrentReviveAmount { get => currentReviveAmount; }
    public bool EnableInfiniteHealth { get => infiniteHealthCheatEnabled; set => infiniteHealthCheatEnabled = value; }

    private Coroutine healVFXCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
            return;

        OnDeath += Player_OnDeath;

        currentHealAmount = healAmount;
        UpdateHealsAmountText();

        ResetReviveCount();
        SetHealPotionVisibility(false);

        UpdateHealthUI();

        InputManager.Input.Player.Heal.started += HealInput;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsOwner)
            return;

        OnDeath -= Player_OnDeath;

        InputManager.Input.Player.Heal.started -= HealInput;
    }

    #region Input

    private void HealInput(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (currentHealAmount == 0 || healing || animator.GetBool("Attacking") || playerMovement.Dodging)
            return;

        animator.SetTrigger("Heal");
    }

    #endregion

    #region Heal Animation Callbacks

    /// <summary>
    /// В начале анимации
    /// </summary>
    private void StartHealAnimation()
    {
        healing = true;
        playerMovement.BlockDodging(true);
        SetHealPotionVisibility(true);
    }

    /// <summary>
    /// В конце анимации
    /// </summary>
    private void EndHealAnimation()
    {
        healing = false;
        playerMovement.BlockDodging(false);
        SetHealPotionVisibility(false);
    }

    /// <summary>
    /// Во время анимации по достижению определенного момента
    /// </summary>
    private void HealFull()
    {
        if (healVFXCoroutine != null)
        {
            StopCoroutine(healVFXCoroutine);
            healVFXCoroutine = null;
        }

        healVFXCoroutine = StartCoroutine(DoHealVFX(healVFXTime));
        sfxController.PlayHealSFX();

        Heal(maxHealth);
        currentHealAmount--;
        UpdateHealsAmountText();
    }

    private IEnumerator DoHealVFX(float time)
    {
        healingVFX.Play();

        yield return new WaitForSeconds(time);

        healingVFX.Stop();
    }

    private void SetHealPotionVisibility(bool visible)
    {
        ExecuteSetHealPotionVisibility(visible);
        SetHealPotionVisibility_ToServerRpc(visible);
    }

    private void ExecuteSetHealPotionVisibility(bool visible)
    {
        healObjectModel.SetActive(visible);
    }

    [Rpc(SendTo.Server)]
    private void SetHealPotionVisibility_ToServerRpc(bool visible)
    {
        SetHealPotionVisibility_ToEveryoneRpc(visible);
    }

    [Rpc(SendTo.NotOwner)]
    private void SetHealPotionVisibility_ToEveryoneRpc(bool visible)
    {
        ExecuteSetHealPotionVisibility(visible);
    }

    #endregion

    #region Heal

    public override void Heal(float healAmount)
    {
        if (IsDead || !IsOwner)
            return;

        healAmount = MathF.Round(healAmount, 2);
        ChangeClientSideHealth(clientSideHealth + healAmount);
    }

    [Rpc(SendTo.Owner)]
    public void ChangeHealsAmount_OwnerRpc(int amountToAdd)
    {
        int oldAmount = healAmount;
        int newAmount = healAmount + amountToAdd;
        int difference = newAmount - oldAmount;

        healAmount = newAmount;
        currentHealAmount += difference;

        UpdateHealsAmountText();
    }

    /// <summary>
    /// Восстановить количество хилок игроку
    /// </summary>
    /// <param name="amount">Сколько хилок восстановить</param>
    /// <returns></returns>
    public bool RefillHeals(int amount)
    {
        if (currentHealAmount == healAmount)
            return false;

        currentHealAmount = Mathf.Clamp(currentHealAmount + amount, 0, healAmount);
        UpdateHealsAmountText();

        return true;
    }

    private void UpdateHealsAmountText()
    {
        healAmountText.text = $"{currentHealAmount} / {healAmount}";
    }

    #endregion

    #region Take Damage

    private bool CheckForTakeDamageIgnore()
    {
#if UNITY_EDITOR || DEBUG
        if (infiniteHealthCheatEnabled)
        {
            return true;
        }
#endif
        return IsDead || playerMovement.Dodging || !canTakeDamage || CheckAvoidence() || !IsOwner;
    }

    public override float TakeDamage(AttackDamageType damage, EntityHealth sender)
    {
        if (CheckForTakeDamageIgnore())
            return 0f;

        float finalDamage = MathF.Round(GetFinalDamage(damage), 2);

        if (StartGameData.GameMode == Gamemode.Singleplayer)
        {
            finalDamage *= EnemyManager.Instance.DamageMult;
        }

        // Проверка на отражение (шипы, типа)
        if (reflectActive && sender != null)
        {
            finalDamage *= 1f - cutDamageMult;
            ReflectDamage(sender, damage);
        }

        if (finalDamage > 0f && !damage.DisableHurtEffect && !reflectActive)
        {
            PlayHurtSFX();
            hurtEffect.ActivateHurtEffect();
        }

        // Наносим урон
        DoOnDamageTaken(finalDamage);

        // Наносим статус значения
        var statusData = GetFinalStatus(damage.StatusData);
        DoOnStatusTaken(statusData);

        ChangeClientSideHealth(clientSideHealth - finalDamage);
        CheckForDeath_ClientSide();

        // Неуязвимость на несколько фреймов физики
        invincibilityCoroutine = SetInvincibility_FixedFrames(3);
        StartCoroutine(invincibilityCoroutine);

        return finalDamage;
    }

    private void Player_OnDeath()
    {
        playerComponents.AddExcludeLayers(deathIgnoreMask);

        playerComponents.ActivateRig(false);
        EndHealAnimation();

        if (IsOwner)
            InputManager.Input.Player.Disable();

        animator.SetBool("Death", true);
        PlayDeathSFX();

        DecideAfterDeath();
    }

    private void DecideAfterDeath()
    {
        switch (StartGameData.GameMode)
        {
            case Gamemode.Singleplayer:

                if (currentReviveAmount > 0 && !StartGameData.SingleplayerOneLife)
                {
                    StartCoroutine(SummonReviveWindow(gameOverDelay));
                    Debug.Log("Singleplayer Death Action: Revive");
                }
                else
                {
                    StartCoroutine(DecideAfterDeathAction(gameOverDelay));
                }
                break;

            case Gamemode.Multiplayer:
                StartCoroutine(DecideAfterDeathAction(gameOverDelay));
                break;
        }
    }

    #endregion

    #region SFX

    private void PlayHurtSFX()
    {
        sfxController.PlayHurtSFX();
    }

    private void PlayDeathSFX()
    {
        sfxController.PlayDeathSFX();
    }

    #endregion

    #region Revive

    public void RevivePlayer(bool reviveWithIFrames = true)
    {
        SetDeathStatus_ServerRpc(false);
        playerComponents.RemoveExcludeLayers(deathIgnoreMask);

        Heal(maxHealth);
        currentReviveAmount--;

        playerComponents.ActivateRig(true);
        animator.SetBool("Death", false);

        if (IsOwner)
            InputManager.Input.Player.Enable();

        if (reviveWithIFrames)
        {
            StopInvincibility();
            invincibilityCoroutine = SetInvincibility_Time(reviveInvincibilityTime);
            StartCoroutine(invincibilityCoroutine);
        }

        if (StartGameData.GameMode == Gamemode.Multiplayer)
        {
            SpectatorManager.Instance.ExitSpectatorMode();
        }
    }

    private IEnumerator SetInvincibility_Time(float time)
    {
        canTakeDamage = false;

        yield return new WaitForSeconds(time);

        canTakeDamage = true;
    }

    private IEnumerator SetInvincibility_FixedFrames(int fixedFramesAmount)
    {
        canTakeDamage = false;
        int framesCount = 0;

        while (framesCount < fixedFramesAmount)
        {
            yield return new WaitForFixedUpdate();
            framesCount++;
        }

        canTakeDamage = true;
    }

    private void StopInvincibility()
    {
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = null;
        }
    }

    public void ResetReviveCount()
    {
        currentReviveAmount = reviveAmount;
    }

    public void SetMaxReviveCount(int value)
    {
        reviveAmount = value;
        currentReviveAmount = reviveAmount;
    }

    #endregion

    #region Utility

    protected override void UpdateHealthUI()
    {
        playerUI.PlayerStateUI.SetHealthBarValue(clientSideHealth / maxHealth);
    }

    public override void CreateHitEffect(HitTransform hitTransform)
    {
        if (playerMovement.Dodging)
            return;

        Vector3 newPosition = playerCollider.ClosestPoint(hitTransform.Position);

        GameObject hitEffect = Instantiate(hitEffectPrefab, newPosition, hitTransform.Rotation);
        hitEffect.transform.localScale = hitEffectScale;
    }

    private bool CheckAvoidence()
    {
        if (!avoidActive)
            return false;

        float randomValue = Random.value;

        return randomValue > 0 && randomValue <= avoidChance;
    }

    public void SetAvoidence(bool active, float avoidChance = 0f)
    {
        avoidActive = active;
        this.avoidChance = avoidChance;
    }

    public void SetReflectStance(bool active, float cutDamageMult = 0f)
    {
        reflectActive = active;
        this.cutDamageMult = cutDamageMult;
    }

    private void ReflectDamage(EntityHealth target, AttackDamageType damage)
    {
        AttackDamageType reflectDamage = damage.GetMultDamage(1f, true);

        target.TakeDamage(reflectDamage, null);

        Transform targetTransform = target.transform;

        target.CreateHitEffect(
            new HitTransform(
                targetTransform.position,
                Quaternion.LookRotation(targetTransform.position - transform.position, Vector3.up) * Quaternion.Euler(new Vector3(0, 180, 0)))
            );
    }

    #endregion

    #region Status Stuff

    public override void SetActiveStatus(StatusType type, StatusInflictData data, bool active)
    {
        if (!IsOwner)
            return;

        switch (type)
        {
            case StatusType.Bloodloss:
                if (active)
                    bloodlossCoroutine = StartCoroutine(TickStatusEffect(BLOODLOSS_DAMAGE));
                else if (bloodlossCoroutine != null)
                    StopCoroutine(bloodlossCoroutine);
                break;

            case StatusType.Poison:
                if (active)
                    poisonCoroutine = StartCoroutine(TickStatusEffect(POISON_DAMAGE));
                else if (poisonCoroutine != null)
                    StopCoroutine(poisonCoroutine);
                break;

            case StatusType.Slowness:
                if (active)
                    SetSlownessEffect(true, data.SlownessIntensity);
                else
                    SetSlownessEffect(false, data.SlownessIntensity);
                break;

            case StatusType.Stun:
                if (active)
                    SetStunEffect(true);
                else
                    SetStunEffect(false);
                break;
        }
    }

    private void SetSlownessEffect(bool active, float slowMult = 0)
    {
        if (slowed == active)
            return;

        if (active)
        {
            playerMovement.OverallMoveSpeedMult -= slowMult;
        }
        else
        {
            playerMovement.OverallMoveSpeedMult += slowMult;
        }

        slowed = active;
    }

    #endregion

    #region Stun

    private void SetStunEffect(bool active)
    {
        stunned = active;

        playerComponents.ActivateRig(!stunned);
        animator.SetBool("Stunned", stunned);

        playerMovement.BlockMovement(stunned);
        playerMovement.BlockTurn(stunned);
        playerAttack.BlockAttack(stunned);
        playerStance.BlockStance(stunned);

        EnableStunVFX(stunned);
    }

    private void EnableStunVFX(bool enable)
    {
        ExecuteEnableStunVFX(enable);
        EnableStunVFX_ToServerRpc(enable);
    }

    private void ExecuteEnableStunVFX(bool enable)
    {
        if (enable)
        {
            stunEffect.Play();
        }
        else
        {
            stunEffect.Stop();
            stunEffect.Clear();
        }
    }

    [Rpc(SendTo.Server)]
    private void EnableStunVFX_ToServerRpc(bool enable)
    {
        EnableStunVFX_EveryoneRpc(enable);
    }

    [Rpc(SendTo.NotOwner)]
    private void EnableStunVFX_EveryoneRpc(bool enable)
    {
        ExecuteEnableStunVFX(enable);
    }

    #endregion

    #region Support Methods

    private IEnumerator DecideAfterDeathAction(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (StartGameData.GameMode == Gamemode.Singleplayer)
        {
            playerUI.SetGameLoseUI_OwnerRpc();
            Debug.Log("Singleplayer Death Action: Gameover");
        }
        else if (StartGameData.GameMode == Gamemode.Multiplayer)
        {
            SpectatorManager.Instance.TryEnterSpectatorMode();
            Debug.Log("Multiplayer Death Action: Spectator");
        }
    }

    private IEnumerator SummonReviveWindow(float delay)
    {
        yield return new WaitForSeconds(delay);

        playerUI.SetWindow(GameUIWindowType.Revive);
    }

    [Rpc(SendTo.Owner)]
    public void ResetAll_OwnerRpc()
    {
        EndHealAnimation();

        RefillHeals(healAmount);

        if (IsDead)
        {
            RevivePlayer(false);
        }

        currentReviveAmount = reviveAmount;
    }

    #endregion
}