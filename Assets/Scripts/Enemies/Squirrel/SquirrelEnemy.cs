using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class SquirrelEnemy : MonoBehaviour
{
    private enum SquirrelState
    {
        Patrol,
        Windup,
        Airborne,
        Recovery
    }

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 1.25f;
    [SerializeField] private float patrolDistance = 2.5f;
    [SerializeField] private int startingDirection = -1;

    [Header("Pounce")]
    [SerializeField] private float pounceRange = 3f;
    [SerializeField] private float pounceWindup = 0.15f;
    [SerializeField] private float pounceHorizontalSpeed = 5.5f;
    [SerializeField] private float pounceVerticalSpeed = 5.5f;
    [SerializeField] private float landingRecovery = 0.4f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Sprite")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Audio")]
    [SerializeField] private AudioClip aggroClip;
    [SerializeField] private AudioClip pounceClip;

    private static readonly int JumpingParameter =
        Animator.StringToHash("Jumping");

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private EnemyHealth health;
    private Transform player;
    private Collider2D playerCollider;

    private SquirrelState state;
    private float startX;
    private float stateEndTime;
    private int direction;
    private int pounceDirection;
    private bool hasLeftGround;
    private bool hasJumpingParameter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        animator = GetComponentInChildren<Animator>(true);
        health = GetComponent<EnemyHealth>();

        if (animator != null)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == JumpingParameter &&
                    parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasJumpingParameter = true;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        startX = rb.position.x;
        direction = startingDirection >= 0 ? 1 : -1;
        state = SquirrelState.Patrol;

        FindPlayer();
        UpdateFacing();
        SetJumping(false);
    }

    private void FixedUpdate()
    {
        if (health.IsDead)
        {
            return;
        }

        bool isGrounded = IsGrounded();

        // Grounded hit stun cancels the windup.
        if (health.IsHitStunned && state != SquirrelState.Airborne)
        {
            if (state == SquirrelState.Windup)
            {
                state = SquirrelState.Patrol;
            }

            return;
        }

        if (player == null)
        {
            FindPlayer();
        }

        switch (state)
        {
            case SquirrelState.Patrol:
                Patrol(isGrounded);
                break;

            case SquirrelState.Windup:
                Windup();
                break;

            case SquirrelState.Airborne:
                Airborne(isGrounded);
                break;

            case SquirrelState.Recovery:
                Recover();
                break;
        }
    }

    private void Patrol(bool isGrounded)
    {
        if (isGrounded && CanDetectAlice(pounceRange))
        {
            StartWindup();
            return;
        }

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

        rb.linearVelocityX = direction * patrolSpeed;
        UpdateFacing();
    }

    private bool CanDetectAlice(float range)
    {
        if (player == null)
        {
            return false;
        }

        float distanceToAlice = Vector2.Distance(rb.position, player.position);

        return IsEnemyInFrontOfAlice() &&
               IsAliceInFront() &&
               distanceToAlice <= range &&
               HasLineOfSight();
    }

    private bool IsEnemyInFrontOfAlice()
    {
        return player != null && rb.position.x > player.position.x;
    }

    private bool IsAliceInFront()
    {
        if (player == null)
        {
            return false;
        }

        float horizontalOffset = player.position.x - rb.position.x;
        return horizontalOffset * direction > 0f;
    }

    private bool HasLineOfSight()
    {
        if (player == null)
        {
            return false;
        }

        Vector2 start = bodyCollider != null
            ? bodyCollider.bounds.center
            : rb.position;

        Vector2 end = playerCollider != null
            ? playerCollider.bounds.center
            : (Vector2)player.position;

        RaycastHit2D[] hits = Physics2D.LinecastAll(start, end);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger)
            {
                continue;
            }

            // Enemy colliders do not block line of sight.
            EnemyHealth hitEnemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (hitEnemy != null)
            {
                continue;
            }

            if (hit.collider.transform == player ||
                hit.collider.transform.IsChildOf(player))
            {
                return true;
            }

            return false;
        }

        return true;
    }

    private void StartWindup()
    {
        // Telegraph the pounce before movement is committed.
        state = SquirrelState.Windup;
        stateEndTime = Time.time + pounceWindup;
        pounceDirection = direction;
        rb.linearVelocityX = 0f;
        GameAudio.Play(aggroClip);
    }

    private void Windup()
    {
        rb.linearVelocityX = 0f;

        if (Time.time < stateEndTime)
        {
            return;
        }

        StartPounce();
    }

    private void StartPounce()
    {
        state = SquirrelState.Airborne;
        hasLeftGround = false;
        SetJumping(true);
        GameAudio.Play(pounceClip);

        // Lock the pounce direction at takeoff.
        rb.linearVelocity = new Vector2(
            pounceDirection * pounceHorizontalSpeed,
            pounceVerticalSpeed
        );
    }

    private void Airborne(bool isGrounded)
    {
        if (!isGrounded)
        {
            hasLeftGround = true;
        }

        if (hasLeftGround)
        {
            rb.linearVelocityX =
                pounceDirection * pounceHorizontalSpeed;
        }

        if (hasLeftGround &&
            isGrounded &&
            rb.linearVelocityY <= 0.1f)
        {
            StartRecovery();
        }
    }

    private void StartRecovery()
    {
        // Pause after landing before patrol can resume.
        state = SquirrelState.Recovery;
        stateEndTime = Time.time + landingRecovery;
        rb.linearVelocityX = 0f;
        SetJumping(false);
    }

    private void Recover()
    {
        rb.linearVelocityX = 0f;

        if (Time.time >= stateEndTime)
        {
            state = SquirrelState.Patrol;
        }
    }

    private bool IsGrounded()
    {
        return groundCheck != null &&
            Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            ) != null;
    }

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        player = playerObject != null
            ? playerObject.transform
            : null;

        playerCollider = playerObject != null
            ? playerObject.GetComponent<Collider2D>()
            : null;
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = spriteFacesRight
            ? direction < 0
            : direction > 0;
    }

    private void SetJumping(bool jumping)
    {
        if (animator != null && hasJumpingParameter)
        {
            animator.SetBool(JumpingParameter, jumping);
        }
    }

    private void OnDisable()
    {
        if (rb != null && (health == null || !health.IsDead))
        {
            rb.linearVelocityX = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, pounceRange);

        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }
    }
}
