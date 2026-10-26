using UnityEngine;

public class PoolController : MonoBehaviour
{
    [System.Serializable]
    private class SpawnEntry
    {
        public GameObject prefab;
        public float distance;
        public float xOffset;
        public float yOffset;
    }

    [Header("Scene")]
    [SerializeField] private Transform alice;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D floorCollider;
    [SerializeField] private Transform activeEnemies;

    [Header("Progress")]
    [SerializeField] private float spawnPadding = 1.5f;
    [SerializeField] private float levelLength = 135f;

    [Header("Sequence")]
    [SerializeField] private SpawnEntry[] spawnSequence;

    private float levelStartX;
    private float furthestProgress;
    private int nextSpawnIndex;

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
                "Pool Controller is missing Alice."
            );
        }

        if (mainCamera == null)
        {
            Debug.LogError(
                "Pool Controller is missing the Main Camera."
            );
        }

        if (floorCollider == null)
        {
            Debug.LogError(
                "Pool Controller is missing the Floor collider."
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
            SpawnEnemy(
                spawnSequence[nextSpawnIndex]
            );

            nextSpawnIndex++;
        }
    }

    private void SpawnEnemy(SpawnEntry entry)
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
                rightEdge + spawnPadding + entry.xOffset,
                floorTop + entry.yOffset,
                0f
            );

        Instantiate(
            entry.prefab,
            spawnPosition,
            Quaternion.identity,
            activeEnemies
        );
    }
}