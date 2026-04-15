using KE;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerComponents : NetworkBehaviour
{
    [Header("Camera System")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private KE.CameraController cameraController;
    [SerializeField] private PlayerTargetLock targetLockSystem;

    [Header("Base")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private RigBuilder mainRig;
    [SerializeField] private RigBuilder attackAimRig;

    [Header("Mechanics")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerAttackBase playerAttack;
    [SerializeField] private PlayerSFXController sfxController;
    [SerializeField] private EntityStatusController statusController;
    [SerializeField] private PlayerLevelController levelController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerStanceBase playerStance;
    [SerializeField] private PlayerStatsController statsController;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerUI playerUI;

    [Header("Network")]
    [SerializeField] private NetworkTransform networkTransform;
    [SerializeField] private NetworkRigidbody networkRigidbody;
    [SerializeField] private NetworkAnimator networkAnimator;

    [Header("Utility")]
    [SerializeField] private SpectatorTarget spectatorTarget;

    #region Public

    public KE.CameraController CameraController { get => cameraController; }
    public PlayerTargetLock TargetLockSystem { get => targetLockSystem; }
    public Animator Animator { get => animator; }
    public Rigidbody Rigidbody { get => rb; }
    public CapsuleCollider CapsuleCollider { get => capsuleCollider; }
    public PlayerMovement Movement { get => playerMovement; }
    public PlayerHealth Health { get => playerHealth; }
    public PlayerStamina Stamina { get => playerStamina; }
    public PlayerAttackBase Attack { get => playerAttack; }
    public PlayerSFXController SfxController { get => sfxController; }
    public EntityStatusController StatusController { get => statusController; }
    public PlayerLevelController LevelController { get => levelController; }
    public PlayerInteraction Interaction { get => playerInteraction; }
    public PlayerStanceBase Stance { get => playerStance; }
    public PlayerStatsController StatsController { get => statsController; }
    public PlayerInventory Inventory { get => playerInventory; }
    public PlayerUI UI { get => playerUI; }
    public NetworkTransform NetworkTransform { get => networkTransform; }
    public NetworkRigidbody NetworkRigidbody { get => networkRigidbody; }
    public NetworkAnimator NetworkAnimator { get => networkAnimator; }
    public Camera MainCamera { get => mainCamera; }
    public SpectatorTarget SpectatorTarget { get => spectatorTarget; }
    public AudioListener AudioListener { get => audioListener; }

    #endregion

    #region Collider Utility

    public void AddExcludeLayers(LayerMask layers)
    {
        ExecuteAddExcludeLayers(layers);
        AddExcludeLayers_Rpc(layers);
    }

    [Rpc(SendTo.NotOwner)]
    private void AddExcludeLayers_Rpc(int layersValue)
    {
        LayerMask layers = layersValue;
        ExecuteAddExcludeLayers(layers);
    }

    private void ExecuteAddExcludeLayers(LayerMask layers)
    {
        capsuleCollider.excludeLayers |= layers;
    }

    public void RemoveExcludeLayers(LayerMask layers)
    {
        ExecuteRemoveExcludeLayers(layers);
        RemoveExcludeLayers_Rpc(layers);
    }

    [Rpc(SendTo.NotOwner)]
    private void RemoveExcludeLayers_Rpc(int layersValue)
    {
        LayerMask layers = layersValue;
        ExecuteRemoveExcludeLayers(layers);
    }

    private void ExecuteRemoveExcludeLayers(LayerMask layers)
    {
        capsuleCollider.excludeLayers &= ~layers;
    }

    #endregion

    #region Rig Utility

    /// <summary>
    /// Активировать или деактивировать риг
    /// </summary>
    /// <param name="activate"></param>
    public void ActivateRig(bool activate)
    {
        if (playerHealth.IsDead && activate)
            return;

        ExecuteActivateRig(activate);
        ActivateRig_Rpc(activate);
    }

    [Rpc(SendTo.NotOwner)]
    private void ActivateRig_Rpc(bool activate)
    {
        ExecuteActivateRig(activate);
    }

    private void ExecuteActivateRig(bool activate)
    {
        foreach (var layer in mainRig.layers)
        {
            layer.active = activate;
        }

        if (attackAimRig != null)
        {
            foreach (var layer in attackAimRig.layers)
            {
                layer.active = activate;
            }
        }
    }

    #endregion
}
