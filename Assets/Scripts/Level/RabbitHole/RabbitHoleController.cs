using UnityEngine;

public class RabbitHoleController : MonoBehaviour
{
    [System.Serializable]
    private class SpawnEntry
    {
        public GameObject prefab;
        public float distance;
        public float yOffset;
    }

    [Header("Scene")]
    [SerializeField] private Transform alice;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D floorCollider;
    [SerializeField] private Transform activeObjects;

    [Header("Camera")]
    [SerializeField] private float leftDeadZone = -3.75f;
    [SerializeField] private float rightDeadZone = 4.25f;

    [Header("Progress")]
    [SerializeField] private float spawnPadding = 1.5f;
    [SerializeField] private float levelLength = 95f;

    [Header("Sequence")]
    [SerializeField] private SpawnEntry[] spawnSequence;

    private float levelStartX;
    private float initialCameraX;
    private float furthestProgress;
    private int nextSpawnIndex;
    private bool completionLogged;

    private void Awake()
    {
        if (alice != null)
        {
            levelStartX = alice.position.x;
        }

        if (mainCamera != null)
        {
            initialCameraX = mainCamera.transform.position.x;
        }
    }

    private void Start()
    {
        if (alice == null)
        {
            Debug.LogError(
                "Rabbit Hole Controller is missing Alice."
            );
        }

        if (mainCamera == null)
        {
            Debug.LogError(
                "Rabbit Hole Controller is missing the Main Camera."
            );
        }

        if (floorCollider == null)
        {
            Debug.LogError(
                "Rabbit Hole Controller is missing the Floor collider."
            );
        }
    }

    private void Update()
    {
        if (alice == null)
        {
            return;
        }

        // Track only the furthest point Alice has reached so backtracking
        // never causes an already-used sequence entry to spawn again.
        float currentProgress = Mathf.Clamp(
            alice.position.x - levelStartX,
            0f,
            levelLength
        );

        furthestProgress = Mathf.Max(
            furthestProgress,
            currentProgress
        );

        // Spawn each entry once when Alice first reaches its distance.
        while (nextSpawnIndex < spawnSequence.Length &&
               furthestProgress >=
               spawnSequence[nextSpawnIndex].distance)
        {
            SpawnObject(
                spawnSequence[nextSpawnIndex]
            );

            nextSpawnIndex++;
        }

        if (!completionLogged &&
            furthestProgress >= levelLength)
        {
            completionLogged = true;

            Debug.Log(
                "Rabbit Hole placeholder complete."
            );
        }
    }

    private void LateUpdate()
    {
        if (alice == null || mainCamera == null)
        {
            return;
        }

        Transform cameraTransform = mainCamera.transform;
        float cameraX = cameraTransform.position.x;
        float targetX = cameraX;

        // Alice moves freely inside the horizontal dead zone. The camera
        // follows only after she crosses one of its edges.
        float leftEdge = cameraX + leftDeadZone;
        float rightEdge = cameraX + rightDeadZone;

        if (alice.position.x < leftEdge)
        {
            targetX = alice.position.x - leftDeadZone;
        }
        else if (alice.position.x > rightEdge)
        {
            targetX = alice.position.x - rightDeadZone;
        }

        // Do not scroll before the beginning of the Rabbit Hole or beyond
        // the playable level length.
        float maximumCameraX =
            levelStartX + levelLength - rightDeadZone;

        targetX = Mathf.Clamp(
            targetX,
            initialCameraX,
            maximumCameraX
        );

        cameraTransform.position = new Vector3(
            targetX,
            cameraTransform.position.y,
            cameraTransform.position.z
        );
    }

    private void SpawnObject(SpawnEntry entry)
    {
        if (entry.prefab == null ||
            mainCamera == null ||
            floorCollider == null)
        {
            return;
        }

        float cameraDistance =
            Mathf.Abs(mainCamera.transform.position.z);

        float rightEdge =
            mainCamera.ViewportToWorldPoint(
                new Vector3(
                    1f,
                    0.5f,
                    cameraDistance
                )
            ).x;

        float floorTop =
            floorCollider.bounds.max.y;

        Vector3 spawnPosition =
            new Vector3(
                rightEdge + spawnPadding,
                floorTop + entry.yOffset,
                0f
            );

        Instantiate(
            entry.prefab,
            spawnPosition,
            Quaternion.identity,
            activeObjects
        );
    }
}