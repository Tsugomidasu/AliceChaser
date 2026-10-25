using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class RaccoonEnemy : MonoBehaviour
{
    private enum RaccoonState
    {
        Idle,
        Aggro,
        Windup,
        Recovery
    }

    [Header("Throw")]
    [SerializeField] private float detectionRange = 5.5f;
    [SerializeField] private float minimumThrowRange = 1.5f;
    [SerializeField] private float throwWindup = 0.35f;
    [SerializeField] private float throwRecovery = 0.75f;
    [SerializeField] private RaccoonRock rockPrefab;
    [SerializeField] private Vector2 throwOffset = new Vector2(0.3f, 1f);

    [Header("Sprite")]
    [SerializeField] private int startingDirection = -1;
    [SerializeField] private bool spriteFacesRight = true;

    private static readonly int AggroBool = Animator.StringToHash("Aggro");
    private static readonly int ThrowTrigger = Animator.StringToHash("Throw");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.RaccoonIdle");
    private static readonly int AggroState = Animator.StringToHash("Base Layer.RaccoonAggro");

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private EnemyHealth health;
    private Transform player;
    private Collider2D playerCollider;

    private RaccoonState state;
    private float stateEndTime;
    private int direction;
    private bool deathHandled;

    private void Awake()
    {
        // Cache the Raccoon components.
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        animator = GetComponentInChildren<Animator>(true);
        health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        direction = startingDirection >= 0 ? 1 : -1;
        state = RaccoonState.Idle;

        FindPlayer();
        UpdateFacing();
        SetAggro(false);
    }

    private void FixedUpdate()
    {
        // Shared health handles defeat movement.
        if (health.IsDead)
        {
            if (!deathHandled)
            {
                CancelThrow(false);
                deathHandled = true;
            }

            return;
        }

        // Hit stun cancels an unreleased throw.
        if (health.IsHitStunned)
        {
            if (state == RaccoonState.Windup)
            {
                CancelThrow(CanDetectAlice());
            }

            rb.linearVelocityX = 0f;
            return;
        }

        if (player == null)
        {
            FindPlayer();
        }

        rb.linearVelocityX = 0f;

        switch (state)
        {
            case RaccoonState.Idle:
                Idle();
                break;

            case RaccoonState.Aggro:
                Aggro();
                break;

            case RaccoonState.Windup:
                Windup();
                break;

            case RaccoonState.Recovery:
                Recover();
                break;
        }
    }

    private void Idle()
    {
        if (!CanDetectAlice())
        {
            return;
        }

        EnterAggro();

        if (CanThrowAtAlice())
        {
            StartThrow();
        }
    }

    private void Aggro()
    {
        if (!CanDetectAlice())
        {
            ReturnToIdle();
            return;
        }

        if (CanThrowAtAlice())
        {
            StartThrow();
        }
    }

    private void EnterAggro()
    {
        state = RaccoonState.Aggro;
        SetAggro(true);
    }

    private void StartThrow()
    {
        state = RaccoonState.Windup;
        stateEndTime = Time.time + throwWindup;
        SetAggro(true);

        if (animator != null)
        {
            animator.SetTrigger(ThrowTrigger);
        }
    }

    private void Windup()
    {
        // Recheck detection during the windup.
        if (!CanDetectAlice())
        {
            CancelThrow(false);
            return;
        }

        // Stay aggroed inside the minimum throw range.
        if (!IsOutsideMinimumThrowRange())
        {
            CancelThrow(true);
            return;
        }

        if (Time.time < stateEndTime)
        {
            return;
        }

        ReleaseRock();
    }

    private void ReleaseRock()
    {
        if (rockPrefab != null && player != null)
        {
            Vector2 targetPosition = playerCollider != null
                ? playerCollider.bounds.center
                : (Vector2)player.position;

            RaccoonRock rock = Instantiate(
                rockPrefab,
                GetThrowPosition(),
                Quaternion.identity
            );

            rock.Launch(
                targetPosition,
                GetComponentsInChildren<Collider2D>(true)
            );
        }

        state = RaccoonState.Recovery;
        stateEndTime = Time.time + throwRecovery;
    }

    private void Recover()
    {
        if (!CanDetectAlice())
        {
            ReturnToIdle();
            return;
        }

        SetAggro(true);

        if (Time.time < stateEndTime)
        {
            return;
        }

        state = RaccoonState.Aggro;

        if (CanThrowAtAlice())
        {
            StartThrow();
        }
    }

    private bool CanDetectAlice()
    {
        if (player == null)
        {
            return false;
        }

        float distanceToAlice = Vector2.Distance(rb.position, player.position);

        return IsEnemyInFrontOfAlice() &&
               IsAliceInFront() &&
               distanceToAlice <= detectionRange &&
               HasLineOfSight();
    }

    private bool CanThrowAtAlice()
    {
        return CanDetectAlice() && IsOutsideMinimumThrowRange();
    }

    private bool IsOutsideMinimumThrowRange()
    {
        if (player == null)
        {
            return false;
        }

        return Vector2.Distance(rb.position, player.position) >= minimumThrowRange;
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

    private Vector2 GetThrowPosition()
    {
        Vector3 localOffset = new Vector3(
            throwOffset.x * direction,
            throwOffset.y,
            0f
        );

        return transform.TransformPoint(localOffset);
    }

    private void CancelThrow(bool stayAggro)
    {
        if (animator != null)
        {
            animator.ResetTrigger(ThrowTrigger);
        }

        if (stayAggro)
        {
            state = RaccoonState.Aggro;
            SetAggro(true);

            if (animator != null && animator.HasState(0, AggroState))
            {
                animator.Play(AggroState, 0, 0f);
            }
        }
        else
        {
            ReturnToIdle();
        }
    }

    private void ReturnToIdle()
    {
        state = RaccoonState.Idle;
        SetAggro(false);

        if (animator != null && animator.HasState(0, IdleState))
        {
            animator.Play(IdleState, 0, 0f);
        }
    }

    private void SetAggro(bool aggro)
    {
        if (animator != null)
        {
            animator.SetBool(AggroBool, aggro);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

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

        // Keep the Raccoon facing its set attack direction.
        spriteRenderer.flipX = spriteFacesRight
            ? direction < 0
            : direction > 0;
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            animator.ResetTrigger(ThrowTrigger);
            animator.SetBool(AggroBool, false);
        }

        if (rb != null && (health == null || !health.IsDead))
        {
            rb.linearVelocityX = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Show the throw ranges.
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.DrawWireSphere(transform.position, minimumThrowRange);
    }
}