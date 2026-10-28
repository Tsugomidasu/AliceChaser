using UnityEngine;

public class RabbitHoleController : MonoBehaviour
{
    private enum FurnitureSize
    {
        Small,
        Medium,
        Large
    }

    [System.Serializable]
    private class SpawnEntry
    {
        public GameObject prefab;
        public float distance;
        public float xOffset;
        public float yOffset;
        public float rotation;
        public FurnitureSize size = FurnitureSize.Small;
    }

    [Header("Scene")]
    [SerializeField] private Transform alice;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D floorCollider;
    [SerializeField] private Transform activeObjects;

    [Header("Progress")]
    [SerializeField] private float spawnPadding = 1.5f;
    [SerializeField] private float levelLength = 95f;

    [Header("Furniture Size Tiers")]
    [SerializeField, Min(0.1f)] private float smallScale = 1f;
    [SerializeField, Min(0.1f)] private float mediumScale = 1.45f;
    [SerializeField, Min(0.1f)] private float largeScale = 2f;

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

        SortSpawnSequence();
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

        SpawnVisibleEntries();
    }

    private void Update()
    {
        if (alice == null)
        {
            return;
        }

        // Track Alice's furthest progress.
        float currentProgress = Mathf.Clamp(
            alice.position.x - levelStartX,
            0f,
            levelLength
        );

        furthestProgress = Mathf.Max(
            furthestProgress,
            currentProgress
        );

        SpawnVisibleEntries();

        if (!completionLogged &&
            furthestProgress >= levelLength)
        {
            completionLogged = true;

            Debug.Log(
                "Rabbit Hole complete."
            );
        }
    }

    private void SpawnVisibleEntries()
    {
        if (spawnSequence == null ||
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

        // Spawn objects just before they enter the camera.
        while (nextSpawnIndex < spawnSequence.Length)
        {
            SpawnEntry entry = spawnSequence[nextSpawnIndex];
            float spawnX = GetSpawnX(entry);

            if (spawnX > rightEdge + spawnPadding)
            {
                break;
            }

            SpawnObject(entry, spawnX);
            nextSpawnIndex++;
        }
    }

    private void SpawnObject(SpawnEntry entry, float spawnX)
    {
        if (entry.prefab == null || floorCollider == null)
        {
            return;
        }

        float floorTop =
            floorCollider.bounds.max.y;

        Vector3 spawnPosition =
            new Vector3(
                spawnX,
                floorTop + entry.yOffset,
                0f
            );

        GameObject spawnedObject = Instantiate(
            entry.prefab,
            spawnPosition,
            Quaternion.Euler(0f, 0f, entry.rotation),
            activeObjects
        );

        // Apply the selected furniture size tier.
        spawnedObject.transform.localScale *= GetFurnitureScale(entry.size);
    }

    private float GetFurnitureScale(FurnitureSize size)
    {
        switch (size)
        {
            case FurnitureSize.Medium:
                return mediumScale;

            case FurnitureSize.Large:
                return largeScale;

            default:
                return smallScale;
        }
    }

    private float GetSpawnX(SpawnEntry entry)
    {
        return levelStartX +
            entry.distance +
            entry.xOffset;
    }

    private void SortSpawnSequence()
    {
        if (spawnSequence == null)
        {
            return;
        }

        System.Array.Sort(
            spawnSequence,
            (a, b) =>
                (a.distance + a.xOffset).CompareTo(
                    b.distance + b.xOffset
                )
        );
    }

    private void OnValidate()
    {
        SortSpawnSequence();
    }
}
