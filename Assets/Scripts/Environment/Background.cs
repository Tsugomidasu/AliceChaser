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
    [Range(0f, 0.15f)]
    [SerializeField] private float horizontalStrength = 0.03f;

    [SerializeField] private float verticalStrength = 0.14f;
    [SerializeField] private Vector2 maxOffset = new Vector2(2.6f, 0.45f);
    [SerializeField] private float smoothing = 7f;

    private Vector2 currentOffset;
    private float targetStartY;
    private float cameraStartX;
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
        cameraStartX = targetCamera.transform.position.x;
        targetStartY = target != null
            ? target.position.y
            : targetCamera.transform.position.y;

        FitToCamera();

        currentOffset = Vector2.zero;
        FollowCamera(currentOffset);
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        // Camera-locked layers behave as fixed screen-space scenery.
        if (lockToCamera)
        {
            currentOffset = Vector2.zero;
            FollowCamera(currentOffset);
            return;
        }

        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        float cameraTravelX =
            targetCamera.transform.position.x - cameraStartX;

        float targetRelativeY = target != null
            ? target.position.y - targetStartY
            : 0f;

        // Counter camera travel for parallax.
        Vector2 desiredOffset = new Vector2(
            -cameraTravelX * horizontalStrength,
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
        // Scale the sprite to cover the camera plus its allowed parallax travel.
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
