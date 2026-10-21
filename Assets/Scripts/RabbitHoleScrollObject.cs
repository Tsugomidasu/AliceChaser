using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RabbitHoleScrollObject : MonoBehaviour
{
    private Rigidbody2D rb;
    private RabbitHoleController controller;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(
        RabbitHoleController rabbitHoleController
    )
    {
        controller = rabbitHoleController;
    }

    private void FixedUpdate()
    {
        if (controller == null ||
            !controller.IsScrolling)
        {
            return;
        }

        Vector2 nextPosition =
            rb.position +
            Vector2.left *
            controller.ScrollSpeed *
            Time.fixedDeltaTime;

        // Move with the Rabbit Hole.
        rb.MovePosition(nextPosition);

        // Remove objects after they leave the camera.
        if (nextPosition.x <= controller.DespawnX)
        {
            Destroy(gameObject);
        }
    }
}