using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class Tutorial: MonoBehaviour
{
    private enum Lesson
    {
        None,
        Move,
        Jump,
        Attack,
        Heavy
    }

    [Header("Scene")]
    [SerializeField] private Transform alice;
    [SerializeField] private GameUIController gameUI;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Move Lesson")]
    [SerializeField] private float movePromptDistance = 0f;
    [SerializeField] private float moveDismissDistance = 5f;

    [Header("Jump Lesson")]
    [SerializeField] private float jumpPromptDistance = 7.4f;
    [SerializeField] private float jumpDismissDistance = 12.5f;

    [Header("Attack Lesson")]
    [SerializeField] private float attackPromptDistance = 15.2f;
    [SerializeField] private float attackDismissDistance = 20.5f;

    [Header("Heavy Lesson")]
    [SerializeField] private float heavyPromptDistance = 49.5f;
    [SerializeField] private float heavyDismissDistance = 55.5f;

    [Header("Timing")]
    [SerializeField] private float movePromptDelay = 0.1f;
    [SerializeField] private float hintDelay = 0.75f;
    [SerializeField] private float heavyPromptDelay = 0.1f;

    private AliceMovement aliceMovement;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;

    private float levelStartX;
    private float promptTime;

    private Lesson pendingLesson;
    private Lesson activeLesson;

    private bool moveResolved;
    private bool jumpResolved;
    private bool attackResolved;
    private bool heavyResolved;

    private void Awake()
    {
        if (alice == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                alice = player.transform;
            }
        }

        if (gameUI == null)
        {
            gameUI = FindAnyObjectByType<GameUIController>();
        }

        if (alice != null)
        {
            levelStartX = alice.position.x;
            aliceMovement = alice.GetComponent<AliceMovement>();
        }

        if (inputActions != null)
        {
            SettingsMenu.ApplySavedBindings(inputActions);
            moveAction = inputActions.FindAction("Player/Move");
            jumpAction = inputActions.FindAction("Player/Jump");
            attackAction = inputActions.FindAction("Player/Attack");
        }
    }

    private void OnEnable()
    {
        SettingsMenu.BindingsChanged += RefreshActivePrompt;

        if (jumpAction != null)
        {
            jumpAction.performed += JumpPerformed;
        }

        if (attackAction != null)
        {
            attackAction.performed += AttackPerformed;
        }
    }

    private void OnDisable()
    {
        SettingsMenu.BindingsChanged -= RefreshActivePrompt;

        if (jumpAction != null)
        {
            jumpAction.performed -= JumpPerformed;
        }

        if (attackAction != null)
        {
            attackAction.performed -= AttackPerformed;
        }

        gameUI?.HideTutorialPrompt();
    }

    private void Start()
    {
        if (alice == null)
        {
            Debug.LogError("Tutorial Controller is missing Alice.");
        }

        if (gameUI == null)
        {
            Debug.LogError("Tutorial Controller is missing Game UI.");
        }

        if (inputActions == null ||
            moveAction == null ||
            jumpAction == null ||
            attackAction == null)
        {
            Debug.LogError(
                "Tutorial Controller is missing the required input actions."
            );
        }
    }

    private void Update()
    {
        if (alice == null || gameUI == null)
        {
            return;
        }

        // Advance lessons as Alice reaches each tutorial area.
        float progress = alice.position.x - levelStartX;

        if (!moveResolved)
        {
            if (aliceMovement != null && !aliceMovement.CanAct)
            {
                return;
            }

            // Skip the movement hint after valid movement input.
            if ((aliceMovement != null && aliceMovement.IsMoving) ||
                (moveAction != null && Mathf.Abs(moveAction.ReadValue<float>()) > 0.01f))
            {
                ResolveLesson(Lesson.Move);
                return;
            }

            UpdateLesson(
                Lesson.Move,
                progress,
                movePromptDistance,
                moveDismissDistance,
                movePromptDelay,
                GetLessonMessage(Lesson.Move)
            );

            return;
        }

        if (!jumpResolved)
        {
            UpdateLesson(
                Lesson.Jump,
                progress,
                jumpPromptDistance,
                jumpDismissDistance,
                hintDelay,
                GetLessonMessage(Lesson.Jump)
            );

            return;
        }

        if (!attackResolved)
        {
            UpdateLesson(
                Lesson.Attack,
                progress,
                attackPromptDistance,
                attackDismissDistance,
                hintDelay,
                GetLessonMessage(Lesson.Attack)
            );

            return;
        }

        if (!heavyResolved)
        {
            UpdateLesson(
                Lesson.Heavy,
                progress,
                heavyPromptDistance,
                heavyDismissDistance,
                heavyPromptDelay,
                GetLessonMessage(Lesson.Heavy)
            );
        }
    }

    private string GetLessonMessage(Lesson lesson)
    {
        switch (lesson)
        {
            case Lesson.Move:
                return $"{SettingsMenu.GetMoveLeftDisplay(inputActions)} / {SettingsMenu.GetMoveRightDisplay(inputActions)}    ← / →\nMOVE";

            case Lesson.Jump:
                return $"{SettingsMenu.GetJumpDisplay(inputActions)}\nJUMP";

            case Lesson.Attack:
                return $"{SettingsMenu.GetAttackDisplay(inputActions)}\nATTACK";

            case Lesson.Heavy:
                return $"HOLD {SettingsMenu.GetAttackDisplay(inputActions)}\nHEAVY ATTACK";

            default:
                return string.Empty;
        }
    }

    private void RefreshActivePrompt()
    {
        if (activeLesson != Lesson.None && gameUI != null)
        {
            gameUI.ShowTutorialPrompt(GetLessonMessage(activeLesson));
        }
    }

    private void UpdateLesson(
        Lesson lesson,
        float progress,
        float promptDistance,
        float dismissDistance,
        float delay,
        string message)
    {
        if (progress > dismissDistance)
        {
            ResolveLesson(lesson);
            return;
        }

        if (progress < promptDistance)
        {
            return;
        }

        if (activeLesson == lesson)
        {
            return;
        }

        // Delay hints to prevent trigger overlap.
        if (pendingLesson != lesson)
        {
            pendingLesson = lesson;
            promptTime = Time.time + delay;
            return;
        }

        if (Time.time < promptTime)
        {
            return;
        }

        pendingLesson = Lesson.None;
        activeLesson = lesson;
        gameUI.ShowTutorialPrompt(message);
    }

    private void JumpPerformed(InputAction.CallbackContext context)
    {
        ResolveLesson(Lesson.Jump);
    }

    private void AttackPerformed(InputAction.CallbackContext context)
    {
        // SlowTap distinguishes Heavy from a normal attack.
        if (context.interaction is SlowTapInteraction)
        {
            ResolveLesson(Lesson.Heavy);
            return;
        }

        if (context.interaction is TapInteraction)
        {
            ResolveLesson(Lesson.Attack);
        }
    }

    private void ResolveLesson(Lesson lesson)
    {
        // Mark completed lessons so their prompts cannot reappear.
        switch (lesson)
        {
            case Lesson.Move:
                moveResolved = true;
                break;

            case Lesson.Jump:
                jumpResolved = true;
                break;

            case Lesson.Attack:
                attackResolved = true;
                break;

            case Lesson.Heavy:
                heavyResolved = true;
                break;
        }

        if (pendingLesson == lesson)
        {
            pendingLesson = Lesson.None;
        }

        if (activeLesson == lesson)
        {
            activeLesson = Lesson.None;
            gameUI?.HideTutorialPrompt();
        }
    }
}
