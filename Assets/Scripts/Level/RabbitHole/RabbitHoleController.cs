using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Scrolling")]
    [SerializeField] private float scrollStartX = 2.45f;
    [SerializeField] private float scrollSpeed = 6f;
    [SerializeField] private float spawnPadding = 1.5f;
    [SerializeField] private float despawnPadding = 2f;
    [SerializeField] private float levelLength = 95f;

    [Header("Sequence")]
    [SerializeField] private SpawnEntry[] spawnSequence;

    private float distanceTravelled;
    private int nextSpawnIndex;
    private bool levelFinished;

    public bool IsScrolling { get; private set; }
    public float ScrollSpeed => scrollSpeed;

    public float DespawnX
    {
        get
        {
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

            return leftEdge - despawnPadding;
        }
    }

    private void Update()
    {
        if (levelFinished)
        {
            IsScrolling = false;
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            IsScrolling = false;
            return;
        }

        bool rightHeld =
            keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed;

        bool leftHeld =
            keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed;

        // Scroll while Alice pushes against the right boundary.
        IsScrolling =
            alice.position.x >= scrollStartX &&
            rightHeld &&
            !leftHeld;
    }

    private void FixedUpdate()
    {
        if (!IsScrolling)
        {
            return;
        }

        // Advance through the Rabbit Hole.
        distanceTravelled +=
            scrollSpeed * Time.fixedDeltaTime;

        // Spawn reached entries.
        while (nextSpawnIndex < spawnSequence.Length &&
               distanceTravelled >=
               spawnSequence[nextSpawnIndex].distance)
        {
            SpawnObject(
                spawnSequence[nextSpawnIndex]
            );

            nextSpawnIndex++;
        }

        // End the placeholder sequence.
        if (distanceTravelled >= levelLength)
        {
            distanceTravelled = levelLength;
            levelFinished = true;
            IsScrolling = false;

            Debug.Log(
                "Rabbit Hole placeholder complete."
            );
        }
    }

    private void SpawnObject(SpawnEntry entry)
    {
        if (entry.prefab == null)
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

        GameObject spawned = Instantiate(
            entry.prefab,
            spawnPosition,
            Quaternion.identity,
            activeObjects
        );

        RabbitHoleScrollObject scrollObject =
            spawned.GetComponent<RabbitHoleScrollObject>();

        if (scrollObject == null)
        {
            Debug.LogError(
                spawned.name +
                " is missing RabbitHoleScrollObject."
            );

            return;
        }

        scrollObject.Initialize(this);
    }
}