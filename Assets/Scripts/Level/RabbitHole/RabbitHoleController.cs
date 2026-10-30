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

    [Header("Audio")]
    [SerializeField] private AudioClip backgroundMusicClip;

    [Header("Progression")]
    [SerializeField] private string nextScene = "03_Pool";
    [SerializeField] private string completionLabel = "CHAPTER I COMPLETE";
    [SerializeField] private string completionSubtitle = "DOWN THE RABBIT-HOLE";
    [SerializeField] private AudioClip chapterCompleteClip;

    [Header("Furniture Size Tiers")]
    [SerializeField, Min(0.1f)] private float smallScale = 1f;
    [SerializeField, Min(0.1f)] private float mediumScale = 1.45f;
    [SerializeField, Min(0.1f)] private float largeScale = 2f;

    [Header("Sequence")]
    [SerializeField] private SpawnEntry[] spawnSequence;

    private float levelStartX;
    private float furthestProgress;
    private int nextSpawnIndex;
    private bool completionStarted;

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
        GameAudio.PlayMusic(backgroundMusicClip);
        GameAudio.StopAmbience();

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

        float currentProgress = Mathf.Clamp(
            alice.position.x - levelStartX,
            0f,
            levelLength
        );

        // Keep completion progress even if Alice backtracks.
        furthestProgress = Mathf.Max(
            furthestProgress,
            currentProgress
        );

        SpawnVisibleEntries();

        AliceHealth health = alice.GetComponent<AliceHealth>();

        // Start the chapter transition once Alice reaches the level end.
        if (!completionStarted &&
            furthestProgress >= levelLength &&
            (health == null || !health.IsDefeated))
        {
            completionStarted = true;
            ChapterProgress.UnlockChapter(2);

            LevelTransition.Play(
                completionLabel,
                completionSubtitle,
                nextScene,
                chapterCompleteClip
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

        // Spawn furniture beyond the camera edge.
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

        // Apply the authored size category after the object is spawned.
        GameObject spawnedObject = Instantiate(
            entry.prefab,
            spawnPosition,
            Quaternion.Euler(0f, 0f, entry.rotation),
            activeObjects
        );

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
