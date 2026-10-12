using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    [SerializeField] private int health = 4;

    public void TakeDamage(int damage)
    {
        // Remove health.
        health -= damage;

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}