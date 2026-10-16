using System.Collections;
using UnityEngine;

public class AliceAttackVFX : MonoBehaviour
{
    [Header("Slash Effects")]
    [SerializeField] private SpriteRenderer verticalSlash;
    [SerializeField] private SpriteRenderer horizontalSlash;

    [Header("Slash Animation")]
    [SerializeField] private float duration = 0.09f;
    [SerializeField] private float startScaleMultiplier = 0.85f;
    [SerializeField] private float endScaleMultiplier = 1.05f;
    [SerializeField, Range(0f, 1f)] private float fadeStart = 0.55f;

    private MaterialPropertyBlock propertyBlock;
    private int opacityId;

    private Vector3 verticalBaseScale;
    private Vector3 horizontalBaseScale;

    private Coroutine activeRoutine;

    private void Awake()
    {
        // Set up the shader property block.
        propertyBlock = new MaterialPropertyBlock();
        opacityId = Shader.PropertyToID("_Opacity");

        // Store the original slash scales.
        if (verticalSlash != null)
        {
            verticalBaseScale = verticalSlash.transform.localScale;
            ResetSlash(verticalSlash, verticalBaseScale);
        }

        if (horizontalSlash != null)
        {
            horizontalBaseScale = horizontalSlash.transform.localScale;
            ResetSlash(horizontalSlash, horizontalBaseScale);
        }
    }

    public void ShowVerticalSlash()
    {
        PlaySlash(
            verticalSlash,
            verticalBaseScale
        );
    }

    public void ShowHorizontalSlash()
    {
        PlaySlash(
            horizontalSlash,
            horizontalBaseScale
        );
    }

    private void PlaySlash(
        SpriteRenderer renderer,
        Vector3 baseScale
    )
    {
        if (renderer == null)
        {
            return;
        }

        // Stop the previous slash effect.
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        // Hide both slash effects.
        if (verticalSlash != null)
        {
            ResetSlash(
                verticalSlash,
                verticalBaseScale
            );
        }

        if (horizontalSlash != null)
        {
            ResetSlash(
                horizontalSlash,
                horizontalBaseScale
            );
        }

        // Play the selected slash effect.
        activeRoutine = StartCoroutine(
            AnimateSlash(renderer, baseScale)
        );
    }

    private IEnumerator AnimateSlash(
        SpriteRenderer renderer,
        Vector3 baseScale
    )
    {
        renderer.transform.localScale =
            baseScale * startScaleMultiplier;

        SetOpacity(renderer, 1f);
        renderer.enabled = true;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            // Expand the slash slightly.
            float scaleMultiplier = Mathf.Lerp(
                startScaleMultiplier,
                endScaleMultiplier,
                t
            );

            renderer.transform.localScale =
                baseScale * scaleMultiplier;

            float opacity = 1f;

            // Fade the slash near the end.
            if (t > fadeStart)
            {
                float fadeT = Mathf.InverseLerp(
                    fadeStart,
                    1f,
                    t
                );

                opacity = 1f - Mathf.SmoothStep(
                    0f,
                    1f,
                    fadeT
                );
            }

            SetOpacity(renderer, opacity);

            yield return null;
        }

        // Hide and restore the slash.
        ResetSlash(renderer, baseScale);
        activeRoutine = null;
    }

    private void ResetSlash(
        SpriteRenderer renderer,
        Vector3 baseScale
    )
    {
        renderer.enabled = false;
        renderer.transform.localScale = baseScale;

        SetOpacity(renderer, 1f);
    }

    private void SetOpacity(
        SpriteRenderer renderer,
        float opacity
    )
    {
        if (propertyBlock == null)
        {
            return;
        }

        // Override the shader opacity.
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(opacityId, opacity);
        renderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDisable()
    {
        // Stop the active slash effect.
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        // Reset both slash effects.
        if (verticalSlash != null)
        {
            ResetSlash(
                verticalSlash,
                verticalBaseScale
            );
        }

        if (horizontalSlash != null)
        {
            ResetSlash(
                horizontalSlash,
                horizontalBaseScale
            );
        }
    }
}