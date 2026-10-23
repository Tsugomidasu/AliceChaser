using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class RoosterEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float normalMoveSpeed = 1f;
    [SerializeField] private float aggressiveMoveSpeed = 1.75f;
    [SerializeField] private float patrolDistance = 2.5f;
    [SerializeField] private int startingDirection = -1;

    [Header("Aggression")]
    [SerializeField] private float aggressionRange = 3.5f;
    [SerializeField] private float disengageRange = 4.5f;
    [SerializeField] private float peckRange = 1f;

    [Header("Peck")]
    [SerializeField] private Collider2D peckHitbox;
    [SerializeField] private float peckWindup = 0.1f;
    [SerializeField] private float peckActiveTime = 0.15f;
    [SerializeField] private float peckRecovery = 0.35f;

    [Header("Sprite")]
    [SerializeField] private bool spriteFacesRight = true;

    private static readonly int PeckTrigger = Animator.StringToHash("Peck");
    private static readonly int WalkState = Animator.StringToHash("RoosterWalk");

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private EnemyHealth health;
    private Transform player;

    private Coroutine peckRoutine;
    private float startX;
    private float peckHitboxX;
    private int direction;
    private int aggressionSide;
    private bool isAggressive;
    private bool isAttacking;
    private bool reengageLocked;
    private bool wasHitStunned;
    private bool deathHandled;

    private void Awake()
    {
        // Cache the Rooster components.
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        animator = GetComponentInChildren<Animator>(true);
        health = GetComponent<EnemyHealth>();

        // Keep the peck hitbox inactive until the attack reaches its active frames.
        if (peckHitbox != null)
        {
            peckHitbox.enabled = false;
            peckHitboxX = Mathf.Abs(peckHitbox.transform.localPosition.x);
        }
    }

    private void Start()
    {
        // Store the center of the normal patrol area.
        startX = rb.position.x;
        direction = startingDirection >= 0 ? 1 : -1;

        FindPlayer();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        // Leave defeat movement to the shared health reaction.
        if (health.IsDead)
        {
            if (!deathHandled)
            {
                CancelPeck(false);
                deathHandled = true;
            }

            return;
        }

        // Interrupt an attack while the Rooster is reacting to a hit.
        if (health.IsHitStunned)
        {
            if (!wasHitStunned)
            {
                CancelPeck(true);
            }

            wasHitStunned = true;
            rb.linearVelocityX = 0f;
            return;
        }

        wasHitStunned = false;

        if (player == null)
        {
            FindPlayer();
        }

        if (player == null)
        {
            Patrol();
            return;
        }

        float playerDistance = Vector2.Distance(rb.position, player.position);

        // Unlock detection after Alice has moved clear of the Rooster.
        if (reengageLocked && playerDistance >= disengageRange)
        {
            reengageLocked = false;
        }

        if (isAggressive)
        {
            // End the chase when Alice crosses behind the side where aggression started.
            if (HasCrossedAggressionSide())
            {
                StopAggression(true);
            }
            else if (playerDistance >= disengageRange)
            {
                StopAggression(false);
            }
        }
        else if (!reengageLocked &&
                 playerDistance <= aggressionRange &&
                 IsPlayerInFront())
        {
            StartAggression();
        }

        if (isAttacking)
        {
            rb.linearVelocityX = 0f;
            return;
        }

        if (isAggressive)
        {
            AggressiveBehavior(playerDistance);
        }
        else
        {
            Patrol();
        }
    }

    private void AggressiveBehavior(float playerDistance)
    {
        // Stop and attack once Alice is close enough to the beak.
        if (playerDistance <= peckRange)
        {
            rb.linearVelocityX = 0f;
            StartPeck();
            return;
        }

        // Keep chasing in the direction the Rooster was already facing.
        rb.linearVelocityX = direction * aggressiveMoveSpeed;
    }

    private void Patrol()
    {
        float leftEdge = startX - patrolDistance;
        float rightEdge = startX + patrolDistance;

        // Turn around at either patrol limit.
        if (rb.position.x <= leftEdge)
        {
            direction = 1;
        }
        else if (rb.position.x >= rightEdge)
        {
            direction = -1;
        }

        rb.linearVelocityX = direction * normalMoveSpeed;
        UpdateFacing();
    }

    private void StartAggression()
    {
        isAggressive = true;

        // Remember which side Alice was on when the chase began.
        float horizontalOffset = player.position.x - rb.position.x;
        aggressionSide = horizontalOffset >= 0f ? 1 : -1;
    }

    private void StopAggression(bool lockReengage)
    {
        isAggressive = false;

        if (lockReengage)
        {
            reengageLocked = true;
        }

        if (isAttacking || peckRoutine != null)
        {
            CancelPeck(true);
        }
    }

    private bool IsPlayerInFront()
    {
        if (player == null)
        {
            return false;
        }

        // Compare Alice's horizontal position with the Rooster's current direction.
        float horizontalOffset = player.position.x - rb.position.x;
        return horizontalOffset * direction > 0f;
    }

    private bool HasCrossedAggressionSide()
    {
        if (player == null || aggressionSide == 0)
        {
            return false;
        }

        float horizontalOffset = player.position.x - rb.position.x;
        return horizontalOffset * aggressionSide <= 0f;
    }

    private void StartPeck()
    {
        if (isAttacking || peckRoutine != null)
        {
            return;
        }

        peckRoutine = StartCoroutine(PeckRoutine());
    }

    private IEnumerator PeckRoutine()
    {
        isAttacking = true;
        rb.linearVelocityX = 0f;
        animator.SetTrigger(PeckTrigger);

        // Give the player a short warning before the damaging frames begin.
        if (peckWindup > 0f)
        {
            yield return new WaitForSeconds(peckWindup);
        }

        if (health.IsDead || health.IsHitStunned)
        {
            FinishPeck();
            yield break;
        }

        if (peckHitbox != null)
        {
            peckHitbox.enabled = true;
        }

        if (peckActiveTime > 0f)
        {
            yield return new WaitForSeconds(peckActiveTime);
        }

        if (peckHitbox != null)
        {
            peckHitbox.enabled = false;
        }

        if (peckRecovery > 0f)
        {
            yield return new WaitForSeconds(peckRecovery);
        }

        FinishPeck();
    }

    private void FinishPeck()
    {
        if (peckHitbox != null)
        {
            peckHitbox.enabled = false;
        }

        isAttacking = false;
        peckRoutine = null;
    }

    private void CancelPeck(bool returnToWalk)
    {
        if (peckRoutine != null)
        {
            StopCoroutine(peckRoutine);
            peckRoutine = null;
        }

        if (peckHitbox != null)
        {
            peckHitbox.enabled = false;
        }

        isAttacking = false;
        animator.ResetTrigger(PeckTrigger);

        if (returnToWalk)
        {
            animator.Play(WalkState, 0, 0f);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    private void UpdateFacing()
    {
        // Face the current patrol or committed chase direction.
        spriteRenderer.flipX = spriteFacesRight
            ? direction < 0
            : direction > 0;

        // Keep the peck hitbox in front of the Rooster when it turns around.
        if (peckHitbox != null)
        {
            Vector3 localPosition = peckHitbox.transform.localPosition;
            localPosition.x = peckHitboxX * direction;
            peckHitbox.transform.localPosition = localPosition;
        }
    }

    private void OnDisable()
    {
        // Stop an active peck when the enemy is disabled.
        if (peckRoutine != null)
        {
            StopCoroutine(peckRoutine);
            peckRoutine = null;
        }

        if (peckHitbox != null)
        {
            peckHitbox.enabled = false;
        }

        if (rb != null && (health == null || !health.IsDead))
        {
            rb.linearVelocityX = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Show the aggression, disengage, and peck ranges in the Scene view.
        Gizmos.DrawWireSphere(transform.position, aggressionRange);
        Gizmos.DrawWireSphere(transform.position, disengageRange);
        Gizmos.DrawWireSphere(transform.position, peckRange);
    }
}