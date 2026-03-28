using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerCheats : NetworkBehaviour
{
    private readonly static string CHEATS_DEBUG = $"[{LogTags.BLUE_COLOR}Debug Cheats{LogTags.END_COLOR}]";

    private readonly static string ENABLED = $"{LogTags.GREEN_COLOR}Enabled{LogTags.END_COLOR}";
    private readonly static string DISABLED = $"{LogTags.RED_COLOR}Disabled{LogTags.END_COLOR}";

    [SerializeField] private TMP_Text cheatsStatus;
    [SerializeField] private PlayerComponents playerComponents;

    private bool InfiniteHealth 
    {
        get => playerComponents.Health.EnableInfiniteHealth; 
        set => playerComponents.Health.EnableInfiniteHealth = value; 
    }

    private bool InfiniteDamage
    {
        get => playerComponents.Attack.EnableInfiniteDamage;
        set => playerComponents.Attack.EnableInfiniteDamage = value;
    }

    public override void OnNetworkSpawn()
    {
#if UNITY_EDITOR || DEBUG
        if (!IsOwner || playerComponents == null)
            return;

        SubToCheatsInputs(true);
        EnableInfiniteHealth(false);
        EnableInfiniteDamage(false);
#endif
    }

    public override void OnNetworkDespawn()
    {
#if UNITY_EDITOR || DEBUG
        if (!IsOwner || playerComponents == null)
            return;

        SubToCheatsInputs(false);
#endif
    }

    private void SubToCheatsInputs(bool sub)
    {
        if (sub)
        {
            InputManager.Input.Debug.InfiniteHealth.started += InfiniteHealth_Input;
            InputManager.Input.Debug.InfiniteDamage.started += InfiniteDamage_Input;
            InputManager.Input.Debug.InfiniteStamina.started += InfiniteStamina_Input;
        }
        else
        {
            InputManager.Input.Debug.InfiniteHealth.started -= InfiniteHealth_Input;
            InputManager.Input.Debug.InfiniteDamage.started -= InfiniteDamage_Input;
            InputManager.Input.Debug.InfiniteStamina.started -= InfiniteStamina_Input;
        }
    }

    private void InfiniteStamina_Input(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        
    }

    private void InfiniteDamage_Input(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        EnableInfiniteDamage(!InfiniteDamage);
    }

    private void InfiniteHealth_Input(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        EnableInfiniteHealth(!InfiniteHealth);
    }

    public void EnableInfiniteHealth(bool enable)
    {
        InfiniteHealth = enable;
        UpdateCheatsStatus();

        Debug.Log($"{CHEATS_DEBUG} Infinite Health: {(enable ? ENABLED : DISABLED)}");
    }

    public void EnableInfiniteDamage(bool enable)
    {
        InfiniteDamage = enable;
        UpdateCheatsStatus();

        Debug.Log($"{CHEATS_DEBUG} Infinite Damage: {(enable ? ENABLED : DISABLED)}");
    }

    private void UpdateCheatsStatus()
    {
        if (cheatsStatus == null)
            return;

        string finalText = "";

        if (InfiniteHealth)
            finalText += $"Ignore Damage: {ENABLED}\n";

        if (InfiniteDamage)
            finalText += $"Infinite Damage: {ENABLED}\n";

        cheatsStatus.text = finalText;
    }
}
