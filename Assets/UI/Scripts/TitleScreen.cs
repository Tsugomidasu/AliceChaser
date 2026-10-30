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
    [SerializeField] private Font titleFont;
    [SerializeField] private Font uiFont;

    [Header("Scenes")]
    [SerializeField] private string startScene = "02_RabbitHole";
    [SerializeField] private string levelSelectScene = "01_Levels";

    [Header("Audio")]
    [SerializeField] private AudioClip navigateClip;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private AudioClip titleBgm;
    [SerializeField, Range(0f, 1f)] private float titleBgmVolume = 1f;

    private readonly Color menuWhite = Color.white;
    private readonly Color selectedColor = new Color32(229, 138, 122, 255);
    private readonly List<Text> options = new();
    private Font font;
    private Canvas canvas;
    private InputAction navigateAction;
    private InputAction submitAction;
    private SettingsMenu settingsMenu;
    private int selectedIndex;
    private string secretInput = string.Empty;

    private const string SecretBgmCode = "ecila";

    private void Awake()
    {
        Time.timeScale = 1f;

        FindInputActions();

        font = uiFont != null
            ? uiFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildUI();
        BuildSettingsMenu();
        RefreshSelection();
    }

    private void OnEnable()
    {
        navigateAction?.Enable();
        submitAction?.Enable();

        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput += HandleTextInput;
        }
    }

    private void OnDisable()
    {
        navigateAction?.Disable();
        submitAction?.Disable();

        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= HandleTextInput;
        }
    }

    private void Update()
    {
        if (settingsMenu != null && settingsMenu.IsOpen)
        {
            return;
        }

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

    private void HandleTextInput(char character)
    {
        if (settingsMenu != null && settingsMenu.IsOpen)
        {
            return;
        }

        char letter = char.ToLowerInvariant(character);

        if (letter < 'a' || letter > 'z')
        {
            return;
        }

        // Keep only the letters needed to match the hidden title code.
        secretInput += letter;

        if (secretInput.Length > SecretBgmCode.Length)
        {
            secretInput = secretInput.Substring(
                secretInput.Length - SecretBgmCode.Length
            );
        }

        if (secretInput != SecretBgmCode)
        {
            return;
        }

        GameAudio.PlayMusic(titleBgm, titleBgmVolume);
        secretInput = string.Empty;
    }

    private void FindInputActions()
    {
        if (inputActions == null)
        {
            Debug.LogError(
                "Title Screen is missing the Input Actions asset."
            );
            return;
        }

        SettingsMenu.ApplySavedBindings(inputActions);
        navigateAction = inputActions.FindAction("UI/Navigate");
        submitAction = inputActions.FindAction("UI/Submit");
    }

    private void BuildUI()
    {
        // Build the title menu from the configured scene and visual assets.
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

        Text title = CreateText(
            "Title",
            canvas.transform,
            "ALICE CHASER",
            104,
            menuWhite,
            new Vector2(0.5f, 0.70f),
            new Vector2(1400f, 140f),
            TextAnchor.MiddleCenter
        );
        title.font = titleFont != null ? titleFont : font;

        options.Add(CreateMenuText("START", 0.45f));
        options.Add(CreateMenuText("LEVEL SELECT", 0.38f));
        options.Add(CreateMenuText("SETTINGS", 0.31f));
        options.Add(CreateMenuText("QUIT", 0.24f));
    }

    private void BuildSettingsMenu()
    {
        settingsMenu = gameObject.AddComponent<SettingsMenu>();
        settingsMenu.Initialize(
            canvas.transform,
            font,
            inputActions,
            navigateClip,
            confirmClip,
            confirmClip
        );
    }

    private void ActivateSelection()
    {
        // Route the current menu selection to its scene or submenu action.
        switch (selectedIndex)
        {
            case 0:
                GameAudio.StopMusic();
                SceneManager.LoadScene(startScene);
                break;

            case 1:
                SceneManager.LoadScene(levelSelectScene);
                break;

            case 2:
                // Restore SETTINGS as the selected item when returning.
                settingsMenu?.Open(() =>
                {
                    selectedIndex = 2;
                    RefreshSelection();
                });
                break;

            case 3:
                QuitGame();
                break;
        }
    }

    private void RefreshSelection()
    {
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
