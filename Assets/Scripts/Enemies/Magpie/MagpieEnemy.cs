using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class MagpieEnemy : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float flightSpeed = 4f;
    [SerializeField] private float despawnDistance = 24f;

    [Header("Drop")]
    [SerializeField] private float dropCooldown = 1.25f;
    [SerializeField] private MagpieThimble thimblePrefab;
    [SerializeField] private Vector2 dropOffset = new Vector2(0f, -0.45f);

    [Header("Sprite")]
    [SerializeField] private bool spriteFacesRight = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth health;

    private float startX;
    private float nextDropTime;

    private void Awake()
    {
        // Cache the Magpie components.
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        // Store where the Magpie entered the encounter.
        startX = rb.position.x;
        nextDropTime = Time.time + dropCooldown;

        // Face left while flying.
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

        // Keep flying through normal hits.
        rb.linearVelocity = new Vector2(-flightSpeed, 0f);

        TryDropThimble();

        // Remove the Magpie offscreen.
        if (startX - rb.position.x >= despawnDistance)
        {
            Destroy(gameObject);
        }
    }

    private void TryDropThimble()
    {
        // Drop without targeting Alice.
        if (thimblePrefab == null || Time.time < nextDropTime)
        {
            return;
        }

        DropThimble();
        nextDropTime = Time.time + dropCooldown;
    }

    private void DropThimble()
    {
        Vector2 spawnPosition = rb.position + dropOffset;

        MagpieThimble thimble = Instantiate(
            thimblePrefab,
            spawnPosition,
            Quaternion.identity
        );

        thimble.Drop(GetComponentsInChildren<Collider2D>(true));
    }

    private void OnDisable()
    {
        // Stop normal movement when the Magpie is disabled.
        if (rb != null && (health == null || !health.IsDead))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}