using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class FoxEnemy : MonoBehaviour
{
    [Header("Run")]
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float despawnDistance = 24f;

    [Header("Sprite")]
    [SerializeField] private bool spriteFacesRight = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth health;

    private float startX;

    private void Awake()
    {
        // Cache the Fox components.
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        // Store where the Fox entered the encounter.
        startX = rb.position.x;

        // Face left while running.
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = spriteFacesRight;
        }
    }

    private void FixedUpdate()
    {
        // Let shared health handle defeat.
        if (health.IsDead)
        {
            return;
        }

        // Keep running through normal hits.
        rb.linearVelocityX = -runSpeed;

        // Remove the Fox offscreen.
        if (startX - rb.position.x >= despawnDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        // Stop normal movement when the Fox is disabled.
        if (rb != null && (health == null || !health.IsDead))
        {
            rb.linearVelocityX = 0f;
        }
    }
}