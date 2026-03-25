using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask interationMask;
    [SerializeField] private float maxInterationDistance;
    //[SerializeField] private float minInteractionDistance;

    [Header("UI")]
    [SerializeField] private CanvasGroup toolTipObject;
    [SerializeField] private TMP_Text toolTipText;

    private IInteractable currentInteractable;

    private void Start()
    {
        SetToolTip(false);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        InputManager.Input.Player.Interact.started += Interact_started;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        InputManager.Input.Player.Interact.started -= Interact_started;
    }

    private void Interact_started(InputAction.CallbackContext obj)
    {
        currentInteractable?.Interact(gameObject);
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        CheckForInteractObjects();
    }

    private void CheckForInteractObjects()
    {
        IInteractable interactableObject = null;
        Collider selectedObject = null;
        Collider[] objectsCol = new Collider[64];

        if (Physics.OverlapSphereNonAlloc(transform.position, maxInterationDistance, objectsCol, interationMask) > 0)
        {
            float minDistance = float.MaxValue;

            for (int i = 0; i < objectsCol.Length; i++)
            {
                if (objectsCol[i] == null)
                    continue;

                if (!objectsCol[i].TryGetComponent(out IInteractable interactable))
                    continue;

                float distance = Vector3.Distance(transform.position, objectsCol[i].transform.position);

                if (minDistance > distance)
                {
                    minDistance = distance;
                    selectedObject = objectsCol[i];
                    interactableObject = interactable;
                }
            }

            if (interactableObject == null)
                UnselectObject(interactableObject);

            if (currentInteractable == interactableObject)
                return;

            currentInteractable?.HighlightObject(false);
            currentInteractable = interactableObject;
            SetToolTip(true, currentInteractable.GetInteractionToolTip() + ": " + GetInteractionKey());
            currentInteractable?.HighlightObject(true);
        }
        else if (currentInteractable != null)
        {
            UnselectObject(interactableObject);
        }
    }

    private void UnselectObject(IInteractable newInteractable)
    {
        newInteractable?.HighlightObject(false);
        currentInteractable?.HighlightObject(false);
        currentInteractable = newInteractable;
        SetToolTip(false);
    }

    private void SetToolTip(bool active, string text = "")
    {
        toolTipObject.alpha = active ? 1 : 0;
        toolTipText.text = text;
    }

    private string GetInteractionKey()
    {
        /*var keyboardScheme = InputManager.Input.controlSchemes.First(x => x.name == "Keyboard (EN)").bindingGroup;
        InputManager.Input.bindingMask = InputBinding.MaskByGroup(keyboardScheme);*/

        return InputManager.Input.Player.Interact.GetBindingDisplayString(0);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, maxInterationDistance);
    }
}
