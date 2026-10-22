using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        // Cache the health component when this belongs to an enemy.
        enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Do not deal contact damage while the enemy is interrupted or defeated.
        if (enemyHealth != null && (enemyHealth.IsHitStunned || enemyHealth.IsDead))
        {
            return;
        }

        // Find Alice on the collider hierarchy.
        AliceHealth alice = other.GetComponentInParent<AliceHealth>();

        // Alice Health handles invulnerability between hits.
        if (alice != null)
        {
            alice.TakeDamage(damage, transform.position);
        }
    }
}
