using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class HurtEffect : NetworkBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float hurtFadeDuration;

    public void ActivateHurtEffect()
    {
        if (IsOwner)
        {
            ExecuteHurtEffect();
        }
        else
        {
            ActivateHurtEffect_OwnerRpc();
        }
    }

    [Rpc(SendTo.Owner)]
    private void ActivateHurtEffect_OwnerRpc()
    {
        ExecuteHurtEffect();
    }

    private void ExecuteHurtEffect()
    {
        canvasGroup.DOKill();
        canvasGroup.DOFade(1, 0.1f);
        canvasGroup.DOFade(0, hurtFadeDuration).SetDelay(0.1f);

        //Debug.Log("Hurt Effect Activated");
    }
}
