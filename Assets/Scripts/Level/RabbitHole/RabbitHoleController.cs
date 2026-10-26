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

    [Header("Progress")]
    [SerializeField] private float spawnPadding = 1.5f;
    [SerializeField] private float levelLength = 95f;

    [Header("Sequence")]
    [SerializeField] private SpawnEntry[] spawnSequence;

    private float levelStartX;
    private float furthestProgress;
    private int nextSpawnIndex;
    private bool completionLogged;

    private void Awake()
    {
        if (alice != null)
        {
            levelStartX = alice.position.x;
        }
    }

    private void Start()
    {
        // Check the scene references.
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

        // Track Alice's furthest progress so used entries do not respawn.
        float currentProgress = Mathf.Clamp(
            alice.position.x - levelStartX,
            0f,
            levelLength
        );

        furthestProgress = Mathf.Max(
            furthestProgress,
            currentProgress
        );

        if (spawnSequence == null)
        {
            return;
        }

        // Spawn each entry once at its progress distance.
        while (nextSpawnIndex < spawnSequence.Length &&
               furthestProgress >= spawnSequence[nextSpawnIndex].distance)
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