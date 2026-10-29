using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Typography")]
    [SerializeField] private Font uiFont;

    [Header("Scenes")]
    [SerializeField] private string startScene = "02_RabbitHole";
    [SerializeField] private string levelSelectScene = "01_Levels";

    [Header("Audio")]
    [SerializeField] private AudioClip navigateClip;
    [SerializeField] private AudioClip confirmClip;

    private readonly Color menuWhite = Color.white;
    private readonly Color selectedColor = new Color32(229, 138, 122, 255);
    private readonly Color backgroundColor = new Color32(10, 8, 11, 255);

    private readonly List<Text> options = new();
    private Font font;
    private Canvas canvas;
    private Texture2D whiteTexture;
    private Sprite whiteSprite;
    private InputAction navigateAction;
    private InputAction submitAction;
    private int selectedIndex;

    private void Awake()
    {
        // Build the runtime title menu.
        Time.timeScale = 1f;

        FindInputActions();

        font = uiFont != null
            ? uiFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateWhiteSprite();
        BuildUI();
        RefreshSelection();
    }

    private void OnEnable()
    {
        navigateAction?.Enable();
        submitAction?.Enable();
    }

    private void OnDisable()
    {
        navigateAction?.Disable();
        submitAction?.Disable();
    }

    private void Update()
    {
        // Read menu navigation and confirmation.
        if (navigateAction != null &&
            navigateAction.WasPerformedThisFrame())
        {
            float vertical = navigateAction.ReadValue<Vector2>().y;

            if (vertical > 0.5f)
            {
                selectedIndex = WrapIndex(selectedIndex - 1, options.Count);
                GameAudio.Play(navigateClip);
                RefreshSelection();
            }
            else if (vertical < -0.5f)
            {
                selectedIndex = WrapIndex(selectedIndex + 1, options.Count);
                GameAudio.Play(navigateClip);
                RefreshSelection();
            }
        }

        if (submitAction != null &&
            submitAction.WasPressedThisFrame())
        {
            GameAudio.Play(confirmClip);
            ActivateSelection();
        }
    }

    private void FindInputActions()
    {
        // Find the shared UI actions.
        if (inputActions == null)
        {
            Debug.LogError(
                "Title Screen is missing the Input Actions asset."
            );
            return;
        }

        navigateAction = inputActions.FindAction("UI/Navigate");
        submitAction = inputActions.FindAction("UI/Submit");
    }

    private void OnDestroy()
    {
        if (whiteSprite != null)
        {
            Destroy(whiteSprite);
        }

        if (whiteTexture != null)
        {
            Destroy(whiteTexture);
        }
    }

    private void BuildUI()
    {
        // Create the title screen entirely at runtime.
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform background = CreateRect(
            "Background",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.sprite = whiteSprite;
        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = false;

        Text title = CreateText(
            "Title",
            canvas.transform,
            "ALICE\nCHASER",
            104,
            menuWhite,
            new Vector2(0.5f, 0.70f),
            new Vector2(1000f, 250f),
            TextAnchor.MiddleCenter
        );
        title.lineSpacing = 0.78f;

        options.Add(CreateMenuText("START", 0.425f));
        options.Add(CreateMenuText("LEVEL SELECT", 0.345f));
        options.Add(CreateMenuText("QUIT", 0.265f));
    }

    private void ActivateSelection()
    {
        // Open the selected destination.
        switch (selectedIndex)
        {
            case 0:
                SceneManager.LoadScene(startScene);
                break;

            case 1:
                SceneManager.LoadScene(levelSelectScene);
                break;

            case 2:
                QuitGame();
                break;
        }
    }

    private void RefreshSelection()
    {
        // Update the selector and selected text color.
        for (int i = 0; i < options.Count; i++)
        {
            bool selected = i == selectedIndex;
            string label = options[i].gameObject.name;

            options[i].text = selected
                ? $"> {label}"
                : label;

            options[i].color = selected
                ? selectedColor
                : menuWhite;
        }
    }

    private void QuitGame()
    {
        // Stop Play Mode in the Editor or quit the build.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private Text CreateMenuText(string value, float anchorY)
    {
        return CreateText(
            value,
            canvas.transform,
            value,
            40,
            menuWhite,
            new Vector2(0.5f, anchorY),
            new Vector2(800f, 70f),
            TextAnchor.MiddleCenter
        );
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        string value,
        int fontSize,
        Color color,
        Vector2 anchor,
        Vector2 size,
        TextAnchor alignment)
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
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
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
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private void CreateWhiteSprite()
    {
        whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();

        whiteSprite = Sprite.Create(
            whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private int WrapIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        if (index < 0)
        {
            return count - 1;
        }

        if (index >= count)
        {
            return 0;
        }

        return index;
    }
}