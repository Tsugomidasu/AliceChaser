using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class ChickEnemy : MonoBehaviour
{
    [Header("Hop")]
    [SerializeField] private float forwardSpeed = 1.5f;
    [SerializeField] private float hopSpeed = 4f;
    [SerializeField] private float landingPause = 0.12f;
    [SerializeField] private float patrolDistance = 2.5f;
    [SerializeField] private int startingDirection = -1;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.06f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Sprite")]
    [SerializeField] private bool spriteFacesRight = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth health;

    private float startX;
    private float nextHopTime;
    private int direction;
    private bool wasGrounded;

    private void Awake()
    {
        // Cache the Chick components.
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        // Store the center of the hopping area.
        startX = rb.position.x;
        direction = startingDirection >= 0 ? 1 : -1;
        nextHopTime = Time.time + landingPause;

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        // Let shared health handle defeat.
        if (health.IsDead)
        {
            return;
        }

        // Stop during hit stun.
        if (health.IsHitStunned)
        {
            return;
        }

        bool isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            ) != null;

        // Start the landing pause.
        if (isGrounded && !wasGrounded)
        {
            nextHopTime = Time.time + landingPause;
            rb.linearVelocityX = 0f;
        }

        if (isGrounded)
        {
            // Keep the Chick still briefly between hops.
            rb.linearVelocityX = 0f;

            if (Time.time >= nextHopTime)
            {
                UpdateDirection();

                // Hop forward in a small arc.
                rb.linearVelocity = new Vector2(
                    direction * forwardSpeed,
                    hopSpeed
                );
            }
        }
        else
        {
            // Keep forward momentum while airborne.
            rb.linearVelocityX = direction * forwardSpeed;
        }

        wasGrounded = isGrounded;
        UpdateFacing();
    }

    private void UpdateDirection()
    {
        // Turn at the patrol limits.
        float leftEdge = startX - patrolDistance;
        float rightEdge = startX + patrolDistance;

        if (rb.position.x <= leftEdge)
        {
            direction = 1;
        }
        else if (rb.position.x >= rightEdge)
        {
            direction = -1;
        }
    }

    private void UpdateFacing()
    {
        // Face the current hopping direction.
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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        // Show the ground check.
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}