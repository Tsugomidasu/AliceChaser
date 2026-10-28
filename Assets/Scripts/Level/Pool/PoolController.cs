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

    [System.Serializable]
    private class Placement
    {
        public GameObject prefab;
        public Sprite sprite;
        public float x;
        public float yOffset;
        [Min(0.1f)] public float scale = 1f;
        public bool flipX;
    }

    [Header("Scene")]
    [SerializeField] private Transform alice;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D floorCollider;
    [SerializeField] private Transform activeEnemies;
    [SerializeField] private Transform rocksParent;
    [SerializeField] private Transform treesParent;

    [Header("Spawning")]
    [SerializeField] private float spawnPadding = 1.5f;

    [Header("Progression")]
    [SerializeField] private float levelLength = 135f;
    [SerializeField] private string nextScene = "00_Title";
    [SerializeField] private string completionLabel = "CHAPTER II COMPLETE";
    [SerializeField] private string completionSubtitle = "THE POOL OF TEARS";

    [Header("Enemy Sequence")]
    [SerializeField] private SpawnEntry[] spawnSequence;

    [Header("Gameplay Rocks")]
    [SerializeField] private Placement[] rockPlacements;

    [Header("Decorative Trees")]
    [SerializeField] private Placement[] treePlacements;

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
            return;
        }

        SpawnPlacements(rockPlacements, rocksParent);
        SpawnPlacements(treePlacements, treesParent);
        SpawnVisibleEnemies();
    }

    private void Update()
    {
        SpawnVisibleEnemies();

        if (alice == null)
        {
            return;
        }

        float currentProgress = Mathf.Clamp(
            alice.position.x - levelStartX,
            0f,
            levelLength
        );

        furthestProgress = Mathf.Max(
            furthestProgress,
            currentProgress
        );

        AliceHealth health = alice.GetComponent<AliceHealth>();

        if (!completionStarted &&
            furthestProgress >= levelLength &&
            (health == null || !health.IsDefeated))
        {
            completionStarted = true;
            ChapterProgress.UnlockChapter(3);

            LevelTransition.Play(
                completionLabel,
                completionSubtitle,
                nextScene
            );
        }
    }

    private void SpawnVisibleEnemies()
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

        // Spawn enemies shortly before they enter the camera.
        while (nextSpawnIndex < spawnSequence.Length)
        {
            SpawnEntry entry = spawnSequence[nextSpawnIndex];
            float spawnX = GetSpawnX(entry);

            if (spawnX > rightEdge + spawnPadding)
            {
                break;
            }

            SpawnEnemy(entry, spawnX);
            nextSpawnIndex++;
        }
    }

    private void SpawnEnemy(SpawnEntry entry, float spawnX)
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

        Instantiate(
            entry.prefab,
            spawnPosition,
            Quaternion.identity,
            activeEnemies
        );
    }

    private void SpawnPlacements(Placement[] placements, Transform parent)
    {
        if (placements == null || floorCollider == null)
        {
            return;
        }

        float floorTop = floorCollider.bounds.max.y;

        foreach (Placement entry in placements)
        {
            if (entry.prefab == null)
            {
                continue;
            }

            GameObject spawned = Instantiate(
                entry.prefab,
                new Vector3(
                    entry.x,
                    floorTop + entry.yOffset,
                    0f
                ),
                Quaternion.identity,
                parent
            );

            spawned.transform.localScale =
                Vector3.one * entry.scale;

            SpriteRenderer sprite =
                spawned.GetComponentInChildren<SpriteRenderer>();

            if (sprite != null)
            {
                if (entry.sprite != null)
                {
                    sprite.sprite = entry.sprite;
                }

                sprite.flipX = entry.flipX;
            }
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