using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer))]
public class Background : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform target;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Parallax")]
    [SerializeField] private bool lockToCamera;
    [SerializeField] private float horizontalStrength = 0.12f;
    [SerializeField] private float verticalStrength = 0.1f;
    [SerializeField] private Vector2 maxOffset = new Vector2(0.5f, 0.35f);
    [SerializeField] private float smoothing = 8f;

    private Vector2 currentOffset;
    private float targetStartY;
    private float previousCameraX;
    private float fixedZ;

    private void Start()
    {
        ResolveReferences();

        if (targetCamera == null || spriteRenderer == null)
        {
            Debug.LogError(
                $"{name} is missing its parallax references."
            );

            enabled = false;
            return;
        }

        fixedZ = transform.position.z;
        previousCameraX = targetCamera.transform.position.x;
        targetStartY = target != null
            ? target.position.y
            : targetCamera.transform.position.y;

        FitToCamera();
        FollowCamera(Vector2.zero);
    }

    private void LateUpdate()
    {
        if (lockToCamera)
        {
            currentOffset = Vector2.zero;
            FollowCamera(Vector2.zero);
            previousCameraX = targetCamera.transform.position.x;
            return;
        }

        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        float cameraX = targetCamera.transform.position.x;
        float cameraVelocityX =
            (cameraX - previousCameraX) / deltaTime;

        previousCameraX = cameraX;

        float targetRelativeX = target != null
            ? target.position.x - cameraX
            : 0f;

        float targetRelativeY = target != null
            ? target.position.y - targetStartY
            : 0f;

        Vector2 desiredOffset = new Vector2(
            -cameraVelocityX * horizontalStrength +
                targetRelativeX * horizontalStrength * 0.25f,
            targetRelativeY * verticalStrength
        );

        desiredOffset.x = Mathf.Clamp(
            desiredOffset.x,
            -maxOffset.x,
            maxOffset.x
        );

        desiredOffset.y = Mathf.Clamp(
            desiredOffset.y,
            -maxOffset.y,
            maxOffset.y
        );

        float blend = 1f - Mathf.Exp(-smoothing * deltaTime);

        currentOffset = Vector2.Lerp(
            currentOffset,
            desiredOffset,
            blend
        );

        FollowCamera(currentOffset);
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (!lockToCamera && target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void FitToCamera()
    {
        if (!targetCamera.orthographic || spriteRenderer.sprite == null)
        {
            return;
        }

        Vector3 spriteBounds = spriteRenderer.sprite.bounds.size;
        Vector2 spriteSize = new Vector2(
            spriteBounds.x,
            spriteBounds.y
        );

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;

        float requiredWidth = viewWidth + maxOffset.x * 2f;
        float requiredHeight = viewHeight + maxOffset.y * 2f;

        float scale = Mathf.Max(
            requiredWidth / spriteSize.x,
            requiredHeight / spriteSize.y
        );

        transform.localScale = new Vector3(
            scale,
            scale,
            1f
        );
    }

    private void FollowCamera(Vector2 offset)
    {
        Vector3 cameraPosition = targetCamera.transform.position;

        transform.position = new Vector3(
            cameraPosition.x + offset.x,
            cameraPosition.y + offset.y,
            fixedZ
        );
    }
}
