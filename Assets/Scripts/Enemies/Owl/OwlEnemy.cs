using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class OwlEnemy : MonoBehaviour
{
    private enum OwlState
    {
        Flying,
        Telegraph,
        Swooping,
        Recovering
    }

    [Header("Flight")]
    [SerializeField] private float flightSpeed = 2f;
    [SerializeField] private float cornerDistance = 8f;
    [SerializeField] private int startingSide = 1;
    [SerializeField] private float recoverySpeed = 6f;
    [SerializeField] private float attackCooldown = 0.65f;

    [Header("Swoop")]
    [SerializeField] private float detectionRange = 5.5f;
    [SerializeField] private float telegraphDuration = 0.35f;
    [SerializeField] private float telegraphSpeedMultiplier = 0.4f;
    [SerializeField] private float swoopDuration = 0.55f;
    [SerializeField] private float passDistance = 1.25f;
    [SerializeField] private float swoopDip = 0.45f;
    [SerializeField] private Collider2D swoopHitbox;

    [Header("Sprite")]
    [SerializeField] private bool spriteFacesRight = true;

    private static readonly int FlyState =
        Animator.StringToHash("Base Layer.OwlFly");
    private static readonly int SwoopState =
        Animator.StringToHash("Base Layer.OwlSwoop");
    private static readonly int RecoverState =
        Animator.StringToHash("Base Layer.OwlRecover");

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private EnemyHealth health;
    private Transform player;
    private Collider2D playerCollider;

    private OwlState state;
    private Vector2 leftCorner;
    private Vector2 rightCorner;
    private Vector2 flightCenter;
    private Vector2 swoopStart;
    private Vector2 swoopControl;
    private Vector2 swoopEnd;
    private Vector2 recoveryTarget;
    private float flightY;
    private float stateEndTime;
    private float nextAttackTime;
    private float swoopElapsed;
    private int direction;
    private int travelDirection;
    private bool activated;

    private void Awake()
    {
        // Cache the Owl components.
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        animator = GetComponentInChildren<Animator>(true);
        health = GetComponent<EnemyHealth>();

        // Keep contact damage inactive outside the swoop.
        if (swoopHitbox != null)
        {
            swoopHitbox.enabled = false;
        }
    }

    private void Start()
    {
        // Use the spawn position as one end of the upper flight path.
        if (startingSide >= 0)
        {
            rightCorner = rb.position;
            leftCorner = rightCorner + Vector2.left * cornerDistance;
            direction = -1;
        }
        else
        {
            leftCorner = rb.position;
            rightCorner = leftCorner + Vector2.right * cornerDistance;
            direction = 1;
        }

        flightCenter = (leftCorner + rightCorner) * 0.5f;
        flightY = rb.position.y;
        state = OwlState.Flying;
        nextAttackTime = Time.time + attackCooldown;

        FindPlayer();
        UpdateFacing(direction);
        PlayState(FlyState);
    }

    private void FixedUpdate()
    {
        // Shared health handles defeat movement.
        if (health.IsDead)
        {
            DisableSwoopHitbox();
            return;
        }

        // Hits briefly interrupt the current movement.
        if (health.IsHitStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null)
        {
            FindPlayer();
        }

        rb.linearVelocity = Vector2.zero;

        switch (state)
        {
            case OwlState.Flying:
                Fly();
                break;

            case OwlState.Telegraph:
                Telegraph();
                break;

            case OwlState.Swooping:
                Swoop();
                break;

            case OwlState.Recovering:
                Recover();
                break;
        }
    }

    private void Fly()
    {
        MoveHorizontally(flightSpeed);

        if (Time.time < nextAttackTime || !CanAttackAlice())
        {
            return;
        }

        // The first valid approach activates the repeating attack cycle.
        activated = true;
        StartTelegraph();
    }

    private bool CanAttackAlice()
    {
        if (player == null)
        {
            return false;
        }

        float horizontalDistance = Mathf.Abs(
            player.position.x - rb.position.x
        );

        if (horizontalDistance > detectionRange)
        {
            return false;
        }

        // Before activation, only engage while the encounter is ahead of Alice.
        if (!activated && flightCenter.x <= player.position.x)
        {
            return false;
        }

        return true;
    }

    private void StartTelegraph()
    {
        state = OwlState.Telegraph;
        stateEndTime = Time.time + telegraphDuration;
        PlayState(FlyState);
    }

    private void Telegraph()
    {
        // Slow the horizontal flight briefly before committing to the swoop.
        MoveHorizontally(flightSpeed * telegraphSpeedMultiplier);

        if (Time.time >= stateEndTime)
        {
            StartSwoop();
        }
    }

    private void StartSwoop()
    {
        if (player == null)
        {
            ReturnToFlight();
            return;
        }

        state = OwlState.Swooping;
        swoopElapsed = 0f;
        travelDirection = direction;
        swoopStart = rb.position;

        // Lock Alice's position when the Owl commits to the attack.
        Vector2 targetPosition = playerCollider != null
            ? playerCollider.bounds.center
            : (Vector2)player.position;

        swoopEnd = targetPosition + new Vector2(
            travelDirection * passDistance,
            0f
        );

        Vector2 midpoint = (swoopStart + swoopEnd) * 0.5f;
        swoopControl = new Vector2(
            midpoint.x,
            targetPosition.y - swoopDip
        );

        UpdateFacing(travelDirection);
        PlayState(SwoopState);

        if (swoopHitbox != null)
        {
            swoopHitbox.enabled = true;
        }
    }

    private void Swoop()
    {
        swoopElapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(swoopElapsed / swoopDuration);

        rb.MovePosition(QuadraticBezier(
            swoopStart,
            swoopControl,
            swoopEnd,
            t
        ));

        if (t >= 1f)
        {
            StartRecovery();
        }
    }

    private void StartRecovery()
    {
        state = OwlState.Recovering;
        recoveryTarget = travelDirection > 0
            ? rightCorner
            : leftCorner;

        DisableSwoopHitbox();
        UpdateFacing(travelDirection);
        PlayState(RecoverState);
    }

    private void Recover()
    {
        Vector2 nextPosition = Vector2.MoveTowards(
            rb.position,
            recoveryTarget,
            recoverySpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(nextPosition);

        if (Vector2.Distance(nextPosition, recoveryTarget) > 0.01f)
        {
            return;
        }

        // Resume horizontal flight toward the opposite side.
        rb.position = recoveryTarget;
        direction = travelDirection > 0 ? -1 : 1;
        ReturnToFlight();
    }

    private void ReturnToFlight()
    {
        state = OwlState.Flying;
        nextAttackTime = Time.time + attackCooldown;

        DisableSwoopHitbox();
        UpdateFacing(direction);
        PlayState(FlyState);
    }

    private void MoveHorizontally(float speed)
    {
        // Turn around at either end of the upper flight path.
        if (rb.position.x <= leftCorner.x)
        {
            direction = 1;
        }
        else if (rb.position.x >= rightCorner.x)
        {
            direction = -1;
        }

        float nextX = rb.position.x +
                      direction * speed * Time.fixedDeltaTime;

        nextX = Mathf.Clamp(nextX, leftCorner.x, rightCorner.x);
        rb.MovePosition(new Vector2(nextX, flightY));
        UpdateFacing(direction);
    }

    private Vector2 QuadraticBezier(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float t)
    {
        float inverseT = 1f - t;

        return inverseT * inverseT * start +
               2f * inverseT * t * control +
               t * t * end;
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

    private void UpdateFacing(int facingDirection)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = spriteFacesRight
            ? facingDirection < 0
            : facingDirection > 0;
    }

    private void PlayState(int stateHash)
    {
        if (animator != null && animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
        }
    }

    private void DisableSwoopHitbox()
    {
        if (swoopHitbox != null)
        {
            swoopHitbox.enabled = false;
        }
    }

    private void OnDisable()
    {
        // Stop the swoop when disabled.
        DisableSwoopHitbox();

        if (rb != null && (health == null || !health.IsDead))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Show the Owl's upper horizontal flight path.
        Vector3 left = transform.position;
        Vector3 right = transform.position;

        if (Application.isPlaying)
        {
            left = leftCorner;
            right = rightCorner;
        }
        else if (startingSide >= 0)
        {
            left += Vector3.left * cornerDistance;
        }
        else
        {
            right += Vector3.right * cornerDistance;
        }

        Gizmos.DrawLine(left, right);
    }
}
