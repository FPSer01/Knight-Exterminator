using System;
using UnityEngine;

public class TeleportObject : InteractableObject
{
    [Header("Teleport Options")]
    [SerializeField] private Transform teleportPoint;

    [Space(20f)]
    [SerializeField] private ParticleSystem portalVFX;

    public event Action OnTeleportComplete;

    public override void HighlightObject(bool highlight) { }

    public override void Interact(GameObject sender)
    {
        base.Interact(sender);

        if (sender.TryGetComponent(out Rigidbody senderRB))
        {
            senderRB.position = teleportPoint.position;
            OnTeleportComplete?.Invoke();
        }
        else
        {
            Debug.LogError("Нет Rigidbody у отправителя", this);
        }
        
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);

        if (active)
        {
            portalVFX.Play();
        }
        else
        {
            portalVFX.Stop();
        }
    }
}
