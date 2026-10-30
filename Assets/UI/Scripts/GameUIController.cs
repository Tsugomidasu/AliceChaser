using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    private enum MenuMode
    {
        None,
        Pause,
        Settings,
        GameOver
    }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Typography")]
    [SerializeField] private Font uiFont;

    [Header("Health")]
    [SerializeField] private Sprite heartFullSprite;
    [SerializeField] private Sprite heartHalfSprite;
    [SerializeField] private Sprite heartEmptySprite;

    [Header("Heavy Charge")]
    [SerializeField] private Vector2 chargeOffset = new Vector2(0f, 85f);

    [Header("Game Over")]
    [SerializeField] private float gameOverDelay = 0.8f;
    [SerializeField] private float gameOverFadeTime = 1.1f;
    [SerializeField] private float gameOverOptionsDelay = 0.45f;
    [SerializeField] private float gameOverOptionsFadeTime = 0.3f;

    [Header("Scenes")]
    [SerializeField] private string levelSelectScene = "01_Levels";
    [SerializeField] private string titleScene = "00_Title";

    [Header("Audio")]
    [SerializeField] private AudioClip navigateClip;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private AudioClip cancelClip;

    private readonly Color menuWhite = Color.white;
    private readonly Color selectedColor = new Color32(229, 138, 122, 255);
    private readonly Color chargeCrimson = new Color32(158, 18, 15, 255);
    private readonly Color darkCrimson = new Color32(169, 43, 43, 255);

    private Canvas canvas;
    private Font font;
    private Texture2D whiteTexture;
    private Sprite whiteSprite;

    private AliceHealth health;
    private AliceMovement movement;
    private AliceCombat combat;
    private Camera mainCamera;

    private RectTransform healthRoot;
    private readonly List<Image> hearts = new();
    private int displayedMaxHealth = -1;
    private int displayedHealth = -1;

    private RectTransform chargeRoot;
    private Image chargeFill;
    private bool wasCharging;
    private float chargeStartedAt;

    private GameObject tutorialRoot;
    private Text tutorialText;
    private string tutorialMessage;

    private GameObject pauseRoot;
    private SettingsMenu settingsMenu;
    private GameObject gameOverRoot;
    private CanvasGroup gameOverCanvasGroup;
    private GameObject gameOverOptionsRoot;
    private CanvasGroup gameOverOptionsCanvasGroup;

    private readonly List<Text> pauseOptions = new();
    private readonly List<Text> gameOverOptions = new();

    private MenuMode menuMode;
    private int selectedIndex;
    private bool gameOverStarted;
    private float previousTimeScale = 1f;
    private bool movementWasEnabled;
    private bool combatWasEnabled;

    private InputAction pauseAction;
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    public Font UIFont => font;

    private void Awake()
    {
        Time.timeScale = 1f;

        FindInputActions();

        font = uiFont != null
            ? uiFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateWhiteSprite();
        mainCamera = Camera.main;

        FindAlice();
        BuildUI();
    }

    private void Start()
    {
        RefreshHealth();
    }

    private void Update()
    {
        if (health == null)
        {
            FindAlice();
        }

        UpdateHealth();
        UpdateCharge();

        if (!gameOverStarted &&
            health != null &&
            health.IsDefeated)
        {
            StartCoroutine(GameOverSequence());
        }

        if (gameOverStarted)
        {
            if (menuMode == MenuMode.GameOver)
            {
                ReadMenuInput();
            }

            return;
        }

        if (menuMode == MenuMode.Settings)
        {
            return;
        }

        if (menuMode == MenuMode.None &&
            pauseAction != null &&
            pauseAction.WasPressedThisFrame())
        {
            OpenPause();
            return;
        }

        if (menuMode == MenuMode.Pause)
        {
            ReadMenuInput();
        }
    }

    private void OnEnable()
    {
        pauseAction?.Enable();

        if (menuMode == MenuMode.None)
        {
            DisableMenuActions();
        }
        else
        {
            EnableMenuActions();
        }
    }

    private void EnableMenuActions()
    {
        navigateAction?.Enable();
        submitAction?.Enable();
        cancelAction?.Enable();
    }

    private void DisableMenuActions()
    {
        navigateAction?.Disable();
        submitAction?.Disable();
        cancelAction?.Disable();
    }

    private void OnDisable()
    {
        pauseAction?.Disable();
        DisableMenuActions();
    }

    private void FindInputActions()
    {
        if (inputActions == null)
        {
            Debug.LogError(
                "Game UI is missing the Input Actions asset."
            );
            return;
        }

        SettingsMenu.ApplySavedBindings(inputActions);
        pauseAction = inputActions.FindAction("Player/Pause");
        navigateAction = inputActions.FindAction("UI/Navigate");
        submitAction = inputActions.FindAction("UI/Submit");
        cancelAction = inputActions.FindAction("UI/Cancel");
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        if (whiteSprite != null)
        {
            Destroy(whiteSprite);
        }

        if (whiteTexture != null)
        {
            Destroy(whiteTexture);
        }
    }


    private void CreateWhiteSprite()
    {
        whiteTexture = new Texture2D(
            1,
            1,
            TextureFormat.RGBA32,
            false
        );

        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();

        whiteSprite = Sprite.Create(
            whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private void FindAlice()
    {
        health = FindAnyObjectByType<AliceHealth>();

        if (health != null)
        {
            movement = health.GetComponent<AliceMovement>();
            combat = health.GetComponent<AliceCombat>();
        }
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform)
        );

        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        // Build gameplay HUD and menu overlays under one canvas.
        BuildHealth();
        BuildCharge();
        BuildTutorial();
        BuildPause();
        BuildSettingsMenu();
        BuildGameOver();
    }

    private void BuildHealth()
    {
        healthRoot = CreateRect(
            "Health",
            canvas.transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -22f),
            new Vector2(220f, 72f)
        );

        healthRoot.gameObject.SetActive(false);
    }

    private void BuildCharge()
    {
        chargeRoot = CreateRect(
            "HeavyCharge",
            canvas.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(150f, 10f)
        );

        Image background = chargeRoot.gameObject.AddComponent<Image>();
        background.sprite = whiteSprite;
        background.color = new Color(0.05f, 0.04f, 0.04f, 0.82f);

        RectTransform fillRect = CreateRect(
            "Fill",
            chargeRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(-4f, -4f)
        );

        chargeFill = fillRect.gameObject.AddComponent<Image>();
        chargeFill.sprite = whiteSprite;
        chargeFill.color = chargeCrimson;
        chargeFill.type = Image.Type.Filled;
        chargeFill.fillMethod = Image.FillMethod.Horizontal;
        chargeFill.fillOrigin = 0;
        chargeFill.fillAmount = 0f;

        chargeRoot.gameObject.SetActive(false);
    }


    private void BuildTutorial()
    {
        RectTransform tutorialRect = CreateRect(
            "TutorialPrompt",
            canvas.transform,
            new Vector2(0.5f, 0.17f),
            new Vector2(0.5f, 0.17f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(640f, 110f)
        );

        tutorialRoot = tutorialRect.gameObject;
        tutorialText = CreateText(
            "TutorialText",
            tutorialRoot.transform,
            string.Empty,
            34,
            menuWhite,
            new Vector2(0.5f, 0.5f),
            new Vector2(640f, 110f)
        );

        tutorialRoot.SetActive(false);
    }

    public void ShowTutorialPrompt(string message)
    {
        tutorialMessage = message ?? string.Empty;

        if (tutorialText != null)
        {
            tutorialText.text = tutorialMessage;
        }

        if (tutorialRoot != null &&
            menuMode == MenuMode.None &&
            !gameOverStarted &&
            !string.IsNullOrEmpty(tutorialMessage))
        {
            tutorialRoot.SetActive(true);
        }
    }

    public void HideTutorialPrompt()
    {
        tutorialMessage = string.Empty;

        if (tutorialText != null)
        {
            tutorialText.text = string.Empty;
        }

        if (tutorialRoot != null)
        {
            tutorialRoot.SetActive(false);
        }
    }

    private void BuildPause()
    {
        RectTransform pauseRect = CreateRect(
            "PauseMenu",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        pauseRoot = pauseRect.gameObject;

        CreateDimmer(pauseRoot.transform, 0.66f);

        CreateText(
            "PauseTitle",
            pauseRoot.transform,
            "PAUSED",
            66,
            menuWhite,
            new Vector2(0.5f, 0.65f),
            new Vector2(720f, 90f)
        );

        pauseOptions.Add(CreateMenuText(
            pauseRoot.transform,
            "RESUME",
            0.49f
        ));

        pauseOptions.Add(CreateMenuText(
            pauseRoot.transform,
            "SETTINGS",
            0.42f
        ));

        pauseOptions.Add(CreateMenuText(
            pauseRoot.transform,
            "RESTART",
            0.35f
        ));

        pauseOptions.Add(CreateMenuText(
            pauseRoot.transform,
            "LEVEL SELECT",
            0.28f
        ));

        pauseOptions.Add(CreateMenuText(
            pauseRoot.transform,
            "RETURN TO TITLE",
            0.21f
        ));

        pauseRoot.SetActive(false);
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
            cancelClip
        );
    }

    private void BuildGameOver()
    {
        RectTransform gameOverRect = CreateRect(
            "GameOver",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        gameOverRoot = gameOverRect.gameObject;

        gameOverCanvasGroup = gameOverRoot.AddComponent<CanvasGroup>();
        gameOverCanvasGroup.alpha = 0f;

        CreateDimmer(gameOverRoot.transform, 0.68f);

        CreateText(
            "GameOverTitle",
            gameOverRoot.transform,
            "GAME OVER",
            78,
            darkCrimson,
            new Vector2(0.5f, 0.62f),
            new Vector2(900f, 110f)
        );

        RectTransform gameOverOptionsRect = CreateRect(
            "Options",
            gameOverRoot.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        gameOverOptionsRoot = gameOverOptionsRect.gameObject;
        gameOverOptionsCanvasGroup =
            gameOverOptionsRoot.AddComponent<CanvasGroup>();
        gameOverOptionsCanvasGroup.alpha = 0f;

        gameOverOptions.Add(CreateMenuText(
            gameOverOptionsRoot.transform,
            "RETRY",
            0.46f
        ));

        gameOverOptions.Add(CreateMenuText(
            gameOverOptionsRoot.transform,
            "LEVEL SELECT",
            0.39f
        ));

        gameOverOptions.Add(CreateMenuText(
            gameOverOptionsRoot.transform,
            "RETURN TO TITLE",
            0.32f
        ));

        gameOverRoot.SetActive(false);
    }

    private void UpdateHealth()
    {
        if (health == null ||
            heartFullSprite == null ||
            heartHalfSprite == null ||
            heartEmptySprite == null)
        {
            healthRoot.gameObject.SetActive(false);
            return;
        }

        if (health.MaxHealth != displayedMaxHealth ||
            health.CurrentHealth != displayedHealth)
        {
            RefreshHealth();
        }
    }

    private void RefreshHealth()
    {
        // Rebuild heart slots only when Alice's maximum health changes.
        if (health == null ||
            heartFullSprite == null ||
            heartHalfSprite == null ||
            heartEmptySprite == null)
        {
            healthRoot.gameObject.SetActive(false);
            return;
        }

        int maxHealth = health.MaxHealth;
        int heartSlots = Mathf.CeilToInt(maxHealth / 2f);

        if (displayedMaxHealth != maxHealth)
        {
            foreach (Image heart in hearts)
            {
                if (heart != null)
                {
                    Destroy(heart.gameObject);
                }
            }

            hearts.Clear();

            for (int i = 0; i < heartSlots; i++)
            {
                RectTransform heartRect = CreateRect(
                    $"Heart_{i + 1}",
                    healthRoot,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(i * 68f, 0f),
                    new Vector2(60f, 60f)
                );

                Image heart = heartRect.gameObject.AddComponent<Image>();
                heart.preserveAspect = true;
                hearts.Add(heart);
            }
        }

        for (int i = 0; i < hearts.Count; i++)
        {
            int healthInSlot = health.CurrentHealth - (i * 2);

            if (healthInSlot >= 2)
            {
                hearts[i].sprite = heartFullSprite;
            }
            else if (healthInSlot == 1)
            {
                hearts[i].sprite = heartHalfSprite;
            }
            else
            {
                hearts[i].sprite = heartEmptySprite;
            }
        }

        displayedMaxHealth = maxHealth;
        displayedHealth = health.CurrentHealth;
        healthRoot.gameObject.SetActive(true);
    }

    private void UpdateCharge()
    {
        // Follow Alice and fill the bar over the Heavy charge time.
        if (combat == null ||
            health == null ||
            health.IsDefeated ||
            menuMode != MenuMode.None)
        {
            chargeRoot.gameObject.SetActive(false);
            wasCharging = false;
            return;
        }

        bool isCharging = combat.IsCharging;

        if (!isCharging)
        {
            chargeRoot.gameObject.SetActive(false);
            wasCharging = false;
            return;
        }

        if (!wasCharging)
        {
            chargeStartedAt = Time.unscaledTime;
            wasCharging = true;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(
                health.transform.position
            );

            chargeRoot.position = screenPosition +
                new Vector3(chargeOffset.x, chargeOffset.y, 0f);
        }

        float duration = Mathf.Max(0.01f, combat.HeavyChargeDuration);
        chargeFill.fillAmount = Mathf.Clamp01(
            (Time.unscaledTime - chargeStartedAt) / duration
        );

        chargeRoot.gameObject.SetActive(true);
    }

    private void OpenPause()
    {
        // Preserve control state for resume.
        previousTimeScale = Time.timeScale;
        movementWasEnabled = movement != null && movement.enabled;
        combatWasEnabled = combat != null && combat.enabled;

        if (movement != null)
        {
            movement.enabled = false;
        }

        if (combat != null)
        {
            combat.enabled = false;
        }

        Time.timeScale = 0f;
        EnableMenuActions();

        menuMode = MenuMode.Pause;
        selectedIndex = 0;
        pauseRoot.SetActive(true);
        chargeRoot.gameObject.SetActive(false);

        if (tutorialRoot != null)
        {
            tutorialRoot.SetActive(false);
        }

        RefreshMenuSelection();
    }

    private void ClosePause()
    {
        pauseRoot.SetActive(false);
        menuMode = MenuMode.None;
        DisableMenuActions();
        Time.timeScale = previousTimeScale;

        if (movement != null)
        {
            movement.enabled = movementWasEnabled;
        }

        if (combat != null)
        {
            combat.enabled = combatWasEnabled;
        }

        if (tutorialRoot != null &&
            !string.IsNullOrEmpty(tutorialMessage))
        {
            tutorialRoot.SetActive(true);
        }
    }

    private IEnumerator GameOverSequence()
    {
        // Reveal Game Over first, then enable its menu options.
        gameOverStarted = true;
        pauseRoot.SetActive(false);
        menuMode = MenuMode.None;

        yield return new WaitForSecondsRealtime(gameOverDelay);

        Time.timeScale = 0f;
        chargeRoot.gameObject.SetActive(false);

        if (tutorialRoot != null)
        {
            tutorialRoot.SetActive(false);
        }

        gameOverRoot.SetActive(true);
        gameOverOptionsRoot.SetActive(false);

        float elapsed = 0f;

        while (elapsed < gameOverFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            gameOverCanvasGroup.alpha = Mathf.Clamp01(
                elapsed / Mathf.Max(0.01f, gameOverFadeTime)
            );

            yield return null;
        }

        gameOverCanvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(
            gameOverOptionsDelay
        );

        gameOverOptionsRoot.SetActive(true);

        float optionsElapsed = 0f;

        while (optionsElapsed < gameOverOptionsFadeTime)
        {
            optionsElapsed += Time.unscaledDeltaTime;
            gameOverOptionsCanvasGroup.alpha = Mathf.Clamp01(
                optionsElapsed / Mathf.Max(0.01f, gameOverOptionsFadeTime)
            );

            yield return null;
        }

        gameOverOptionsCanvasGroup.alpha = 1f;
        menuMode = MenuMode.GameOver;
        EnableMenuActions();
        selectedIndex = 0;
        RefreshMenuSelection();
    }

    private void ReadMenuInput()
    {
        // Route shared UI input to the active pause or Game Over menu.
        if (menuMode == MenuMode.Pause &&
            cancelAction != null &&
            cancelAction.WasPressedThisFrame())
        {
            GameAudio.Play(cancelClip);
            ClosePause();
            return;
        }

        List<Text> options = menuMode == MenuMode.Pause
            ? pauseOptions
            : gameOverOptions;

        if (navigateAction != null &&
            navigateAction.WasPerformedThisFrame())
        {
            float vertical = navigateAction.ReadValue<Vector2>().y;

            if (vertical > 0.5f)
            {
                selectedIndex = WrapIndex(
                    selectedIndex - 1,
                    options.Count
                );

                GameAudio.Play(navigateClip);
                RefreshMenuSelection();
            }
            else if (vertical < -0.5f)
            {
                selectedIndex = WrapIndex(
                    selectedIndex + 1,
                    options.Count
                );

                GameAudio.Play(navigateClip);
                RefreshMenuSelection();
            }
        }

        if (submitAction != null &&
            submitAction.WasPressedThisFrame())
        {
            GameAudio.Play(confirmClip);
            ActivateSelection();
        }
    }

    private void ActivateSelection()
    {
        if (menuMode == MenuMode.Pause)
        {
            switch (selectedIndex)
            {
                case 0:
                    ClosePause();
                    break;

                case 1:
                    OpenSettings();
                    break;

                case 2:
                    ReloadCurrentLevel();
                    break;

                case 3:
                    LoadScene(levelSelectScene);
                    break;

                case 4:
                    LoadScene(titleScene);
                    break;
            }

            return;
        }

        if (menuMode == MenuMode.GameOver)
        {
            switch (selectedIndex)
            {
                case 0:
                    ReloadCurrentLevel();
                    break;

                case 1:
                    LoadScene(levelSelectScene);
                    break;

                case 2:
                    LoadScene(titleScene);
                    break;
            }
        }
    }

    private void RefreshMenuSelection()
    {
        List<Text> options = menuMode == MenuMode.Pause
            ? pauseOptions
            : gameOverOptions;

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

    private void OpenSettings()
    {
        // Return to the same pause selection when Settings closes.
        if (settingsMenu == null)
        {
            return;
        }

        pauseRoot.SetActive(false);
        menuMode = MenuMode.Settings;

        settingsMenu.Open(() =>
        {
            menuMode = MenuMode.Pause;
            EnableMenuActions();
            selectedIndex = 1;
            pauseRoot.SetActive(true);
            RefreshMenuSelection();
        });
    }

    private void ReloadCurrentLevel()
    {
        Time.timeScale = 1f;
        GameAudio.StopMusic();
        GameAudio.StopAmbience();

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        GameAudio.StopMusic();
        GameAudio.StopAmbience();
        SceneManager.LoadScene(sceneName);
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

    private void CreateDimmer(Transform parent, float alpha)
    {
        RectTransform rect = CreateRect(
            "Dimmer",
            parent,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = whiteSprite;
        image.color = new Color(0f, 0f, 0f, alpha);
        image.raycastTarget = false;
    }

    private Text CreateText(
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
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    private Text CreateMenuText(
        Transform parent,
        string value,
        float anchorY)
    {
        return CreateText(
            value,
            parent,
            value,
            38,
            menuWhite,
            new Vector2(0.5f, anchorY),
            new Vector2(760f, 64f)
        );
    }

}
