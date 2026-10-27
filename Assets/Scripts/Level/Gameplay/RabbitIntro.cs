using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class RabbitIntro : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float startDelay = 1.5f;
    [SerializeField] private float runSpeed = 8.5f;

    [Header("Ground")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Despawn")]
    [SerializeField] private float despawnPadding = 2f;
    [SerializeField] private float fallbackDistance = 24f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private static readonly int IdleState =
        Animator.StringToHash("Base Layer.RabbitIdle");
    private static readonly int RunState =
        Animator.StringToHash("Base Layer.RabbitRun");

    private Rigidbody2D rb;
    private Camera mainCamera;

    private float startX;
    private float runStartTime;
    private bool isRunning;

    public float StartDelay => startDelay;

    private void Awake()
    {
        // Cache the Rabbit components.
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private void Start()
    {
        startX = rb.position.x;
        runStartTime = Time.time + startDelay;
        mainCamera = Camera.main;

        // Start with the Rabbit's opening poses.
        if (animator != null)
        {
            animator.Play(IdleState, 0, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (!isRunning)
        {
            rb.linearVelocityX = 0f;

            // Start running.
            if (Time.time >= runStartTime && IsGrounded())
            {
                StartRun();
            }

            return;
        }

        // Move the Rabbit.
        rb.linearVelocityX = runSpeed;
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        // Remove the Rabbit offscreen.
        if (mainCamera != null && mainCamera.orthographic)
        {
            float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
            float rightEdge = mainCamera.transform.position.x + halfWidth;

            if (transform.position.x > rightEdge + despawnPadding)
            {
                Destroy(gameObject);
            }

            return;
        }

        // Use the fallback distance.
        if (transform.position.x - startX >= fallbackDistance)
        {
            Destroy(gameObject);
        }
    }

    private void StartRun()
    {
        isRunning = true;

        if (animator != null)
        {
            animator.Play(RunState, 0, 0f);
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return true;
        }

        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        ) != null;
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

        // Show the ground check.
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}