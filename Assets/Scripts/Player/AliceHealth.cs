using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AliceMovement))]
public class AliceHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invulnerabilityTime = 0.75f;

    [Header("Hit Reaction")]
    [SerializeField] private float controlLockTime = 0.2f;
    [SerializeField] private Vector2 knockback = new Vector2(3.5f, 4f);

    [Header("Damage Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashInterval = 0.075f;
    [Range(0f, 1f)]
    [SerializeField] private float flashAlpha = 0.35f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip defeatClip;

    private Rigidbody2D rb;
    private AliceMovement movement;
    private AliceCombat combat;
    private float invulnerableUntil;
    private Color normalSpriteColor = Color.white;
    private Coroutine recoveryRoutine;
    private Coroutine flashRoutine;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDefeated { get; private set; }
    public bool IsInvulnerable =>
        !IsDefeated && Time.time < invulnerableUntil;

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

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            normalSpriteColor = spriteRenderer.color;
        }

        CurrentHealth = maxHealth;
    }

    private void OnDisable()
    {
        RestoreSpriteVisibility();
    }

    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        // Ignore damage when unavailable.
        if (damage <= 0 ||
            IsDefeated ||
            IsInvulnerable)
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

        GameAudio.Play(hurtClip);

        // Start invulnerability.
        invulnerableUntil =
            Time.time + invulnerabilityTime;

        StartDamageFlash();

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

    private void StartDamageFlash()
    {
        if (spriteRenderer == null ||
            invulnerabilityTime <= 0f)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashDuringInvulnerability());
    }

    private IEnumerator FlashDuringInvulnerability()
    {
        bool faded = false;
        float interval = Mathf.Max(0.01f, flashInterval);

        while (IsInvulnerable)
        {
            faded = !faded;
            SetSpriteAlpha(
                faded
                ? normalSpriteColor.a * flashAlpha
                : normalSpriteColor.a
            );

            yield return new WaitForSeconds(interval);
        }

        RestoreSpriteVisibility();
        flashRoutine = null;
    }

    private void SetSpriteAlpha(float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = normalSpriteColor;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }

    private void RestoreSpriteVisibility()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalSpriteColor;
        }
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
        GameAudio.Play(defeatClip);
        invulnerableUntil = 0f;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreSpriteVisibility();

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