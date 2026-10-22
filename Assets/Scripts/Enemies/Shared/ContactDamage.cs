using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerStay2D(Collider2D other)
    {
        // Find Alice on the collider hierarchy.
        AliceHealth alice = other.GetComponentInParent<AliceHealth>();

        // Alice Health handles invulnerability between hits.
        if (alice != null)
        {
            alice.TakeDamage(damage, transform.position);
        }
    }
}
