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
        if (alice == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                alice = player.transform;
            }
        }

        // Reset the level framing before the Rabbit intro begins.
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
            cameraFollow = FindAnyObjectByType<CameraFollow>();
        }

        cameraFollow?.ResetTracking();
    }

    private void SpawnRabbit()
    {
        if (rabbitPrefab == null || rabbitSpawn == null)
        {
            return;
        }

        GameObject rabbit = Instantiate(
            rabbitPrefab,
            rabbitSpawn.position,
            rabbitSpawn.rotation
        );

        RabbitIntro intro = rabbit.GetComponent<RabbitIntro>();
        AliceMovement movement = alice != null
            ? alice.GetComponent<AliceMovement>()
            : null;

        // Lock Alice for the Rabbit intro delay.
        if (intro != null && movement != null)
        {
            movement.LockControls(intro.StartDelay);
        }
    }
}
