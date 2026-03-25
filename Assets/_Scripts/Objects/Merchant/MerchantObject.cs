using UnityEngine;

public class MerchantObject : InteractableObject
{
    private MerchantController merchant;

    private void Awake()
    {
        merchant = GetComponent<MerchantController>();
    }

    public override void Interact(GameObject sender)
    {
        base.Interact(sender);

        merchant.ShowMerchantWindow(sender);
    }
}
