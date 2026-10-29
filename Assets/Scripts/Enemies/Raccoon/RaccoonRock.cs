using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class RaccoonRock : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float horizontalSpeed = 4.9f;
    [SerializeField] private float arcLift = 3.5f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float spinSpeed = 360f;
    [SerializeField] private float deflectSpeed = 8f;
    [SerializeField] private float deflectLift = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip reflectClip;
    [SerializeField] private AudioClip impactClip;

    private Rigidbody2D rb;
    private Collider2D rockCollider;
    private Collider2D[] ownerColliders;
    private bool launched;
    private bool deflected;

    private void Awake()
    {
        // Cache the projectile components.
        rb = GetComponent<Rigidbody2D>();
        rockCollider = GetComponent<Collider2D>();
    }

    public void Launch(int direction, Collider2D[] raccoonColliders)
    {
        if (launched)
        {
            return;
        }

        launched = true;
        ownerColliders = raccoonColliders;

        // Ignore the owner before deflection.
        SetOwnerCollisionIgnored(true);

        float launchDirection = direction >= 0 ? 1f : -1f;

        // Use a fixed arc instead of aiming at Alice.
        rb.linearVelocity = new Vector2(
            launchDirection * horizontalSpeed,
            arcLift
        );

        rb.angularVelocity = -launchDirection * spinSpeed;

        Destroy(gameObject, lifetime);
    }

    public void Deflect(Transform deflector)
    {
        if (!launched || deflected)
        {
            return;
        }

        deflected = true;
        GameAudio.Play(reflectClip);

        // Allow reflected enemy hits.
        SetOwnerCollisionIgnored(false);

        if (deflector != null && rockCollider != null)
        {
            Collider2D[] deflectorColliders =
                deflector.GetComponentsInChildren<Collider2D>(true);

            foreach (Collider2D deflectorCollider in deflectorColliders)
            {
                if (deflectorCollider != null)
                {
                    Physics2D.IgnoreCollision(
                        rockCollider,
                        deflectorCollider,
                        true
                    );
                }
            }
        }

        float direction = deflector == null ||
            transform.position.x >= deflector.position.x
            ? 1f
            : -1f;

        rb.linearVelocity = new Vector2(
            direction * deflectSpeed,
            Mathf.Max(Mathf.Abs(rb.linearVelocityY), deflectLift)
        );

        rb.angularVelocity = direction * spinSpeed;
    }

    private void SetOwnerCollisionIgnored(bool ignored)
    {
        if (rockCollider == null || ownerColliders == null)
        {
            return;
        }

        foreach (Collider2D ownerCollider in ownerColliders)
        {
            if (ownerCollider != null)
            {
                Physics2D.IgnoreCollision(
                    rockCollider,
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

        if (!deflected && alice != null)
        {
            alice.TakeDamage(damage, transform.position);
            Destroy(gameObject);
            return;
        }

        if (deflected)
        {
            EnemyHealth enemy =
                collision.collider.GetComponentInParent<EnemyHealth>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage, transform.position);
                Destroy(gameObject);
                return;
            }

            if (alice != null)
            {
                return;
            }
        }

        Destroy(gameObject);
    }
}