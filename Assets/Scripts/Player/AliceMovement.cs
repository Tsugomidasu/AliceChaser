using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class AliceMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 7.5f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private AliceCombat combat;

    private float moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    private float controlsLockedUntil;

    public bool IsGrounded => isGrounded;
    public bool IsMoving => moveInput != 0f;
    public bool CanAct => Time.time >= controlsLockedUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        combat = GetComponent<AliceCombat>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // Read movement.
        moveInput = 0f;

        if (CanAct)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                moveInput = -1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                moveInput = 1f;
            }
        }

        // Check if Alice is on the ground.
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        ) != null;

        // Stop input while controls are locked.
        if (!CanAct)
        {
            jumpRequested = false;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsGrounded", isGrounded);
            return;
        }

        // Jump with Space.
        if (keyboard.spaceKey.wasPressedThisFrame &&
            isGrounded &&
            (combat == null ||
            (!combat.IsAttacking &&
             !combat.IsCharging &&
             !combat.IsHoldingAttack)))
        {
            jumpRequested = true;
            animator.SetTrigger("Jump");
        }

        // Update movement animations.
        animator.SetBool("IsMoving", moveInput != 0f);
        animator.SetBool("IsGrounded", isGrounded);
    }

    private void FixedUpdate()
    {
        if (!CanAct)
        {
            return;
        }

        // Move Alice.
        rb.linearVelocityX = moveInput * moveSpeed;

        // Jump.
        if (jumpRequested)
        {
            rb.linearVelocityY = jumpSpeed;
            jumpRequested = false;
        }
    }

    public void LockControls(float duration)
    {
        // Extend the current control lock if needed.
        controlsLockedUntil = Mathf.Max(
            controlsLockedUntil,
            Time.time + duration
        );

        moveInput = 0f;
        jumpRequested = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        // Show the ground check.
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}