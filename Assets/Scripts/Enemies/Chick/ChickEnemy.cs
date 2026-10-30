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

    [Header("Audio")]
    [SerializeField] private AudioClip chirpClip;
    [SerializeField] private float minChirpInterval = 3f;
    [SerializeField] private float maxChirpInterval = 8f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth health;

    private float startX;
    private float nextHopTime;
    private float nextChirpTime;
    private int direction;
    private bool wasGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        startX = rb.position.x;
        direction = startingDirection >= 0 ? 1 : -1;
        nextHopTime = Time.time + landingPause;
        ScheduleNextChirp();

        UpdateFacing();
    }

    private void Update()
    {
        if (health == null || health.IsDead)
        {
            return;
        }

        if (Time.time >= nextChirpTime)
        {
            GameAudio.Play(chirpClip);
            ScheduleNextChirp();
        }
    }

    private void FixedUpdate()
    {
        if (health.IsDead)
        {
            return;
        }

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

        // Start the landing delay when a hop ends.
        if (isGrounded && !wasGrounded)
        {
            nextHopTime = Time.time + landingPause;
            rb.linearVelocityX = 0f;
        }

        if (isGrounded)
        {
            rb.linearVelocityX = 0f;

            // Hop again only after the landing pause finishes.
            if (Time.time >= nextHopTime)
            {
                UpdateDirection();

                rb.linearVelocity = new Vector2(
                    direction * forwardSpeed,
                    hopSpeed
                );
            }
        }
        else
        {
            rb.linearVelocityX = direction * forwardSpeed;
        }

        wasGrounded = isGrounded;
        UpdateFacing();
    }


    private void ScheduleNextChirp()
    {
        float minimum = Mathf.Max(0.1f, minChirpInterval);
        float maximum = Mathf.Max(minimum, maxChirpInterval);
        nextChirpTime = Time.time + Random.Range(minimum, maximum);
    }

    private void UpdateDirection()
    {
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
        spriteRenderer.flipX = spriteFacesRight
            ? direction < 0
            : direction > 0;
    }

    private void OnDisable()
    {
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

        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
