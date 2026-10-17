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
    [SerializeField] private float comboInputWindow = 0.25f;

    [Header("Heavy Attack")]
    [SerializeField] private Transform heavyHitbox;
    [SerializeField] private int heavyDamage = 4;

    [Header("Damage")]
    [SerializeField] private LayerMask damageLayers;

    private AliceMovement movement;
    private InputAction attackAction;

    private int currentAttack;
    private int queuedAttacks;

    private bool waitingForComboInput;
    private float comboWindowEndTime;

    private bool isHeavyAttack;

    // Track objects already damaged by the current attack.
    private readonly HashSet<DestructibleObject> damagedThisAttack = new();

    public bool IsAttacking { get; private set; }
    public bool IsCharging { get; private set; }
    public bool IsHeavyAttack => isHeavyAttack;

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

        // Find the Attack action.
        if (inputActions != null)
        {
            attackAction = inputActions.FindAction("Player/Attack");
        }
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
            movement.IsGrounded)
        {
            IsCharging = true;
        }
    }

    private void AttackPerformed(InputAction.CallbackContext context)
    {
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
        // Start Attack 1.
        if (!IsAttacking)
        {
            if (movement.IsGrounded)
            {
                StartLightAttack(1);
            }

            return;
        }

        if (isHeavyAttack)
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

    private void StartHeavyAttack()
    {
        // Reset damaged objects for the new attack.
        damagedThisAttack.Clear();

        IsAttacking = true;
        IsCharging = false;

        isHeavyAttack = true;
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
        Vector3 scale = hitbox.lossyScale;

        Vector2 size = new Vector2(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y)
        );

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            hitbox.position,
            size,
            hitbox.eulerAngles.z,
            damageLayers
        );

        foreach (Collider2D hit in hits)
        {
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

    private void StartComboWindow()
    {
        waitingForComboInput = true;
        comboWindowEndTime = Time.time + comboInputWindow;
    }

    private void EndAttack()
    {
        IsAttacking = false;
        IsCharging = false;

        currentAttack = 0;
        queuedAttacks = 0;

        waitingForComboInput = false;
        isHeavyAttack = false;

        damagedThisAttack.Clear();

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