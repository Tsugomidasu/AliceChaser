using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class FoxEnemy : MonoBehaviour
{
    [Header("Run")]
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float despawnDistance = 24f;

    [Header("Sprite")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Audio")]
    [SerializeField] private AudioClip spawnClip;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth health;

    private float startX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        startX = rb.position.x;
        GameAudio.Play(spawnClip);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = spriteFacesRight;
        }
    }

    private void FixedUpdate()
    {
        if (health.IsDead)
        {
            return;
        }

        // Normal hits do not interrupt the run.
        rb.linearVelocityX = -runSpeed;

        // Remove the Fox after it clears the encounter.
        if (startX - rb.position.x >= despawnDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        if (rb != null && (health == null || !health.IsDead))
        {
            rb.linearVelocityX = 0f;
        }
    }
}
