using Unity.Netcode;
using UnityEngine;

public class HealPickUpObject : InteractableObject
{
    [SerializeField] private ParticleSystem highlightEffect;

    private bool highlight = false;

    public override void HighlightObject(bool highlight)
    {
        if (this.highlight == highlight || highlightEffect == null)
            return;

        this.highlight = highlight;

        if (highlight)
            highlightEffect.Play();
        else
            highlightEffect.Stop();

        base.HighlightObject(highlight);
    }

    public override void Interact(GameObject sender)
    {
        if (sender.TryGetComponent(out PlayerHealth playerHealth))
        {
            bool success = playerHealth.RefillHeals(1);

            if (success)
            {
                RequestDestroy_ServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestDestroy_ServerRpc()
    {
        NetworkObject.Despawn();
    }

    private void FixedUpdate()
    {
        HoldEffectStill();
    }

    private void HoldEffectStill()
    {
        var rotation = Quaternion.Euler(Vector3.zero);

        highlightEffect.transform.rotation = rotation;
        highlightEffect.transform.rotation = rotation;
    }
}
