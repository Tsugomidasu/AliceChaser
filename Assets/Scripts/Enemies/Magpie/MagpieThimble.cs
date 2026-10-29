using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MagpieThimble : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float spinSpeed = 240f;

    [Header("Audio")]
    [SerializeField] private AudioClip impactClip;

    private Rigidbody2D rb;
    private Collider2D thimbleCollider;
    private Collider2D[] ownerColliders;
    private bool dropped;

    private void Awake()
    {
        // Cache the projectile components.
        rb = GetComponent<Rigidbody2D>();
        thimbleCollider = GetComponent<Collider2D>();
    }

    public void Drop(Collider2D[] magpieColliders)
    {
        if (dropped)
        {
            return;
        }

        dropped = true;
        ownerColliders = magpieColliders;

        // Ignore the Magpie that released the thimble.
        SetOwnerCollisionIgnored(true);

        float spinDirection = Random.value < 0.5f ? -1f : 1f;
        rb.angularVelocity = spinDirection * spinSpeed;

        Destroy(gameObject, lifetime);
    }

    private void SetOwnerCollisionIgnored(bool ignored)
    {
        if (thimbleCollider == null || ownerColliders == null)
        {
            return;
        }

        foreach (Collider2D ownerCollider in ownerColliders)
        {
            if (ownerCollider != null)
            {
                Physics2D.IgnoreCollision(
                    thimbleCollider,
                    ownerCollider,
                    ignored
                );
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameAudio.Play(impactClip);

        AliceHealth alice = collision.collider.GetComponentInParent<AliceHealth>();

        if (alice != null)
        {
            alice.TakeDamage(damage, transform.position);
        }

        Destroy(gameObject);
    }
}