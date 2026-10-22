using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 4;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake()
    {
        // Initialize health.
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        // Ignore invalid damage.
        if (IsDead || damage <= 0)
        {
            return;
        }

        // Remove health.
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        // Destroy the enemy at zero health.
        if (CurrentHealth <= 0)
        {
            IsDead = true;
            Destroy(gameObject);
        }
    }
}
