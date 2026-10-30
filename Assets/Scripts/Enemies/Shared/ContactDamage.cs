using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Disable contact damage during hit stun and defeat.
        if (enemyHealth != null && (enemyHealth.IsHitStunned || enemyHealth.IsDead))
        {
            return;
        }

        AliceHealth alice = other.GetComponentInParent<AliceHealth>();

        if (alice != null)
        {
            alice.TakeDamage(damage, transform.position);
        }
    }
}
