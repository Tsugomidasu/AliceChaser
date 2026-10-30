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

    [Header("Audio")]
    [SerializeField] private AudioClip attack01Clip;
    [SerializeField] private AudioClip attack02Clip;
    [SerializeField] private AudioClip attack03Clip;
    [SerializeField] private AudioClip heavyChargeClip;
    [SerializeField] private AudioClip heavySwingClip;
    [SerializeField] private AudioClip heavyImpactClip;

    private AliceMovement movement;
    private InputAction attackAction;

    private int currentAttack;
    private int queuedAttacks;

    private bool waitingForComboInput;
    private float comboWindowEndTime;

    private bool isHeavyAttack;
    private bool isAirAttack;
    private bool hasUsedAirAttack;
    private bool heavyImpactPlayed;

    private readonly HashSet<UnityEngine.Object> damagedThisAttack = new();

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

        // Heavy reuses Attack 3's hitbox.
        if (heavyHitbox == null)
        {
            heavyHitbox = attack3Hitbox;
        }

        if (inputActions != null)
        {
            attackAction = inputActions.FindAction("Player/Attack");
        }

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
        if (movement.IsGrounded)
        {
            hasUsedAirAttack = false;
        }

        if (waitingForComboInput &&
            Time.time >= comboWindowEndTime)
        {
            EndAttack();
        }
    }

    private void AttackStarted(InputAction.CallbackContext context)
    {
        if (context.interaction is SlowTapInteraction &&
            !IsAttacking &&
            movement.IsGrounded &&
            movement.CanAct)
        {
            IsCharging = true;
            GameAudio.Play(heavyChargeClip);
        }
    }

    private void AttackPerformed(InputAction.CallbackContext context)
    {
        if (!movement.CanAct)
        {
            return;
        }

        if (context.interaction is SlowTapInteraction)
        {
            IsCharging = false;

            if (!IsAttacking && movement.IsGrounded)
            {
                StartHeavyAttack();
            }

            return;
        }

        if (context.interaction is TapInteraction)
        {
            IsCharging = false;
            ReadLightAttack();
        }
    }

    private void AttackCanceled(InputAction.CallbackContext context)
    {
        if (context.interaction is SlowTapInteraction)
        {
            IsCharging = false;
        }
    }

    private void ReadLightAttack()
    {
        // Start, continue, or buffer the grounded combo from the same input.
        if (!movement.CanAct)
        {
            return;
        }

        if (!IsAttacking)
        {
            if (movement.IsGrounded)
            {
                StartLightAttack(1);
            }
            // Limit Alice to one basic attack per jump.
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

        if (waitingForComboInput && currentAttack < 3)
        {
            StartLightAttack(currentAttack + 1);
            return;
        }

        if (currentAttack + queuedAttacks < 3)
        {
            queuedAttacks++;
        }
    }

    private void StartLightAttack(int attackNumber)
    {
        damagedThisAttack.Clear();

        IsAttacking = true;
        isHeavyAttack = false;
        isAirAttack = false;
        currentAttack = attackNumber;

        waitingForComboInput = false;

        switch (currentAttack)
        {
            case 1:
                animator.Play(AliceAttack1State, 0, 0f);
                GameAudio.Play(attack01Clip);
                break;

            case 2:
                animator.Play(AliceAttack2State, 0, 0f);
                GameAudio.Play(attack02Clip);
                break;

            case 3:
                animator.Play(AliceAttack3State, 0, 0f);
                GameAudio.Play(attack03Clip);
                break;
        }
    }

    private void StartAirAttack()
    {
        damagedThisAttack.Clear();

        IsAttacking = true;
        isHeavyAttack = false;
        isAirAttack = true;
        hasUsedAirAttack = true;
        currentAttack = 1;
        queuedAttacks = 0;

        waitingForComboInput = false;

        animator.Play(AliceAttack1State, 0, 0f);
        GameAudio.Play(attack01Clip);
    }

    private void StartHeavyAttack()
    {
        // Reuse Attack 3 visually while keeping Heavy damage and state separate.
        damagedThisAttack.Clear();

        IsAttacking = true;
        IsCharging = false;

        isHeavyAttack = true;
        isAirAttack = false;
        currentAttack = 3;
        queuedAttacks = 0;

        waitingForComboInput = false;
        heavyImpactPlayed = false;

        animator.Play(AliceAttack3State, 0, 0f);
        GameAudio.Play(heavySwingClip);
    }

    public void AttackHit()
    {
        if (isHeavyAttack)
        {
            CheckHitbox(
                heavyHitbox,
                heavyDamage
            );

            return;
        }

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
            // Check deflectable rocks before damage targets.
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

            EnemyHealth enemy =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemy != null)
            {
                // Damage each target once per attack.
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

                    PlayHeavyImpactOnce();
                }

                continue;
            }

            DestructibleObject destructible =
                hit.GetComponentInParent<DestructibleObject>();

            if (destructible != null &&
                damagedThisAttack.Add(destructible))
            {
                destructible.TakeDamage(damage);
                PlayHeavyImpactOnce();
            }
        }
    }

    private void PlayHeavyImpactOnce()
    {
        if (!isHeavyAttack || heavyImpactPlayed)
        {
            return;
        }

        heavyImpactPlayed = true;
        GameAudio.Play(heavyImpactClip);
    }

    public void AttackFinished()
    {
        // Continue buffered combo input or return control when the animation ends.
        if (isAirAttack)
        {
            EndAttack();
            return;
        }

        if (isHeavyAttack)
        {
            EndAttack();
            return;
        }

        if (queuedAttacks > 0 && currentAttack < 3)
        {
            queuedAttacks--;
            StartLightAttack(currentAttack + 1);
            return;
        }

        if (currentAttack < 3)
        {
            StartComboWindow();
            return;
        }

        EndAttack();
    }

    public void InterruptAttack()
    {
        IsAttacking = false;
        IsCharging = false;

        currentAttack = 0;
        queuedAttacks = 0;

        waitingForComboInput = false;
        isHeavyAttack = false;
        isAirAttack = false;
        heavyImpactPlayed = false;

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
