using System.Collections;
using UnityEngine;

public class AliceAttackVFX : MonoBehaviour
{
    [Header("Slash Effects")]
    [SerializeField] private SpriteRenderer verticalSlash;
    [SerializeField] private SpriteRenderer horizontalSlash;

    [Header("Thrust Effect")]
    [SerializeField] private SpriteRenderer thrustEffect;
    [SerializeField] private float heavyThrustScaleMultiplier = 1.5f;

    [Header("Slash and Thrust Animation")]
    [SerializeField] private float duration = 0.09f;
    [SerializeField] private float startScaleMultiplier = 0.85f;
    [SerializeField] private float endScaleMultiplier = 1.05f;
    [SerializeField, Range(0f, 1f)] private float fadeStart = 0.55f;

    private AliceCombat combat;

    private MaterialPropertyBlock propertyBlock;
    private int opacityId;
    private int thrustColorId;
    private int coreColorId;

    private static readonly Color NormalThrustColor =
        new Color32(0x00, 0xB6, 0xFF, 0xFF);

    private static readonly Color HeavyThrustColor =
        new Color32(0xBB, 0x0A, 0x1E, 0xFF);

    private static readonly Color NormalThrustCoreColor =
        Color.white;

    private static readonly Color HeavyThrustCoreColor =
        Color.black;

    private Vector3 verticalBaseScale;
    private Vector3 horizontalBaseScale;
    private Vector3 thrustBaseScale;

    private Coroutine activeRoutine;

    private void Awake()
    {
        combat = GetComponent<AliceCombat>();

        // Set up the shader property block.
        propertyBlock = new MaterialPropertyBlock();
        opacityId = Shader.PropertyToID("_Opacity");
        thrustColorId = Shader.PropertyToID("_ThrustColor");
        coreColorId = Shader.PropertyToID("_CoreColor");

        // Store the original slash scales.
        if (verticalSlash != null)
        {
            verticalBaseScale = verticalSlash.transform.localScale;
            ResetEffect(verticalSlash, verticalBaseScale);
        }

        if (horizontalSlash != null)
        {
            horizontalBaseScale = horizontalSlash.transform.localScale;
            ResetEffect(horizontalSlash, horizontalBaseScale);
        }

        // Store the original thrust scale.
        if (thrustEffect != null)
        {
            thrustBaseScale = thrustEffect.transform.localScale;
            ResetEffect(thrustEffect, thrustBaseScale);
        }
    }

    public void ShowVerticalSlash()
    {
        PlayEffect(
            verticalSlash,
            verticalBaseScale
        );
    }

    public void ShowHorizontalSlash()
    {
        PlayEffect(
            horizontalSlash,
            horizontalBaseScale
        );
    }

    public void ShowThrust()
    {
        if (thrustEffect == null)
        {
            return;
        }

        bool isHeavy =
            combat != null && combat.IsHeavyAttack;

        // Use the heavy thrust colors.
        if (isHeavy)
        {
            SetThrustColors(
                HeavyThrustColor,
                HeavyThrustCoreColor
            );
        }
        else
        {
            // Use the normal thrust colors.
            SetThrustColors(
                NormalThrustColor,
                NormalThrustCoreColor
            );
        }

        PlayEffect(
            thrustEffect,
            thrustBaseScale,
            isHeavy ? heavyThrustScaleMultiplier : 1f
        );
    }

    private void PlayEffect(
        SpriteRenderer renderer,
        Vector3 baseScale,
        float effectScaleMultiplier = 1f
    )
    {
        if (renderer == null)
        {
            return;
        }

        // Stop the previous slash or thrust effect.
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        // Hide both slash effects.
        if (verticalSlash != null)
        {
            ResetEffect(
                verticalSlash,
                verticalBaseScale
            );
        }

        if (horizontalSlash != null)
        {
            ResetEffect(
                horizontalSlash,
                horizontalBaseScale
            );
        }

        // Hide the thrust effect.
        if (thrustEffect != null)
        {
            ResetEffect(
                thrustEffect,
                thrustBaseScale
            );
        }

        // Play the selected slash or thrust effect.
        activeRoutine = StartCoroutine(
            AnimateEffect(
                renderer,
                baseScale,
                effectScaleMultiplier
            )
        );
    }

    private IEnumerator AnimateEffect(
        SpriteRenderer renderer,
        Vector3 baseScale,
        float effectScaleMultiplier
    )
    {
        renderer.transform.localScale =
            baseScale *
            effectScaleMultiplier *
            startScaleMultiplier;

        SetOpacity(renderer, 1f);
        renderer.enabled = true;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            // Expand the slash or thrust effect slightly.
            float scaleMultiplier = Mathf.Lerp(
                startScaleMultiplier,
                endScaleMultiplier,
                t
            );

            renderer.transform.localScale =
                baseScale *
                effectScaleMultiplier *
                scaleMultiplier;

            float opacity = 1f;

            // Fade the slash or thrust effect near the end.
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

        // Hide and restore the slash or thrust effect.
        ResetEffect(renderer, baseScale);
        activeRoutine = null;
    }

    private void ResetEffect(
        SpriteRenderer renderer,
        Vector3 baseScale
    )
    {
        renderer.enabled = false;
        renderer.transform.localScale = baseScale;

        SetOpacity(renderer, 1f);
    }

    private void SetThrustColors(
        Color thrustColor,
        Color coreColor
    )
    {
        if (propertyBlock == null || thrustEffect == null)
        {
            return;
        }

        // Override the thrust shader colors.
        thrustEffect.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(thrustColorId, thrustColor);
        propertyBlock.SetColor(coreColorId, coreColor);
        thrustEffect.SetPropertyBlock(propertyBlock);
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
        // Stop the active slash or thrust effect.
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        // Reset both slash effects.
        if (verticalSlash != null)
        {
            ResetEffect(
                verticalSlash,
                verticalBaseScale
            );
        }

        if (horizontalSlash != null)
        {
            ResetEffect(
                horizontalSlash,
                horizontalBaseScale
            );
        }

        // Reset the thrust effect.
        if (thrustEffect != null)
        {
            ResetEffect(
                thrustEffect,
                thrustBaseScale
            );
        }
    }
}