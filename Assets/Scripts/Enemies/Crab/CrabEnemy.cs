using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class CrabEnemy : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float moveSpeed = 1.25f;
    [SerializeField] private float patrolDistance = 2.5f;
    [SerializeField] private int startingDirection = -1;

    [Header("Sprite")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Audio")]
    [SerializeField] private AudioClip turnClip;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth health;
    private float startX;
    private int direction;

    private void Awake()
    {
        // Cache the Crab components.
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        // Store the center of the patrol area.
        startX = rb.position.x;
        direction = startingDirection >= 0 ? 1 : -1;

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        // Stop during hit stun.
        if (health.IsHitStunned || health.IsDead)
        {
            return;
        }

        // Calculate the patrol limits.
        float leftEdge = startX - patrolDistance;
        float rightEdge = startX + patrolDistance;

        // Turn around at either patrol limit.
        int nextDirection = direction;

        if (rb.position.x <= leftEdge)
        {
            nextDirection = 1;
        }
        else if (rb.position.x >= rightEdge)
        {
            nextDirection = -1;
        }

        if (nextDirection != direction)
        {
            direction = nextDirection;
            GameAudio.Play(turnClip);
        }

        // Move horizontally.
        rb.linearVelocityX = direction * moveSpeed;
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        // Face the current movement direction.
        spriteRenderer.flipX = spriteFacesRight
            ? direction < 0
            : direction > 0;
    }

    private void OnDisable()
    {
        // Stop horizontal movement when disabled.
        if (rb != null)
        {
            rb.linearVelocityX = 0f;
        }
    }
}