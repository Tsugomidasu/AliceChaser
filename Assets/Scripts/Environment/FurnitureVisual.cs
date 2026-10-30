using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FurnitureVisual : MonoBehaviour
{
    [SerializeField] private Sprite[] variants;
    [SerializeField] private bool randomFlipX = true;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private Sprite referenceSprite;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        referenceSprite = spriteRenderer.sprite;

        if (variants == null || variants.Length == 0)
        {
            return;
        }

        if (referenceSprite == null)
        {
            referenceSprite = variants[0];
        }

        Sprite sprite = variants[Random.Range(0, variants.Length)];
        ApplyVariant(sprite);

        spriteRenderer.flipX =
            randomFlipX && Random.value < 0.5f;
    }

    private void ApplyVariant(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        spriteRenderer.sprite = sprite;

        if (referenceSprite == null || sprite.bounds.size.y <= 0f)
        {
            return;
        }

        // Preserve authored height across sprite variants.
        float heightScale =
            referenceSprite.bounds.size.y / sprite.bounds.size.y;

        transform.localScale = new Vector3(
            baseScale.x * heightScale,
            baseScale.y * heightScale,
            baseScale.z
        );
    }
}
