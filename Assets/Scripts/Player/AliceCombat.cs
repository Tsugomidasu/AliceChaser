using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

[RequireComponent(typeof(AliceMovement))]
public class AliceCombat : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Attack 1")]
    [SerializeField] private Transform attack1Hitbox;
    [SerializeField] private int attack1Damage = 1;

    [Header("Attack 2")]
    [SerializeField] private Transform attack2Hitbox;
    [SerializeField] private int attack2Damage = 1;

    [Header("Attack 3")]
    [SerializeField] private Transform attack3Hitbox;
    [SerializeField] private int attack3Damage = 2;

    [Header("Combo")]
    [SerializeField] private float comboInputWindow = 0.50f;

    [Header("Heavy Attack")]
    [SerializeField] private Transform heavyHitbox;
    [SerializeField] private float heavyChargeDuration = 0.6f;
    [SerializeField] private int heavyDamage = 6;

    [Header("Damage")]
    [SerializeField] private LayerMask damageLayers;

    [Header("Enemy Knockback")]
    [SerializeField] private float lightKnockbackSpeed = 2f;
    [SerializeField] private float heavyKnockbackSpeed = 5f;

    private AliceMovement movement;
    private InputAction attackAction;

    private int currentAttack;
    private int queuedAttacks;

    private bool waitingForComboInput;
    private float comboWindowEndTime;

    private bool isHeavyAttack;
    private bool isAirAttack;
    private bool hasUsedAirAttack;

    // Track objects already damaged by the current attack.
    private readonly HashSet<UnityEngine.Object> damagedThisAttack = new();

    // Reuse attack query results.
    private readonly List<Collider2D> hitResults = new();
    private ContactFilter2D damageFilter;

    public bool IsAttacking { get; private set; }
    public bool IsCharging { get; private set; }
    public bool IsHeavyAttack => isHeavyAttack;
    public float HeavyChargeDuration => heavyChargeDuration;

    public bool IsHoldingAttack =>
        attackAction != null &&
        attackAction.IsPressed();

    private static readonly int AliceIdleState =
        Animator.StringToHash("Base Layer.AliceIdle");

    private static readonly int AliceRunState =
        Animator.StringToHash("Base Layer.AliceRun");

    private static readonly int AliceJumpState =
        Animator.StringToHash("Base Layer.AliceJump");

    private static readonly int AliceAttack1State =
        Animator.StringToHash("Base Layer.AliceAttack1");

    private static readonly int AliceAttack2State =
        Animator.StringToHash("Base Layer.AliceAttack2");

    private static readonly int AliceAttack3State =
        Animator.StringToHash("Base Layer.AliceAttack3");

    private void Awake()
    {
        movement = GetComponent<AliceMovement>();

        // Heavy attack uses Attack 3's hitbox by default.
        if (heavyHitbox == null)
        {
            heavyHitbox = attack3Hitbox;
        }

        // Find the Attack action.
        if (inputActions != null)
        {
            attackAction = inputActions.FindAction("Player/Attack");
        }

        // Include trigger hurtboxes in attack checks.
        damageFilter = new ContactFilter2D();
        damageFilter.SetLayerMask(damageLayers);
        damageFilter.useTriggers = true;
    }

    private void OnEnable()
    {
        if (attackAction == null)
        {
            return;
        }

        attackAction.started += AttackStarted;
        attackAction.performed += AttackPerformed;
        attackAction.canceled += AttackCanceled;

        attackAction.Enable();
    }

    private void OnDisable()
    {
        if (attackAction == null)
        {
            return;
        }

        attackAction.started -= AttackStarted;
        attackAction.performed -= AttackPerformed;
        attackAction.canceled -= AttackCanceled;

        attackAction.Disable();
    }

    private void Start()
    {
        // Check the Attack action.
        if (inputActions == null)
        {
            Debug.LogError(
                "Alice Combat is missing the Input Actions asset."
            );
        }
        else if (attackAction == null)
        {
            Debug.LogError(
                "Alice Input Actions is missing Player/Attack."
            );
        }

        // Check the attack states.
        if (!animator.HasState(0, AliceAttack1State) ||
            !animator.HasState(0, AliceAttack2State) ||
            !animator.HasState(0, AliceAttack3State))
        {
            Debug.LogError(
                "Alice Animator is missing AliceAttack1, AliceAttack2, or AliceAttack3."
            );
        }
    }

    private void Update()
    {
        // Reset the air attack after Alice lands.
        if (movement.IsGrounded)
        {
            hasUsedAirAttack = false;
        }

        // End the combo window.
        if (waitingForComboInput &&
            Time.time >= comboWindowEndTime)
        {
            EndAttack();
        }
    }

    private void AttackStarted(InputAction.CallbackContext context)
    {
        // Start charging the heavy attack.
        if (context.interaction is SlowTapInteraction &&
            !IsAttacking &&
            movement.IsGrounded &&
            movement.CanAct)
        {
            IsCharging = true;
        }
    }

    private void AttackPerformed(InputAction.CallbackContext context)
    {
        // Ignore attacks while controls are locked.
        if (!movement.CanAct)
        {
            return;
        }

        // Use the heavy attack.
        if (context.interaction is SlowTapInteraction)
        {
            IsCharging = false;

            if (!IsAttacking && movement.IsGrounded)
            {
                StartHeavyAttack();
            }

            return;
        }

        // Use a normal attack.
        if (context.interaction is TapInteraction)
        {
            IsCharging = false;
            ReadLightAttack();
        }
    }

    private void AttackCanceled(InputAction.CallbackContext context)
    {
        // Cancel the charge.
        if (context.interaction is SlowTapInteraction)
        {
            IsCharging = false;
        }
    }

    private void ReadLightAttack()
    {
        if (!movement.CanAct)
        {
            return;
        }

        // Start Attack 1.
        if (!IsAttacking)
        {
            if (movement.IsGrounded)
            {
                StartLightAttack(1);
            }
            else if (!hasUsedAirAttack)
            {
                StartAirAttack();
            }

            return;
        }

        if (isHeavyAttack || isAirAttack)
        {
            return;
        }

        // Continue during the combo window.
        if (waitingForComboInput && currentAttack < 3)
        {
            StartLightAttack(currentAttack + 1);
            return;
        }

        // Queue the next attack.
        if (currentAttack + queuedAttacks < 3)
        {
            queuedAttacks++;
        }
    }

    private void StartLightAttack(int attackNumber)
    {
        // Reset damaged objects for the new attack.
        damagedThisAttack.Clear();

        IsAttacking = true;
        isHeavyAttack = false;
        isAirAttack = false;
        currentAttack = attackNumber;

        waitingForComboInput = false;

        // Play the current attack.
        switch (currentAttack)
        {
            case 1:
                animator.Play(AliceAttack1State, 0, 0f);
                break;

            case 2:
                animator.Play(AliceAttack2State, 0, 0f);
                break;

            case 3:
                animator.Play(AliceAttack3State, 0, 0f);
                break;
        }
    }

    private void StartAirAttack()
    {
        // Reset damaged objects for the air attack.
        damagedThisAttack.Clear();

        IsAttacking = true;
        isHeavyAttack = false;
        isAirAttack = true;
        hasUsedAirAttack = true;
        currentAttack = 1;
        queuedAttacks = 0;

        waitingForComboInput = false;

        // Air attack uses Attack 1.
        animator.Play(AliceAttack1State, 0, 0f);
    }

    private void StartHeavyAttack()
    {
        // Reset damaged objects for the new attack.
        damagedThisAttack.Clear();

        IsAttacking = true;
        IsCharging = false;

        isHeavyAttack = true;
        isAirAttack = false;
        currentAttack = 3;
        queuedAttacks = 0;

        waitingForComboInput = false;

        // Heavy attack uses Attack 3.
        animator.Play(AliceAttack3State, 0, 0f);
    }

    public void AttackHit()
    {
        // Check the heavy hitbox.
        if (isHeavyAttack)
        {
            CheckHitbox(
                heavyHitbox,
                heavyDamage
            );

            return;
        }

        // Check the current hitbox.
        switch (currentAttack)
        {
            case 1:
                CheckHitbox(
                    attack1Hitbox,
                    attack1Damage
                );
                break;

            case 2:
                CheckHitbox(
                    attack2Hitbox,
                    attack2Damage
                );
                break;

            case 3:
                CheckHitbox(
                    attack3Hitbox,
                    attack3Damage
                );
                break;
        }
    }

    private void CheckHitbox(
        Transform hitbox,
        int damage
    )
    {
        if (hitbox == null)
        {
            Debug.LogError(
                "Alice Combat is missing an attack hitbox."
            );
            return;
        }

        Vector3 scale = hitbox.lossyScale;

        Vector2 size = new Vector2(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y)
        );

        hitResults.Clear();

        Physics2D.OverlapBox(
            hitbox.position,
            size,
            hitbox.eulerAngles.z,
            damageFilter,
            hitResults
        );

        foreach (Collider2D hit in hitResults)
        {
            // Check for deflectable rocks first.
            RaccoonRock rock =
                hit.GetComponentInParent<RaccoonRock>();

            if (rock != null)
            {
                if (damagedThisAttack.Add(rock))
                {
                    rock.Deflect(transform);
                }

                continue;
            }

            // Check for enemies.
            EnemyHealth enemy =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemy != null)
            {
                // Prevent duplicate damage during the same attack.
                if (damagedThisAttack.Add(enemy))
                {
                    float knockbackSpeed = isHeavyAttack
                        ? heavyKnockbackSpeed
                        : lightKnockbackSpeed;

                    enemy.TakeDamage(
                        damage,
                        transform.position,
                        knockbackSpeed
                    );
                }

                continue;
            }

            // Check for destructible objects.
            DestructibleObject destructible =
                hit.GetComponentInParent<DestructibleObject>();

            // Prevent duplicate damage during the same attack.
            if (destructible != null &&
                damagedThisAttack.Add(destructible))
            {
                destructible.TakeDamage(damage);
            }
        }
    }

    public void AttackFinished()
    {
        // Finish the air attack.
        if (isAirAttack)
        {
            EndAttack();
            return;
        }

        // Finish the heavy attack.
        if (isHeavyAttack)
        {
            EndAttack();
            return;
        }

        // Continue a buffered combo.
        if (queuedAttacks > 0 && currentAttack < 3)
        {
            queuedAttacks--;
            StartLightAttack(currentAttack + 1);
            return;
        }

        // Wait briefly for the next combo input.
        if (currentAttack < 3)
        {
            StartComboWindow();
            return;
        }

        // Attack 3 ends the combo.
        EndAttack();
    }

    public void InterruptAttack()
    {
        // Clear the current attack state.
        IsAttacking = false;
        IsCharging = false;

        currentAttack = 0;
        queuedAttacks = 0;

        waitingForComboInput = false;
        isHeavyAttack = false;
        isAirAttack = false;

        damagedThisAttack.Clear();
    }

    private void StartComboWindow()
    {
        waitingForComboInput = true;
        comboWindowEndTime = Time.time + comboInputWindow;
    }

    private void EndAttack()
    {
        InterruptAttack();

        // Return to movement.
        if (!movement.IsGrounded)
        {
            animator.Play(AliceJumpState, 0, 0f);
        }
        else if (movement.IsMoving)
        {
            animator.Play(AliceRunState, 0, 0f);
        }
        else
        {
            animator.Play(AliceIdleState, 0, 0f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Show the attack hitboxes.
        DrawHitbox(attack1Hitbox);
        DrawHitbox(attack2Hitbox);
        DrawHitbox(attack3Hitbox);
        DrawHitbox(heavyHitbox);
    }

    private void DrawHitbox(Transform hitbox)
    {
        if (hitbox == null)
        {
            return;
        }

        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.matrix = hitbox.localToWorldMatrix;
        Gizmos.DrawWireCube(
            Vector3.zero,
            Vector3.one
        );

        Gizmos.matrix = previousMatrix;
    }
}