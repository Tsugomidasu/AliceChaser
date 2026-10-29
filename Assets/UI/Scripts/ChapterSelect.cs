using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChapterSelect : MonoBehaviour
{
    private sealed class ChapterRow
    {
        public Text Selector;
        public Image Icon;
        public Text Number;
        public Text Title;
        public bool Playable;
        public string SceneName;
    }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Typography")]
    [SerializeField] private Font uiFont;

    [Header("Chapter Icons")]
    [SerializeField] private Sprite[] chapterIcons;
    [SerializeField] private Sprite[] lockedChapterIcons;

    [Header("Scenes")]
    [SerializeField] private string titleScene = "00_Title";
    [SerializeField] private string chapterOneScene = "02_RabbitHole";
    [SerializeField] private string chapterTwoScene = "03_Pool";

    private readonly Color menuWhite = Color.white;
    private readonly Color selectedColor = new Color32(229, 138, 122, 255);
    private readonly Color lockedColor = new Color32(102, 102, 102, 255);
    private readonly Color backgroundColor = new Color32(8, 7, 10, 255);

    private readonly List<ChapterRow> chapters = new();
    private Font font;
    private Canvas canvas;
    private Texture2D whiteTexture;
    private Sprite whiteSprite;
    private Text returnText;
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;
    private int selectedItem;

    private static readonly string[] ChapterNumbers =
    {
        "CHAPTER I",
        "CHAPTER II",
        "CHAPTER III",
        "CHAPTER IV",
        "CHAPTER V",
        "CHAPTER VI",
        "CHAPTER VII",
        "CHAPTER VIII"
    };

    private static readonly string[] ChapterTitles =
    {
        "DOWN THE RABBIT-HOLE",
        "THE POOL OF TEARS",
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty
    };

    private void Awake()
    {
        // Build the chapter menu and select the first available row.
        Time.timeScale = 1f;

        FindInputActions();

        font = uiFont != null
            ? uiFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateWhiteSprite();
        BuildUI();
        selectedItem = FirstSelectable();
        RefreshSelection();
    }

    private void OnEnable()
    {
        navigateAction?.Enable();
        submitAction?.Enable();
        cancelAction?.Enable();
    }

    private void OnDisable()
    {
        navigateAction?.Disable();
        submitAction?.Disable();
        cancelAction?.Disable();
    }

    private void Update()
    {
        // Read cancel, navigation, and confirmation.
        if (cancelAction != null &&
            cancelAction.WasPressedThisFrame())
        {
            SceneManager.LoadScene(titleScene);
            return;
        }

        if (navigateAction != null &&
            navigateAction.WasPerformedThisFrame())
        {
            float vertical = navigateAction.ReadValue<Vector2>().y;

            if (vertical > 0.5f)
            {
                selectedItem = PreviousSelectable(selectedItem);
                RefreshSelection();
            }
            else if (vertical < -0.5f)
            {
                selectedItem = NextSelectable(selectedItem);
                RefreshSelection();
            }
        }

        if (submitAction != null &&
            submitAction.WasPressedThisFrame())
        {
            ActivateSelection();
        }
    }

    private void FindInputActions()
    {
        // Find the shared UI actions.
        if (inputActions == null)
        {
            Debug.LogError(
                "Chapter Select is missing the Input Actions asset."
            );
            return;
        }

        navigateAction = inputActions.FindAction("UI/Navigate");
        submitAction = inputActions.FindAction("UI/Submit");
        cancelAction = inputActions.FindAction("UI/Cancel");
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
        // Build all chapter rows and apply saved unlock progress.
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

        CreateText(
            "Header",
            canvas.transform,
            "SELECT CHAPTER",
            58,
            menuWhite,
            new Vector2(0.5f, 0.885f),
            new Vector2(900f, 86f),
            TextAnchor.MiddleCenter
        );

        for (int i = 0; i < ChapterTitles.Length; i++)
        {
            string sceneName = GetSceneName(i);
            bool playable =
                !string.IsNullOrEmpty(sceneName) &&
                ChapterProgress.IsUnlocked(i + 1);

            float y = 0.735f - (i * 0.073f);
            chapters.Add(CreateChapterRow(
                i,
                ChapterNumbers[i],
                ChapterTitles[i],
                playable,
                sceneName,
                y
            ));
        }

        returnText = CreateText(
            "RETURN",
            canvas.transform,
            "RETURN",
            34,
            menuWhite,
            new Vector2(0.5f, 0.095f),
            new Vector2(600f, 64f),
            TextAnchor.MiddleCenter
        );
    }

    private ChapterRow CreateChapterRow(
        int index,
        string chapterNumber,
        string title,
        bool playable,
        string sceneName,
        float anchorY)
    {
        Color rowColor = playable ? menuWhite : lockedColor;

        Text selector = CreateText(
            $"Selector_{index + 1}",
            canvas.transform,
            string.Empty,
            32,
            selectedColor,
            new Vector2(0.115f, anchorY),
            new Vector2(70f, 56f),
            TextAnchor.MiddleRight
        );

        Image icon = CreateChapterIcon(
            index,
            playable,
            anchorY
        );

        Text number = CreateText(
            $"Number_{index + 1}",
            canvas.transform,
            chapterNumber,
            30,
            rowColor,
            new Vector2(0.285f, anchorY),
            new Vector2(330f, 56f),
            TextAnchor.MiddleLeft
        );

        Text titleText = CreateText(
            $"Title_{index + 1}",
            canvas.transform,
            title,
            30,
            rowColor,
            new Vector2(0.59f, anchorY),
            new Vector2(990f, 56f),
            TextAnchor.MiddleLeft
        );

        return new ChapterRow
        {
            Selector = selector,
            Icon = icon,
            Number = number,
            Title = titleText,
            Playable = playable,
            SceneName = sceneName
        };
    }

    private Image CreateChapterIcon(
        int index,
        bool playable,
        float anchorY)
    {
        Sprite sprite = GetChapterIcon(index, playable);

        RectTransform rect = CreateRect(
            $"Icon_{index + 1}",
            canvas.transform,
            new Vector2(0.175f, anchorY),
            new Vector2(0.175f, anchorY),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(58f, 58f)
        );

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = sprite != null;
        return image;
    }

    private Sprite GetChapterIcon(int index, bool playable)
    {
        // Use separate artwork for playable and locked chapters.
        Sprite[] source = playable
            ? chapterIcons
            : lockedChapterIcons;

        if (source == null || index < 0 || index >= source.Length)
        {
            return null;
        }

        return source[index];
    }

    private string GetSceneName(int chapterIndex)
    {
        return chapterIndex switch
        {
            0 => chapterOneScene,
            1 => chapterTwoScene,
            _ => string.Empty
        };
    }

    private void ActivateSelection()
    {
        // Load playable chapters or return to the title screen.
        if (selectedItem == chapters.Count)
        {
            SceneManager.LoadScene(titleScene);
            return;
        }

        ChapterRow row = chapters[selectedItem];

        if (row.Playable && !string.IsNullOrEmpty(row.SceneName))
        {
            SceneManager.LoadScene(row.SceneName);
        }
    }

    private void RefreshSelection()
    {
        // Update selectors, icons, and lock colors.
        for (int i = 0; i < chapters.Count; i++)
        {
            ChapterRow row = chapters[i];
            bool selected = i == selectedItem && row.Playable;
            Color color = row.Playable
                ? (selected ? selectedColor : menuWhite)
                : lockedColor;

            row.Selector.text = selected ? ">" : string.Empty;
            if (row.Icon != null)
            {
                row.Icon.color = row.Playable
                    ? (selected ? selectedColor : menuWhite)
                    : Color.white;
            }
            row.Number.color = color;
            row.Title.color = color;
        }

        bool returnSelected = selectedItem == chapters.Count;
        returnText.text = returnSelected ? "> RETURN" : "RETURN";
        returnText.color = returnSelected ? selectedColor : menuWhite;
    }

    private int FirstSelectable()
    {
        for (int i = 0; i < chapters.Count; i++)
        {
            if (chapters[i].Playable)
            {
                return i;
            }
        }

        return chapters.Count;
    }

    private int NextSelectable(int current)
    {
        // Skip locked or unimplemented chapters.
        for (int i = current + 1; i < chapters.Count; i++)
        {
            if (chapters[i].Playable)
            {
                return i;
            }
        }

        if (current != chapters.Count)
        {
            return chapters.Count;
        }

        return FirstSelectable();
    }

    private int PreviousSelectable(int current)
    {
        // Skip locked or unimplemented chapters.
        int start = current == chapters.Count
            ? chapters.Count - 1
            : current - 1;

        for (int i = start; i >= 0; i--)
        {
            if (chapters[i].Playable)
            {
                return i;
            }
        }

        return chapters.Count;
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
}
