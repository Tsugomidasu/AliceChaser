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
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        startX = rb.position.x;
        direction = startingDirection >= 0 ? 1 : -1;

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (health.IsHitStunned || health.IsDead)
        {
            return;
        }

        // Keep patrol bounds relative to the spawn point.
        float leftEdge = startX - patrolDistance;
        float rightEdge = startX + patrolDistance;

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

        rb.linearVelocityX = direction * moveSpeed;
        UpdateFacing();
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
}
