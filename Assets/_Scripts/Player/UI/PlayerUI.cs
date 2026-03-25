using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerUI : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerComponents playerComponents;

    [Header("Special")]
    [SerializeField] private PlayerStateUI playerStateUI;
    [SerializeField] private BlurEffect blurEffect;
    [SerializeField] private ItemInfoWindow itemInfoWindow;
    [SerializeField] private PlayerMiniMap miniMap;
    [SerializeField] private LevelNameUI levelNameUI;
    [SerializeField] private HurtEffect hurtEffect;
    [SerializeField] private PlayerUsernameUI usernameUI;
    [SerializeField] private BossHealthUI bossHealthUI;

    [Header("Windows")]
    [SerializeField] private PlayerUIWindow hud;
    [SerializeField] private LevelUpWindow levelUpWindow;
    [SerializeField] private PlayerMenuWindow menuWindow;
    [SerializeField] private PlayerSettingsWindow settingsWindow;
    [SerializeField] private PlayerInventoryWindow inventoryWindow;
    [SerializeField] private GameOverWindow gameOverWindow;
    [SerializeField] private MerchantWindow merchantWindow;
    [SerializeField] private PlayerMapWindow mapWindow;
    [SerializeField] private PlayerReviveWindow reviveWindow;
    [SerializeField] private SpectatorWindow spectatorWindow;

    private GameUIWindowType currentWindowType;
    private static bool blockMap;
    private bool enableDebugCursor = false;

    public static event Action<GameUIWindowType> OnUIChange;

    [HideInInspector] public bool DragActive = false;

    public static bool BlockMap { get => blockMap; set => blockMap = value; }

    private bool IsDead => playerComponents.Health.IsDead;

    #region UI Public Fields

    public PlayerUIWindow Hud { get => hud;  }
    public LevelUpWindow LevelUpWindow { get => levelUpWindow; }
    public PlayerMenuWindow MenuWindow { get => menuWindow; }
    public PlayerSettingsWindow SettingsWindow { get => settingsWindow; }
    public PlayerInventoryWindow InventoryWindow { get => inventoryWindow; }
    public GameOverWindow GameOverWindow { get => gameOverWindow; }
    public MerchantWindow MerchantWindow { get => merchantWindow; }
    public LevelNameUI LevelNameUI { get => levelNameUI; }
    public PlayerStateUI PlayerStateUI { get => playerStateUI; }
    public HurtEffect HurtEffect { get => hurtEffect; }
    public SpectatorWindow SpectatorWindow { get => spectatorWindow; }
    public PlayerUsernameUI UsernameUI { get => usernameUI; }
    public BossHealthUI BossHealthUI { get => bossHealthUI; }

    #endregion

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        SetWindow(GameUIWindowType.HUD);
        SubToEvents(true);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        SubToEvents(false);
    }

    private void SubToEvents(bool subscribe)
    {
        if (subscribe)
        {
            InputManager.Input.UI.Pause.started += PauseInput;
            InputManager.Input.UI.Inventory.started += InventoryInput;
            InputManager.Input.UI.Map.started += MapInput;
            InputManager.Input.UI.HideHUD.started += HideHUDInput;

            InputManager.Input.UI.DebugCursor.started += DebugCursorInput;
        }
        else
        {
            InputManager.Input.UI.Pause.started -= PauseInput;
            InputManager.Input.UI.Inventory.started -= InventoryInput;
            InputManager.Input.UI.Map.started -= MapInput;
            InputManager.Input.UI.HideHUD.started -= HideHUDInput;

            InputManager.Input.UI.DebugCursor.started -= DebugCursorInput;
        }
    }

    #region Inputs

    private void DebugCursorInput(InputAction.CallbackContext context)
    {
#if UNITY_EDITOR
        enableDebugCursor = !enableDebugCursor;
        KE.CameraController.LockCursor(!enableDebugCursor);
#endif
    }

    private void HideHUDInput(InputAction.CallbackContext context)
    {
        if (!CanHideHUD())
            return;

        if (currentWindowType == GameUIWindowType.HUD)
            hud.gameObject.SetActive(!hud.gameObject.activeSelf);
        else if (currentWindowType == GameUIWindowType.Spectator)
            spectatorWindow.gameObject.SetActive(!spectatorWindow.gameObject.activeSelf);
    }

    private void MapInput(InputAction.CallbackContext context)
    {
        if (!CanEnterMapUI())
            return;

        if (currentWindowType != GameUIWindowType.HUD)
            SetWindow(GameUIWindowType.HUD);
        else
            SetWindow(GameUIWindowType.Map);
    }

    private void InventoryInput(InputAction.CallbackContext context)
    {
        if (!CanEnterInventoryUI())
            return;

        if (currentWindowType != GameUIWindowType.HUD)
            SetWindow(GameUIWindowType.HUD);
        else
            SetWindow(GameUIWindowType.Inventory);
    }

    private void PauseInput(InputAction.CallbackContext obj)
    {
        if (!CanEnterPauseUI())
            return;

        if (currentWindowType != GameUIWindowType.HUD && !IsDead)
            SetWindow(GameUIWindowType.HUD);
        else if (currentWindowType != GameUIWindowType.Spectator && IsDead)
            SetWindow(GameUIWindowType.Spectator);
        else
            SetWindow(GameUIWindowType.Menu);
    }

    #endregion

    #region Input Checks

    private bool CanHideHUD()
    {
        return currentWindowType == GameUIWindowType.HUD 
            || currentWindowType == GameUIWindowType.Spectator
            && !IsDead;
    }

    private bool CanEnterMapUI()
    {
        return currentWindowType != GameUIWindowType.Menu 
            && currentWindowType != GameUIWindowType.GameOver
            && !blockMap 
            && !IsDead;
    }

    private bool CanEnterInventoryUI()
    {
        return currentWindowType != GameUIWindowType.Menu
            && currentWindowType != GameUIWindowType.GameOver
            && currentWindowType != GameUIWindowType.Revive
            && !IsDead;
    }

    private bool CanEnterPauseUI()
    {
        return currentWindowType != GameUIWindowType.GameOver
            && currentWindowType != GameUIWindowType.Revive;
    }

    #endregion

    public void SetWindow(GameUIWindowType type)
    {
        if (currentWindowType == type)
            return;

        currentWindowType = type;

        bool lockCursor = type == GameUIWindowType.HUD;
        KE.CameraController.LockCursor(lockCursor && !enableDebugCursor);

        if (lockCursor && !IsDead)
            InputManager.Input.Player.Enable();
        else
            InputManager.Input.Player.Disable();

        blurEffect.SetBlur(
            type != GameUIWindowType.HUD 
            && type != GameUIWindowType.Map
            && type != GameUIWindowType.Spectator
            );

        SetGamePause(
            (type == GameUIWindowType.Menu || type == GameUIWindowType.Settings) 
            && StartGameData.GameMode == Gamemode.Singleplayer
            );

        ChangeWindow(type);

        OnUIChange?.Invoke(type);
    }

    private void ChangeWindow(GameUIWindowType type)
    {
        hud.SetWindowActive(type == GameUIWindowType.HUD);
        levelUpWindow.SetWindowActive(type == GameUIWindowType.LevelUp);
        menuWindow.SetWindowActive(type == GameUIWindowType.Menu);
        settingsWindow.SetWindowActive(type == GameUIWindowType.Settings);
        inventoryWindow.SetWindowActive(type == GameUIWindowType.Inventory);
        gameOverWindow.SetWindowActive(type == GameUIWindowType.GameOver);
        merchantWindow.SetWindowActive(type == GameUIWindowType.Merchant);
        mapWindow.SetWindowActive(type == GameUIWindowType.Map);
        reviveWindow.SetWindowActive(type == GameUIWindowType.Revive);
        spectatorWindow.SetWindowActive(type == GameUIWindowType.Spectator);
    }

    [Rpc(SendTo.Owner)]
    public void SetWindow_OwnerRpc(GameUIWindowType type)
    {
        SetWindow(type);
    }

    private void SetGamePause(bool pause)
    {
        Time.timeScale = pause ? 0 : 1;
    }

    public void SetInfoWindow(bool active, UpgradeItem item)
    {
        itemInfoWindow.SetWindowActive(active);
        itemInfoWindow.SetInfo(item);
    }

    [Rpc(SendTo.Owner)]
    public void ResetUI_OwnerRpc()
    {
        hud.gameObject.SetActive(true);
        spectatorWindow.gameObject.SetActive(true);

        ExecuteSetMiniMapVisible(true);
        SetWindow(GameUIWindowType.HUD);
    }

    #region Mini Map

    public void SetMiniMapVisible(bool visible)
    {
        SetMiniMapVisible_OwnerRpc(visible);
    }

    [Rpc(SendTo.Owner)]
    private void SetMiniMapVisible_OwnerRpc(bool visible)
    {
        ExecuteSetMiniMapVisible(visible);
    }

    private void ExecuteSetMiniMapVisible(bool visible)
    {
        miniMap.SetActive(visible);
    }

    #endregion

    #region End Game UI

    public void SetGameOverStatus(GameOverStatus status)
    {
        gameOverWindow.SetGameOverStatus(status);
    }

    [Rpc(SendTo.Owner)]
    public void SetGameWinUI_OwnerRpc()
    {
        SetGameOverStatus(GameOverStatus.Victory);
        SetWindow(GameUIWindowType.GameOver);
    }

    [Rpc(SendTo.Owner)]
    public void SetGameLoseUI_OwnerRpc()
    {
        SetGameOverStatus(GameOverStatus.Defeat);
        SetWindow(GameUIWindowType.GameOver);
    }

    #endregion
}
