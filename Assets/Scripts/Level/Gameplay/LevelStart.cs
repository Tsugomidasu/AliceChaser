using UnityEngine;

public class LevelStart : MonoBehaviour
{
    [Header("Alice")]
    [SerializeField] private Transform alice;
    [SerializeField] private Transform aliceSpawn;

    [Header("Rabbit")]
    [SerializeField] private GameObject rabbitPrefab;
    [SerializeField] private Transform rabbitSpawn;

    [Header("Camera")]
    [SerializeField] private CameraFollow cameraFollow;

    private void Awake()
    {
        // Find Alice.
        if (alice == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                alice = player.transform;
            }
        }

        PlaceAlice();
        ResetCamera();
        SpawnRabbit();
    }

    private void PlaceAlice()
    {
        if (alice == null || aliceSpawn == null)
        {
            return;
        }

        // Place Alice.
        alice.SetPositionAndRotation(
            aliceSpawn.position,
            aliceSpawn.rotation
        );

        Rigidbody2D rb = alice.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void ResetCamera()
    {
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }

        cameraFollow?.ResetTracking();
    }

    private void SpawnRabbit()
    {
        if (rabbitPrefab == null || rabbitSpawn == null)
        {
            return;
        }

        // Spawn the Rabbit.
        GameObject rabbit = Instantiate(
            rabbitPrefab,
            rabbitSpawn.position,
            rabbitSpawn.rotation
        );

        RabbitIntro intro = rabbit.GetComponent<RabbitIntro>();
        AliceMovement movement = alice != null
            ? alice.GetComponent<AliceMovement>()
            : null;

        // Lock Alice during the intro.
        if (intro != null && movement != null)
        {
            movement.LockControls(intro.StartDelay);
        }
    }
}