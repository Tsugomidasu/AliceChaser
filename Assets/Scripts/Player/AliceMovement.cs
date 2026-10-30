using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class AliceMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 7.5f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;

    private Rigidbody2D rb;
    private AliceCombat combat;

    private InputAction moveAction;
    private InputAction jumpAction;

    private float moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    private bool wasGrounded;
    private bool groundStateInitialized;
    private float controlsLockedUntil;

    public bool IsGrounded => isGrounded;
    public bool IsMoving => moveInput != 0f;
    public bool CanAct => Time.time >= controlsLockedUntil;
    public float MoveInput => moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        combat = GetComponent<AliceCombat>();

        if (inputActions != null)
        {
            moveAction = inputActions.FindAction("Player/Move");
            jumpAction = inputActions.FindAction("Player/Jump");
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
    }

    private void Start()
    {
        if (inputActions == null)
        {
            Debug.LogError(
                "Alice Movement is missing the Input Actions asset."
            );
        }
        else if (moveAction == null || jumpAction == null)
        {
            Debug.LogError(
                "Alice Input Actions is missing Player/Move or Player/Jump."
            );
        }
    }

    private void Update()
    {
        moveInput = 0f;

        if (CanAct && moveAction != null)
        {
            moveInput = moveAction.ReadValue<float>();
        }

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        ) != null;

        // Play the landing sound only on an air-to-ground transition.
        if (groundStateInitialized && isGrounded && !wasGrounded)
        {
            GameAudio.Play(landClip);
        }

        wasGrounded = isGrounded;
        groundStateInitialized = true;

        if (!CanAct)
        {
            jumpRequested = false;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsGrounded", isGrounded);
            return;
        }

        // Block jumping during attack states.
        if (jumpAction != null &&
            jumpAction.WasPressedThisFrame() &&
            isGrounded &&
            (combat == null ||
            (!combat.IsAttacking &&
             !combat.IsCharging &&
             !combat.IsHoldingAttack)))
        {
            jumpRequested = true;
            animator.SetTrigger("Jump");
        }

        animator.SetBool("IsMoving", moveInput != 0f);
        animator.SetBool("IsGrounded", isGrounded);
    }

    private void FixedUpdate()
    {
        if (!CanAct)
        {
            return;
        }

        rb.linearVelocityX = moveInput * moveSpeed;

        if (jumpRequested)
        {
            rb.linearVelocityY = jumpSpeed;
            GameAudio.Play(jumpClip);
            jumpRequested = false;
        }
    }

    public void LockControls(float duration)
    {
        // Extend an existing lock instead of shortening a longer one.
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

        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
