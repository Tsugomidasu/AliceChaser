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
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private EnemyHealth health;
    private Transform player;
    private Collider2D playerCollider;

    private Coroutine peckRoutine;
    private float startX;
    private float peckHitboxX;
    private int direction;
    private bool isAggressive;
    private bool isAttacking;
    private bool wasHitStunned;
    private bool deathHandled;

    private void Awake()
    {
        // Cache the Rooster components.
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        animator = GetComponentInChildren<Animator>(true);
        health = GetComponent<EnemyHealth>();

        // Keep the peck hitbox inactive until the attack.
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
        // Shared health handles defeat movement.
        if (health.IsDead)
        {
            if (!deathHandled)
            {
                CancelPeck(false);
                deathHandled = true;
            }

            return;
        }

        // Hits interrupt an active peck.
        if (health.IsHitStunned)
        {
            if (!wasHitStunned)
            {
                CancelPeck(true);
            }

            wasHitStunned = true;
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

        float distanceToAlice = Vector2.Distance(rb.position, player.position);

        if (isAggressive)
        {
            // Disengage from Alice.
            if (!IsEnemyInFrontOfAlice() ||
                distanceToAlice >= disengageRange ||
                !HasLineOfSight())
            {
                StopAggression();
            }
        }
        else if (CanDetectAlice(aggressionRange))
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
            AggressiveBehavior(distanceToAlice);
        }
        else
        {
            Patrol();
        }
    }

    private void AggressiveBehavior(float distanceToAlice)
    {
        // Stop and peck.
        if (distanceToAlice <= peckRange)
        {
            rb.linearVelocityX = 0f;
            StartPeck();
            return;
        }

        // Keep chasing in the current direction.
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
        // Alice always faces right in gameplay.
        return player != null && rb.position.x > player.position.x;
    }

    private bool IsAliceInFront()
    {
        if (player == null)
        {
            return false;
        }

        // Check whether Alice is in front.
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

            // Enemy bodies do not block line of sight.
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

    private void StartAggression()
    {
        isAggressive = true;
    }

    private void StopAggression()
    {
        isAggressive = false;

        if (isAttacking || peckRoutine != null)
        {
            CancelPeck(true);
        }
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

        // Wind up before enabling the hitbox.
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
        playerCollider = playerObject != null ? playerObject.GetComponent<Collider2D>() : null;
    }

    private void UpdateFacing()
    {
        // Face the current direction.
        spriteRenderer.flipX = spriteFacesRight
            ? direction < 0
            : direction > 0;

        // Keep the peck hitbox in front.
        if (peckHitbox != null)
        {
            Vector3 localPosition = peckHitbox.transform.localPosition;
            localPosition.x = peckHitboxX * direction;
            peckHitbox.transform.localPosition = localPosition;
        }
    }

    private void OnDisable()
    {
        // Stop the peck when disabled.
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
        // Show the attack ranges.
        Gizmos.DrawWireSphere(transform.position, aggressionRange);
        Gizmos.DrawWireSphere(transform.position, disengageRange);
        Gizmos.DrawWireSphere(transform.position, peckRange);
    }
}