using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private const string BindingOverridesKey = "InputBindingOverrides";
    private static readonly HashSet<InputActionAsset> BindingAssetsLoaded = new();

    private enum Page
    {
        Hub,
        Sound,
        Controls
    }

    private enum EntryType
    {
        PageLink,
        SoundRow,
        Rebind,
        ResetBindings,
        Back
    }

    private enum SoundTrack
    {
        Music,
        Sfx,
        Ambience
    }

    private sealed class Entry
    {
        public EntryType type;
        public Page targetPage;
        public SoundTrack soundTrack;
        public Text label;
        public Text valueText;
        public Text buttonText;
        public Text selectorText;
        public Slider slider;
        public Image sliderHandle;
        public string actionPath;
        public string originalBindingPath;
        public string fallbackDisplay;
    }

    public static event Action BindingsChanged;

    private readonly Color menuWhite = Color.white;
    private readonly Color selectedColor = new Color32(229, 138, 122, 255);
    private readonly Color dimColor = Color.black;
    private readonly Color sliderBackgroundColor = new Color32(54, 49, 57, 255);
    private readonly Color sliderFillColor = new Color32(169, 43, 43, 255);

    private readonly List<Entry> hubEntries = new();
    private readonly List<Entry> soundEntries = new();
    private readonly List<Entry> controlEntries = new();

    private GameObject root;
    private GameObject hubPage;
    private GameObject soundPage;
    private GameObject controlsPage;
    private GameObject controlsList;
    private Font font;
    private InputActionAsset inputActions;
    private AudioClip navigateClip;
    private AudioClip confirmClip;
    private AudioClip cancelClip;
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;
    private Texture2D whiteTexture;
    private Sprite whiteSprite;
    private Text pageTitleText;
    private Text statusText;
    private Page currentPage;
    private int selectedIndex;
    private Action closeCallback;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;
    private Entry rebindingEntry;
    private InputAction rebindingAction;
    private bool rebindingActionWasEnabled;
    private bool navigateWasEnabled;
    private bool submitWasEnabled;
    private bool cancelWasEnabled;
    private int openedFrame = -1;

    public bool IsOpen => root != null && root.activeSelf;

    private List<Entry> CurrentEntries
    {
        get
        {
            return currentPage switch
            {
                Page.Sound => soundEntries,
                Page.Controls => controlEntries,
                _ => hubEntries
            };
        }
    }

    public void Initialize(
        Transform parent,
        Font menuFont,
        InputActionAsset actions,
        AudioClip navigate,
        AudioClip confirm,
        AudioClip cancel)
    {
        font = menuFont;
        inputActions = actions;
        navigateClip = navigate;
        confirmClip = confirm;
        cancelClip = cancel;

        ApplySavedBindings(inputActions);

        if (inputActions != null)
        {
            navigateAction = inputActions.FindAction("UI/Navigate");
            submitAction = inputActions.FindAction("UI/Submit");
            cancelAction = inputActions.FindAction("UI/Cancel");
        }

        CreateWhiteSprite();
        BuildUI(parent);
        root.SetActive(false);
    }

    public void Open(Action onClosed = null)
    {
        // Preserve the caller's UI action state while Settings is open.
        if (root == null)
        {
            return;
        }

        closeCallback = onClosed;

        navigateWasEnabled = navigateAction != null && navigateAction.enabled;
        submitWasEnabled = submitAction != null && submitAction.enabled;
        cancelWasEnabled = cancelAction != null && cancelAction.enabled;

        navigateAction?.Enable();
        submitAction?.Enable();
        cancelAction?.Enable();


        root.SetActive(true);
        root.transform.SetAsLastSibling();
        openedFrame = Time.frameCount;
        ShowPage(Page.Hub);
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        CancelRebind();
        root.SetActive(false);


        RestoreUIActionState();

        Action callback = closeCallback;
        closeCallback = null;
        callback?.Invoke();
    }

    private void Update()
    {
        if (!IsOpen || rebindOperation != null)
        {
            return;
        }

        // Ignore the submit event that opened Settings.
        if (Time.frameCount == openedFrame)
        {
            return;
        }

        if (cancelAction != null && cancelAction.WasPressedThisFrame())
        {
            GameAudio.Play(cancelClip);
            GoBack();
            return;
        }

        List<Entry> entries = CurrentEntries;

        if (entries.Count == 0)
        {
            return;
        }

        if (navigateAction != null && navigateAction.WasPerformedThisFrame())
        {
            Vector2 direction = navigateAction.ReadValue<Vector2>();

            if (direction.y > 0.5f)
            {
                int nextIndex = Mathf.Max(0, selectedIndex - 1);

                if (nextIndex != selectedIndex)
                {
                    selectedIndex = nextIndex;
                    GameAudio.Play(navigateClip);
                    RefreshEntries();
                }

                return;
            }

            if (direction.y < -0.5f)
            {
                int nextIndex = Mathf.Min(entries.Count - 1, selectedIndex + 1);

                if (nextIndex != selectedIndex)
                {
                    selectedIndex = nextIndex;
                    GameAudio.Play(navigateClip);
                    RefreshEntries();
                }

                return;
            }

            if (Mathf.Abs(direction.x) > 0.5f &&
                entries[selectedIndex].type == EntryType.SoundRow)
            {
                AdjustVolume(
                    entries[selectedIndex],
                    Mathf.Sign(direction.x) * 0.05f
                );

                GameAudio.Play(navigateClip);
                RefreshEntries();
                return;
            }
        }

        if (submitAction != null && submitAction.WasPressedThisFrame())
        {
            GameAudio.Play(confirmClip);
            ActivateEntry(entries[selectedIndex]);
        }
    }

    private void ActivateEntry(Entry entry)
    {
        switch (entry.type)
        {
            case EntryType.PageLink:
                ShowPage(entry.targetPage);
                break;

            case EntryType.SoundRow:
                ToggleMute(entry.soundTrack);
                RefreshEntries();
                break;

            case EntryType.Rebind:
                BeginRebind(entry);
                break;

            case EntryType.ResetBindings:
                ResetBindings();
                break;

            case EntryType.Back:
                GoBack();
                break;
        }
    }

    private void GoBack()
    {
        if (currentPage == Page.Hub)
        {
            Close();
            return;
        }

        ShowPage(Page.Hub);
    }

    private void ShowPage(Page page)
    {
        // Switch between Settings pages without rebuilding the menu.
        currentPage = page;
        selectedIndex = 0;

        hubPage.SetActive(page == Page.Hub);
        soundPage.SetActive(page == Page.Sound);
        controlsPage.SetActive(page == Page.Controls);

        pageTitleText.text = page switch
        {
            Page.Sound => "SOUND",
            Page.Controls => "CONTROLS",
            _ => "SETTINGS"
        };

        statusText.text = page switch
        {
            Page.Sound => "UP / DOWN SELECT    LEFT / RIGHT VOLUME    ENTER MUTE    ESC BACK",
            Page.Controls => "UP / DOWN SELECT    ENTER REBIND    ESC BACK",
            _ => "UP / DOWN SELECT    ENTER CONFIRM    ESC BACK"
        };

        RefreshEntries();
    }

    private void AdjustVolume(Entry entry, float amount)
    {
        SetVolume(
            entry.soundTrack,
            Mathf.Clamp01(GetVolume(entry.soundTrack) + amount)
        );
    }

    private void ToggleMute(SoundTrack track)
    {
        switch (track)
        {
            case SoundTrack.Music:
                GameAudio.SetMusicMuted(!GameAudio.MusicMuted);
                break;

            case SoundTrack.Sfx:
                GameAudio.SetSfxMuted(!GameAudio.SfxMuted);
                break;

            case SoundTrack.Ambience:
                GameAudio.SetAmbienceMuted(!GameAudio.AmbienceMuted);
                break;
        }
    }

    private void BeginRebind(Entry entry)
    {
        // Disable the selected action while waiting for a replacement key.
        if (inputActions == null)
        {
            return;
        }

        InputAction action = inputActions.FindAction(entry.actionPath);
        int bindingIndex = FindBindingIndex(action, entry.originalBindingPath);

        if (action == null || bindingIndex < 0)
        {
            Debug.LogError($"Could not find binding for {entry.actionPath}.");
            return;
        }

        rebindingEntry = entry;
        rebindingAction = action;
        rebindingActionWasEnabled = action.enabled;
        action.Disable();

        statusText.text = "PRESS A KEY    BACKSPACE CANCEL";
        RefreshEntries();

        rebindOperation = action
            .PerformInteractiveRebinding(bindingIndex)
            .WithExpectedControlType("Button")
            .WithControlsExcluding("<Mouse>")
            .WithControlsExcluding("<Gamepad>")
            .WithCancelingThrough("<Keyboard>/backspace")
            .OnComplete(operation => FinishRebind(true))
            .OnCancel(operation => FinishRebind(false))
            .Start();
    }

    private void FinishRebind(bool save)
    {
        // Restore the action and persist the override after rebinding.
        rebindOperation?.Dispose();
        rebindOperation = null;

        if (rebindingAction != null && rebindingActionWasEnabled)
        {
            rebindingAction.Enable();
        }

        rebindingAction = null;
        rebindingEntry = null;

        if (save)
        {
            SaveBindings(inputActions);
            BindingsChanged?.Invoke();
        }

        statusText.text = "UP / DOWN SELECT    ENTER REBIND    ESC BACK";
        RefreshEntries();
    }

    private void CancelRebind()
    {
        if (rebindOperation != null)
        {
            rebindOperation.Cancel();
        }
    }

    private void ResetBindings()
    {
        if (inputActions == null)
        {
            return;
        }

        inputActions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BindingOverridesKey);
        PlayerPrefs.Save();

        BindingsChanged?.Invoke();
        RefreshEntries();
    }

    private void RefreshEntries()
    {
        List<Entry> entries = CurrentEntries;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            bool selected = i == selectedIndex;
            Color color = selected ? selectedColor : menuWhite;

            if (entry.label != null)
            {
                entry.label.color = color;
            }

            if (entry.valueText != null)
            {
                entry.valueText.color = color;
            }

            if (entry.buttonText != null)
            {
                entry.buttonText.color = color;
            }

            if (entry.sliderHandle != null)
            {
                entry.sliderHandle.color = color;
            }

            RefreshEntryValue(entry);

            if (entry.selectorText != null)
            {
                entry.selectorText.text =
                    currentPage == Page.Controls && selected
                        ? ">"
                        : string.Empty;

                entry.selectorText.color = selectedColor;
            }
        }
    }

    private void RefreshEntryValue(Entry entry)
    {
        switch (entry.type)
        {
            case EntryType.SoundRow:
                float volume = GetVolume(entry.soundTrack);
                bool muted = GetMute(entry.soundTrack);

                entry.slider.SetValueWithoutNotify(volume);
                entry.valueText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
                entry.buttonText.text = muted ? "[ UNMUTE ]" : "[ MUTE ]";
                break;

            case EntryType.Rebind:
                string label = RebindLabel(
                    entry.actionPath,
                    entry.originalBindingPath
                );

                if (entry == rebindingEntry)
                {
                    entry.label.text = $"{label}        [ PRESS A KEY... ]";
                }
                else
                {
                    string display = GetBindingDisplay(
                        inputActions,
                        entry.actionPath,
                        entry.originalBindingPath,
                        entry.fallbackDisplay
                    );

                    entry.label.text = $"{label}        [ {display} ]";
                }
                break;
        }
    }

    private float GetVolume(SoundTrack track)
    {
        return track switch
        {
            SoundTrack.Music => GameAudio.MusicVolume,
            SoundTrack.Sfx => GameAudio.SfxVolume,
            _ => GameAudio.AmbienceVolume
        };
    }

    private void SetVolume(SoundTrack track, float value)
    {
        value = Mathf.Clamp01(value);

        switch (track)
        {
            case SoundTrack.Music:
                GameAudio.SetMusicVolume(value);
                break;

            case SoundTrack.Sfx:
                GameAudio.SetSfxVolume(value);
                break;

            case SoundTrack.Ambience:
                GameAudio.SetAmbienceVolume(value);
                break;
        }
    }

    private bool GetMute(SoundTrack track)
    {
        return track switch
        {
            SoundTrack.Music => GameAudio.MusicMuted,
            SoundTrack.Sfx => GameAudio.SfxMuted,
            _ => GameAudio.AmbienceMuted
        };
    }

    private string TrackName(SoundTrack track)
    {
        return track switch
        {
            SoundTrack.Music => "MUSIC",
            SoundTrack.Sfx => "SFX",
            _ => "AMBIENCE"
        };
    }

    private string RebindLabel(string actionPath, string originalPath)
    {
        if (actionPath == "Player/Move")
        {
            return originalPath == "<Keyboard>/a"
                ? "MOVE LEFT"
                : "MOVE RIGHT";
        }

        if (actionPath == "Player/Jump")
        {
            return "JUMP";
        }

        if (actionPath == "Player/Attack")
        {
            return "ATTACK";
        }

        return "PAUSE";
    }

    private void BuildUI(Transform parent)
    {
        // Build all Settings pages once and toggle them by page state.
        RectTransform rootRect = CreateRect(
            "SettingsMenu",
            parent,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        root = rootRect.gameObject;

        Image dimmer = root.AddComponent<Image>();
        dimmer.sprite = whiteSprite;
        dimmer.color = dimColor;
        dimmer.raycastTarget = false;

        pageTitleText = CreateText(
            "SettingsTitle",
            root.transform,
            "SETTINGS",
            62,
            menuWhite,
            new Vector2(0.5f, 0.78f),
            new Vector2(900f, 90f),
            TextAnchor.MiddleCenter
        );

        hubPage = CreatePage("SettingsHub");
        soundPage = CreatePage("SoundPage");
        controlsPage = CreatePage("ControlsPage");

        BuildHubPage();
        BuildSoundPage();
        BuildControlsPage();

        statusText = CreateText(
            "SettingsStatus",
            root.transform,
            string.Empty,
            20,
            new Color(0.78f, 0.78f, 0.78f, 1f),
            new Vector2(0.5f, 0.12f),
            new Vector2(1300f, 45f),
            TextAnchor.MiddleCenter
        );
    }

    private GameObject CreatePage(string name)
    {
        RectTransform rect = CreateRect(
            name,
            root.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        return rect.gameObject;
    }

    private void BuildHubPage()
    {
        AddPageLink(Page.Sound, "SOUND", 0.57f);
        AddPageLink(Page.Controls, "CONTROLS", 0.49f);
        AddHubBack("BACK", 0.41f);
    }

    private void BuildSoundPage()
    {
        AddSoundRow(SoundTrack.Music, 0.58f);
        AddSoundRow(SoundTrack.Sfx, 0.48f);
        AddSoundRow(SoundTrack.Ambience, 0.38f);
        AddSoundBack("BACK", 0.26f);
    }

    private void BuildControlsPage()
    {
        RectTransform listRect = CreateRect(
            "ControlsList",
            controlsPage.transform,
            new Vector2(0.5f, 0.43f),
            new Vector2(0.5f, 0.43f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(920f, 430f)
        );

        controlsList = listRect.gameObject;

        VerticalLayoutGroup layout = controlsList.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        AddRebindEntry("Player/Move", "<Keyboard>/a", "A");
        AddRebindEntry("Player/Move", "<Keyboard>/d", "D");
        AddRebindEntry("Player/Jump", "<Keyboard>/space", "SPACE");
        AddRebindEntry("Player/Attack", "<Keyboard>/x", "X");
        AddRebindEntry("Player/Pause", "<Keyboard>/escape", "ESC");
        AddControlSimple(EntryType.ResetBindings, "RESET DEFAULTS");
        AddControlSimple(EntryType.Back, "BACK");
    }

    private void AddPageLink(Page targetPage, string label, float y)
    {
        Entry entry = new Entry
        {
            type = EntryType.PageLink,
            targetPage = targetPage,
            label = CreateText(
                label,
                hubPage.transform,
                label,
                34,
                menuWhite,
                new Vector2(0.5f, y),
                new Vector2(720f, 54f),
                TextAnchor.MiddleCenter
            )
        };


        hubEntries.Add(entry);
    }

    private void AddHubBack(string label, float y)
    {
        Entry entry = new Entry
        {
            type = EntryType.Back,
            label = CreateText(
                label,
                hubPage.transform,
                label,
                30,
                menuWhite,
                new Vector2(0.5f, y),
                new Vector2(720f, 50f),
                TextAnchor.MiddleCenter
            )
        };


        hubEntries.Add(entry);
    }

    private void AddSoundRow(SoundTrack track, float y)
    {
        Entry entry = new Entry
        {
            type = EntryType.SoundRow,
            soundTrack = track
        };

        entry.label = CreateText(
            $"{TrackName(track)} Label",
            soundPage.transform,
            TrackName(track),
            30,
            menuWhite,
            new Vector2(0.24f, y),
            new Vector2(260f, 50f),
            TextAnchor.MiddleRight
        );

        entry.slider = CreateSlider(
            $"{TrackName(track)} Slider",
            soundPage.transform,
            new Vector2(0.50f, y),
            out Image handleImage
        );

        entry.sliderHandle = handleImage;

        entry.valueText = CreateText(
            $"{TrackName(track)} Value",
            soundPage.transform,
            string.Empty,
            28,
            menuWhite,
            new Vector2(0.69f, y),
            new Vector2(120f, 50f),
            TextAnchor.MiddleCenter
        );

        entry.buttonText = CreateText(
            $"{TrackName(track)} Mute",
            soundPage.transform,
            string.Empty,
            26,
            menuWhite,
            new Vector2(0.82f, y),
            new Vector2(220f, 50f),
            TextAnchor.MiddleLeft
        );




        soundEntries.Add(entry);
    }

    private void AddSoundBack(string label, float y)
    {
        Entry entry = new Entry
        {
            type = EntryType.Back,
            label = CreateText(
                label,
                soundPage.transform,
                label,
                30,
                menuWhite,
                new Vector2(0.5f, y),
                new Vector2(720f, 50f),
                TextAnchor.MiddleCenter
            )
        };


        soundEntries.Add(entry);
    }

    private RectTransform CreateControlRow(string name)
    {
        RectTransform row = CreateRect(
            name,
            controlsList.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(900f, 52f)
        );

        LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = 900f;
        layoutElement.preferredWidth = 900f;
        layoutElement.minHeight = 52f;
        layoutElement.preferredHeight = 52f;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        return row;
    }

    private void AddRebindEntry(
        string actionPath,
        string originalBindingPath,
        string fallbackDisplay)
    {
        Entry entry = new Entry
        {
            type = EntryType.Rebind,
            actionPath = actionPath,
            originalBindingPath = originalBindingPath,
            fallbackDisplay = fallbackDisplay
        };

        RectTransform row = CreateControlRow(
            $"{actionPath} {originalBindingPath} Row"
        );

        entry.selectorText = CreateText(
            $"{actionPath} {originalBindingPath} Selector",
            row,
            string.Empty,
            30,
            selectedColor,
            new Vector2(0.08f, 0.5f),
            new Vector2(70f, 50f),
            TextAnchor.MiddleRight
        );

        entry.label = CreateText(
            $"{actionPath} {originalBindingPath}",
            row,
            string.Empty,
            30,
            menuWhite,
            new Vector2(0.55f, 0.5f),
            new Vector2(760f, 50f),
            TextAnchor.MiddleLeft
        );


        controlEntries.Add(entry);
    }

    private void AddControlSimple(EntryType type, string label)
    {
        Entry entry = new Entry
        {
            type = type
        };

        RectTransform row = CreateControlRow($"{label} Row");

        entry.selectorText = CreateText(
            $"{label} Selector",
            row,
            string.Empty,
            28,
            selectedColor,
            new Vector2(0.08f, 0.5f),
            new Vector2(70f, 48f),
            TextAnchor.MiddleRight
        );

        entry.label = CreateText(
            label,
            row,
            label,
            28,
            menuWhite,
            new Vector2(0.55f, 0.5f),
            new Vector2(760f, 48f),
            TextAnchor.MiddleLeft
        );


        controlEntries.Add(entry);
    }

    private Slider CreateSlider(
        string objectName,
        Transform parent,
        Vector2 anchor,
        out Image handleImage)
    {
        RectTransform sliderRect = CreateRect(
            objectName,
            parent,
            anchor,
            anchor,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(360f, 32f)
        );

        Image background = sliderRect.gameObject.AddComponent<Image>();
        background.sprite = whiteSprite;
        background.color = sliderBackgroundColor;
        background.raycastTarget = false;

        RectTransform fillRect = CreateRect(
            "Fill",
            sliderRect,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(-8f, -8f)
        );

        Image fillImage = fillRect.gameObject.AddComponent<Image>();
        fillImage.sprite = whiteSprite;
        fillImage.color = sliderFillColor;
        fillImage.raycastTarget = false;

        RectTransform handleRect = CreateRect(
            "Handle",
            sliderRect,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(20f, 38f)
        );

        handleImage = handleRect.gameObject.AddComponent<Image>();
        handleImage.sprite = whiteSprite;
        handleImage.color = menuWhite;
        handleImage.raycastTarget = false;

        Slider slider = sliderRect.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;

        return slider;
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

    private void OnDestroy()
    {
        CancelRebind();

        if (whiteSprite != null)
        {
            Destroy(whiteSprite);
        }

        if (whiteTexture != null)
        {
            Destroy(whiteTexture);
        }

    }

    private void RestoreUIActionState()
    {
        if (navigateAction != null && !navigateWasEnabled)
        {
            navigateAction.Disable();
        }

        if (submitAction != null && !submitWasEnabled)
        {
            submitAction.Disable();
        }

        if (cancelAction != null && !cancelWasEnabled)
        {
            cancelAction.Disable();
        }
    }


    private static int FindBindingIndex(InputAction action, string originalPath)
    {
        if (action == null)
        {
            return -1;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (string.Equals(
                action.bindings[i].path,
                originalPath,
                StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static void ApplySavedBindings(InputActionAsset actions)
    {
        // Apply saved overrides once for each Input Action asset instance.
        if (actions == null)
        {
            return;
        }

        if (BindingAssetsLoaded.Contains(actions))
        {
            return;
        }

        BindingAssetsLoaded.Add(actions);

        if (!PlayerPrefs.HasKey(BindingOverridesKey))
        {
            return;
        }

        string json = PlayerPrefs.GetString(BindingOverridesKey, string.Empty);

        if (!string.IsNullOrEmpty(json))
        {
            actions.LoadBindingOverridesFromJson(json);
        }
    }

    public static void SaveBindings(InputActionAsset actions)
    {
        if (actions == null)
        {
            return;
        }

        PlayerPrefs.SetString(
            BindingOverridesKey,
            actions.SaveBindingOverridesAsJson()
        );
        PlayerPrefs.Save();
    }

    public static string GetMoveLeftDisplay(InputActionAsset actions)
    {
        return GetBindingDisplay(
            actions,
            "Player/Move",
            "<Keyboard>/a",
            "A"
        );
    }

    public static string GetMoveRightDisplay(InputActionAsset actions)
    {
        return GetBindingDisplay(
            actions,
            "Player/Move",
            "<Keyboard>/d",
            "D"
        );
    }

    public static string GetJumpDisplay(InputActionAsset actions)
    {
        return GetBindingDisplay(
            actions,
            "Player/Jump",
            "<Keyboard>/space",
            "SPACE"
        );
    }

    public static string GetAttackDisplay(InputActionAsset actions)
    {
        return GetBindingDisplay(
            actions,
            "Player/Attack",
            "<Keyboard>/x",
            "X"
        );
    }

    public static string GetPauseDisplay(InputActionAsset actions)
    {
        return GetBindingDisplay(
            actions,
            "Player/Pause",
            "<Keyboard>/escape",
            "ESC"
        );
    }

    private static string GetBindingDisplay(
        InputActionAsset actions,
        string actionPath,
        string originalPath,
        string fallback)
    {
        if (actions == null)
        {
            return fallback;
        }

        InputAction action = actions.FindAction(actionPath);
        int bindingIndex = FindBindingIndex(action, originalPath);

        if (action == null || bindingIndex < 0)
        {
            return fallback;
        }

        string display = action.GetBindingDisplayString(
            bindingIndex,
            InputBinding.DisplayStringOptions.DontIncludeInteractions
        );

        return string.IsNullOrWhiteSpace(display)
            ? fallback
            : display.ToUpperInvariant();
    }
}
