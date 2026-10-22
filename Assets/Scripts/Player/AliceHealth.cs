using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AliceMovement))]
public class AliceHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invulnerabilityTime = 1f;

    [Header("Hit Reaction")]
    [SerializeField] private float controlLockTime = 0.2f;
    [SerializeField] private Vector2 knockback = new Vector2(3.5f, 4f);

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private AliceMovement movement;
    private AliceCombat combat;
    private float invulnerableUntil;
    private Coroutine recoveryRoutine;

    public int CurrentHealth { get; private set; }
    public bool IsDefeated { get; private set; }

    private static readonly int AliceIdleState =
        Animator.StringToHash("Base Layer.AliceIdle");

    private static readonly int AliceRunState =
        Animator.StringToHash("Base Layer.AliceRun");

    private static readonly int AliceJumpState =
        Animator.StringToHash("Base Layer.AliceJump");

    private static readonly int AliceHurtState =
        Animator.StringToHash("Base Layer.AliceHurt");

    private static readonly int AliceDefeatState =
        Animator.StringToHash("Base Layer.AliceDefeat");

    private void Awake()
    {
        // Get the required components.
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<AliceMovement>();
        combat = GetComponent<AliceCombat>();

        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        // Ignore damage when unavailable.
        if (damage <= 0 ||
            IsDefeated ||
            Time.time < invulnerableUntil)
        {
            return;
        }

        // Remove health.
        CurrentHealth = Mathf.Max(
            0,
            CurrentHealth - damage
        );

        // Stop the current attack.
        combat?.InterruptAttack();

        // Defeat Alice at zero health.
        if (CurrentHealth <= 0)
        {
            Defeat();
            return;
        }

        // Start invulnerability.
        invulnerableUntil =
            Time.time + invulnerabilityTime;

        // Lock controls during the hit reaction.
        movement.LockControls(controlLockTime);

        // Knock Alice away from the damage source.
        float knockbackDirection =
            transform.position.x >= sourcePosition.x
            ? 1f
            : -1f;

        rb.linearVelocity = new Vector2(
            knockback.x * knockbackDirection,
            knockback.y
        );

        // Play the hurt animation.
        if (animator != null &&
            animator.HasState(0, AliceHurtState))
        {
            animator.Play(
                AliceHurtState,
                0,
                0f
            );
        }

        // Restart the recovery timer.
        if (recoveryRoutine != null)
        {
            StopCoroutine(recoveryRoutine);
        }

        recoveryRoutine =
            StartCoroutine(RecoverFromHit());
    }

    private IEnumerator RecoverFromHit()
    {
        // Wait for the hit reaction to finish.
        yield return new WaitForSeconds(
            controlLockTime
        );

        recoveryRoutine = null;

        if (IsDefeated || animator == null)
        {
            yield break;
        }

        // Return to the correct movement animation.
        if (!movement.IsGrounded &&
            animator.HasState(0, AliceJumpState))
        {
            animator.Play(
                AliceJumpState,
                0,
                0f
            );
        }
        else if (movement.IsMoving &&
                 animator.HasState(0, AliceRunState))
        {
            animator.Play(
                AliceRunState,
                0,
                0f
            );
        }
        else if (animator.HasState(0, AliceIdleState))
        {
            animator.Play(
                AliceIdleState,
                0,
                0f
            );
        }
    }

    private void Defeat()
    {
        IsDefeated = true;

        // Stop movement and combat.
        movement.LockControls(
            float.PositiveInfinity
        );

        rb.linearVelocity = Vector2.zero;

        if (combat != null)
        {
            combat.enabled = false;
        }

        // Play the defeat animation.
        if (animator != null &&
            animator.HasState(0, AliceDefeatState))
        {
            animator.Play(
                AliceDefeatState,
                0,
                0f
            );
        }
    }
}