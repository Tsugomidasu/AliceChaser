using UnityEngine;

public class EnemyCleanup : MonoBehaviour
{
    [Header("Cleanup")]
    [SerializeField] private float despawnPadding = 3f;

    private Camera mainCamera;
    private EnemyHealth health;

    private void Awake()
    {
        mainCamera = Camera.main;
        health = GetComponent<EnemyHealth>();
    }

    private void LateUpdate()
    {
        // Defeat logic owns dead-enemy cleanup.
        if (health != null && health.IsDead)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        float cameraDistance =
            Mathf.Abs(mainCamera.transform.position.z);

        float leftEdge =
            mainCamera.ViewportToWorldPoint(
                new Vector3(
                    0f,
                    0.5f,
                    cameraDistance
                )
            ).x;

        // Remove living enemies only after they pass behind the camera.
        if (transform.position.x < leftEdge - despawnPadding)
        {
            Destroy(gameObject);
        }
    }
}
