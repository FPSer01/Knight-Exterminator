using UnityEngine;

public class TutorialPlayerDamageObject : MonoBehaviour
{
    [SerializeField] private Collider attackCollider;
    [SerializeField] private AttackDamageType damage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerHealth player))
        {
            player.TakeDamage(damage, null); 
        }
    }
}
