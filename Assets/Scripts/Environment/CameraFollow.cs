using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Framing")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float preferredScreenX = 0.4f;
    [Range(0f, 0.4f)]
    [SerializeField] private float deadZoneWidth = 0.1f;
    [Range(0.2f, 0.9f)]
    [SerializeField] private float softZoneWidth = 0.5f;

    [Header("Follow")]
    [SerializeField] private float horizontalDamping = 0.2f;
    [SerializeField] private float lookAheadTime = 0.25f;
    [SerializeField] private float maxLookAhead = 1.5f;
    [SerializeField] private float lookAheadSmoothing = 0.15f;

    [Header("Bounds")]
    [SerializeField] private float minimumCameraX;
    [SerializeField] private float levelEndX = 95f;

    private Camera cameraComponent;
    private float previousTargetX;
    private float cameraVelocityX;
    private float lookAheadX;
    private float lookAheadVelocityX;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }

        ResetTracking();
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError(
                "Camera Follow is missing a target."
            );
        }
    }

    public void ResetTracking()
    {
        if (target == null)
        {
            return;
        }

        previousTargetX = target.position.x;
        cameraVelocityX = 0f;
        lookAheadX = 0f;
        lookAheadVelocityX = 0f;
    }

    private void LateUpdate()
    {
        if (target == null || !cameraComponent.orthographic)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        // Offset framing toward Alice's current movement direction.
        UpdateLookAhead(deltaTime);

        float screenWidth =
            cameraComponent.orthographicSize *
            cameraComponent.aspect * 2f;

        float cameraX = transform.position.x;
        float framedTargetX = target.position.x + lookAheadX;

        float targetScreenX =
            0.5f + (framedTargetX - cameraX) / screenWidth;

        float halfDeadZone = deadZoneWidth * 0.5f;
        float deadZoneLeft = preferredScreenX - halfDeadZone;
        float deadZoneRight = preferredScreenX + halfDeadZone;

        float desiredCameraX = cameraX;

        // Start following outside the dead zone.
        if (targetScreenX < deadZoneLeft)
        {
            desiredCameraX =
                framedTargetX -
                (deadZoneLeft - 0.5f) * screenWidth;
        }
        else if (targetScreenX > deadZoneRight)
        {
            desiredCameraX =
                framedTargetX -
                (deadZoneRight - 0.5f) * screenWidth;
        }

        float nextCameraX = Mathf.SmoothDamp(
            cameraX,
            desiredCameraX,
            ref cameraVelocityX,
            horizontalDamping
        );

        float halfSoftZone = softZoneWidth * 0.5f;
        float softZoneLeft = preferredScreenX - halfSoftZone;
        float softZoneRight = preferredScreenX + halfSoftZone;

        float actualScreenX =
            0.5f +
            (target.position.x - nextCameraX) / screenWidth;

        // Clamp Alice to the soft zone.
        if (actualScreenX < softZoneLeft)
        {
            nextCameraX =
                target.position.x -
                (softZoneLeft - 0.5f) * screenWidth;
        }
        else if (actualScreenX > softZoneRight)
        {
            nextCameraX =
                target.position.x -
                (softZoneRight - 0.5f) * screenWidth;
        }

        float halfScreenWidth = screenWidth * 0.5f;
        float maximumCameraX = Mathf.Max(
            minimumCameraX,
            levelEndX - halfScreenWidth
        );

        nextCameraX = Mathf.Clamp(
            nextCameraX,
            minimumCameraX,
            maximumCameraX
        );

        transform.position = new Vector3(
            nextCameraX,
            transform.position.y,
            transform.position.z
        );
    }

    private void UpdateLookAhead(float deltaTime)
    {
        float targetVelocityX =
            (target.position.x - previousTargetX) / deltaTime;

        previousTargetX = target.position.x;

        float desiredLookAhead = Mathf.Clamp(
            targetVelocityX * lookAheadTime,
            -maxLookAhead,
            maxLookAhead
        );

        lookAheadX = Mathf.SmoothDamp(
            lookAheadX,
            desiredLookAhead,
            ref lookAheadVelocityX,
            lookAheadSmoothing
        );
    }
}
