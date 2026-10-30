using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelTransition : MonoBehaviour
{
    private const float FadeToBlackTime = 0.55f;
    private const float TextFadeTime = 0.25f;
    private const float HoldTime = 0.9f;
    private const float FadeFromBlackTime = 0.5f;

    private static bool isRunning;

    private string heading;
    private string subtitle;
    private string nextScene;
    private AudioClip chapterCompleteClip;
    private Font font;
    private Image background;
    private CanvasGroup textGroup;

    public static bool IsRunning => isRunning;

    public static void Play(
        string heading,
        string subtitle,
        string nextScene,
        AudioClip chapterCompleteClip)
    {
        if (isRunning)
        {
            return;
        }

        GameObject transitionObject = new GameObject(
            "LevelTransition"
        );

        DontDestroyOnLoad(transitionObject);

        LevelTransition transition =
            transitionObject.AddComponent<LevelTransition>();

        transition.heading = heading;
        transition.subtitle = subtitle;
        transition.nextScene = nextScene;
        transition.chapterCompleteClip = chapterCompleteClip;

        GameUIController gameUI =
            FindAnyObjectByType<GameUIController>();

        if (gameUI != null)
        {
            transition.font = gameUI.UIFont;
            gameUI.enabled = false;
        }

        transition.StartCoroutine(transition.RunTransition());
    }

    private IEnumerator RunTransition()
    {
        // Load while fully faded out.
        isRunning = true;
        Time.timeScale = 1f;

        // Freeze gameplay before the completion presentation begins.
        DisablePlayerControl();
        BuildUI();

        // Fade out, load while black, then fade into the next scene.
        yield return FadeBackground(0f, 1f, FadeToBlackTime);
        GameAudio.Play(chapterCompleteClip);
        yield return FadeText(0f, 1f, TextFadeTime);
        yield return new WaitForSecondsRealtime(HoldTime);
        yield return FadeText(1f, 0f, TextFadeTime);

        if (!string.IsNullOrWhiteSpace(nextScene))
        {
            GameAudio.StopMusic();
            GameAudio.StopAmbience();

            AsyncOperation load = SceneManager.LoadSceneAsync(nextScene);

            while (load != null && !load.isDone)
            {
                yield return null;
            }
        }

        yield return null;
        yield return FadeBackground(1f, 0f, FadeFromBlackTime);

        isRunning = false;
        Destroy(gameObject);
    }

    private void DisablePlayerControl()
    {
        AliceHealth health = FindAnyObjectByType<AliceHealth>();

        if (health == null)
        {
            return;
        }

        AliceMovement movement = health.GetComponent<AliceMovement>();
        AliceCombat combat = health.GetComponent<AliceCombat>();
        Rigidbody2D body = health.GetComponent<Rigidbody2D>();

        if (movement != null)
        {
            movement.enabled = false;
        }

        if (combat != null)
        {
            combat.enabled = false;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void BuildUI()
    {
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
        }

        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform backgroundRect = CreateRect(
            "Background",
            canvasObject.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        background = backgroundRect.gameObject.AddComponent<Image>();
        background.color = Color.clear;
        background.raycastTarget = false;

        RectTransform textRoot = CreateRect(
            "Text",
            canvasObject.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        textGroup = textRoot.gameObject.AddComponent<CanvasGroup>();
        textGroup.alpha = 0f;

        CreateText(
            "CompleteTitle",
            textRoot,
            heading,
            72,
            Color.white,
            new Vector2(0.5f, 0.55f),
            new Vector2(1100f, 100f)
        );

        CreateText(
            "ChapterTitle",
            textRoot,
            subtitle,
            38,
            new Color(0.78f, 0.78f, 0.78f, 1f),
            new Vector2(0.5f, 0.45f),
            new Vector2(1200f, 70f)
        );
    }

    private IEnumerator FadeBackground(
        float from,
        float to,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);
            background.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        background.color = new Color(0f, 0f, 0f, to);
    }

    private IEnumerator FadeText(
        float from,
        float to,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            textGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        textGroup.alpha = to;
    }

    private RectTransform CreateRect(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject child = new GameObject(
            objectName,
            typeof(RectTransform)
        );

        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        return rect;
    }

    private void CreateText(
        string objectName,
        Transform parent,
        string value,
        int fontSize,
        Color color,
        Vector2 anchor,
        Vector2 size)
    {
        RectTransform rect = CreateRect(
            objectName,
            parent,
            anchor,
            anchor,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            size
        );

        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
    }
}
