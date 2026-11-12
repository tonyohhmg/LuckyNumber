using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UIDocument))]
public class MainMenuHandler : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    private UIDocument uiDocument;
    private Button playButton;
    private Button optionsButton;
    private Button closeOptionsButton;

    // Options Panel
    private VisualElement optionsPanel;

    // Custom Slider Elements
    private VisualElement volumeSlider;
    private VisualElement volumeSliderFill;
    private VisualElement volumeSliderThumb;
    private Label volumeValueLabel;
    private bool isDraggingSlider = false;

    // Custom Toggle Elements
    private VisualElement muteToggle;
    private VisualElement fullscreenToggle;
    private bool isMuted = false;
    private bool isFullscreen = false;

    // Control hint containers from UXML
    private VisualElement keyboardControls;
    private VisualElement keyboardControlsTwo;
    private VisualElement gamepadControls;

    private bool isUsingGamepad = false;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            
            // Buttons
            playButton = root.Q<Button>("PlayButton");
            optionsButton = root.Q<Button>("OptionsButton");
            closeOptionsButton = root.Q<Button>("CloseOptionsButton");

            // Sub-Panel
            optionsPanel = root.Q<VisualElement>("OptionsPanel");

            // Custom Slider Queries
            volumeSlider = root.Q<VisualElement>("CustomVolumeSlider");
            volumeSliderFill = root.Q<VisualElement>("VolumeSliderFill");
            volumeSliderThumb = root.Q<VisualElement>("VolumeSliderThumb");
            volumeValueLabel = root.Q<Label>("VolumeValueLabel");

            // Custom Toggle Queries
            muteToggle = root.Q<VisualElement>("CustomMuteToggle");
            fullscreenToggle = root.Q<VisualElement>("CustomFullscreenToggle");

            // Control Hints
            keyboardControls = root.Q<VisualElement>("KeyboardControls");
            keyboardControlsTwo = root.Q<VisualElement>("KeyboardControlsTwo");
            gamepadControls = root.Q<VisualElement>("GamepadControls");
        }

        // Hook up Buttons
        RegisterButton(playButton, OnPlayClicked);
        RegisterButton(optionsButton, ToggleOptionsPanel);
        RegisterButton(closeOptionsButton, ToggleOptionsPanel);

        // Initialize & Hook up Custom Volume Slider
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        SetVolumeVisuals(savedVolume);
        AudioListener.volume = savedVolume;

        if (volumeSlider != null)
        {
            volumeSlider.RegisterCallback<PointerDownEvent>(OnSliderPointerDown);
            volumeSlider.RegisterCallback<PointerMoveEvent>(OnSliderPointerMove);
            volumeSlider.RegisterCallback<PointerUpEvent>(OnSliderPointerUp);
        }

        // Initialize & Hook up Custom Mute Toggle
        isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
        AudioListener.pause = isMuted;
        UpdateToggleVisuals(muteToggle, isMuted);
        if (muteToggle != null)
        {
            muteToggle.RegisterCallback<ClickEvent>(OnMuteClicked);
        }

        // Initialize & Hook up Custom Fullscreen Toggle
        isFullscreen = Screen.fullScreen;
        UpdateToggleVisuals(fullscreenToggle, isFullscreen);
        if (fullscreenToggle != null)
        {
            fullscreenToggle.RegisterCallback<ClickEvent>(OnFullscreenClicked);
        }

        Time.timeScale = 1f;
        CheckActiveController();
    }

    private void OnDisable()
    {
        UnregisterButton(playButton, OnPlayClicked);
        UnregisterButton(optionsButton, ToggleOptionsPanel);
        UnregisterButton(closeOptionsButton, ToggleOptionsPanel);

        if (volumeSlider != null)
        {
            volumeSlider.UnregisterCallback<PointerDownEvent>(OnSliderPointerDown);
            volumeSlider.UnregisterCallback<PointerMoveEvent>(OnSliderPointerMove);
            volumeSlider.UnregisterCallback<PointerUpEvent>(OnSliderPointerUp);
        }

        if (muteToggle != null) muteToggle.UnregisterCallback<ClickEvent>(OnMuteClicked);
        if (fullscreenToggle != null) fullscreenToggle.UnregisterCallback<ClickEvent>(OnFullscreenClicked);
    }

    private void RegisterButton(Button btn, Action onClick)
    {
        if (btn == null) return;
        btn.clicked -= onClick;
        btn.clicked += onClick;
        btn.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
        btn.RegisterCallback<PointerEnterEvent>(OnButtonHover);
    }

    private void UnregisterButton(Button btn, Action onClick)
    {
        if (btn == null) return;
        btn.clicked -= onClick;
        btn.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
    }

    private void Update()
    {
        CheckActiveController();
    }

    private void ToggleOptionsPanel()
    {
        if (optionsPanel == null) return;
        bool isCurrentlyVisible = optionsPanel.style.display == DisplayStyle.Flex;
        optionsPanel.style.display = isCurrentlyVisible ? DisplayStyle.None : DisplayStyle.Flex;
    }

    #region Custom Slider Logic
    private void OnSliderPointerDown(PointerDownEvent evt)
    {
        isDraggingSlider = true;
        volumeSlider.CapturePointer(evt.pointerId);
        UpdateSliderFromPointer(evt.localPosition.x);
    }

    private void OnSliderPointerMove(PointerMoveEvent evt)
    {
        if (!isDraggingSlider) return;
        UpdateSliderFromPointer(evt.localPosition.x);
    }

    private void OnSliderPointerUp(PointerUpEvent evt)
    {
        if (!isDraggingSlider) return;
        isDraggingSlider = false;
        volumeSlider.ReleasePointer(evt.pointerId);
    }

    private void UpdateSliderFromPointer(float localX)
    {
        float width = volumeSlider.resolvedStyle.width;
        if (width <= 0f) return;

        float normalized = Mathf.Clamp01(localX / width);
        SetVolumeVisuals(normalized);

        AudioListener.volume = normalized;
        PlayerPrefs.SetFloat("MasterVolume", normalized);
    }

    private void SetVolumeVisuals(float normalizedValue)
    {
        float percentage = normalizedValue * 100f;
        if (volumeSliderFill != null) volumeSliderFill.style.width = Length.Percent(percentage);
        if (volumeSliderThumb != null) volumeSliderThumb.style.left = Length.Percent(percentage);
        if (volumeValueLabel != null) volumeValueLabel.text = $"{Mathf.RoundToInt(percentage)}%";
    }
    #endregion

    #region Custom Toggle Logic
    private void OnMuteClicked(ClickEvent evt)
    {
        isMuted = !isMuted;
        AudioListener.pause = isMuted;
        PlayerPrefs.SetInt("AudioMuted", isMuted ? 1 : 0);
        UpdateToggleVisuals(muteToggle, isMuted);
    }

    private void OnFullscreenClicked(ClickEvent evt)
    {
        isFullscreen = !isFullscreen;
        Screen.fullScreen = isFullscreen;
        UpdateToggleVisuals(fullscreenToggle, isFullscreen);
    }

    private void UpdateToggleVisuals(VisualElement toggleElement, bool isChecked)
    {
        if (toggleElement == null) return;
        if (isChecked)
            toggleElement.AddToClassList("custom-toggle--checked");
        else
            toggleElement.RemoveFromClassList("custom-toggle--checked");
    }
    #endregion

    private void CheckActiveController()
    {
        bool gamepadPressed = false;

        if (Gamepad.current != null)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control.IsPressed() && !control.synthetic)
                {
                    gamepadPressed = true;
                    break;
                }
            }
        }

        if (gamepadPressed)
        {
            SetControlType(true);
        }
        else if ((Keyboard.current != null && Keyboard.current.anyKey.isPressed) || 
                 (Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero))
        {
            SetControlType(false);
        }
    }

    private void SetControlType(bool useGamepad)
    {
        if (isUsingGamepad == useGamepad) return;

        isUsingGamepad = useGamepad;

        StyleEnum<DisplayStyle> keyboardDisplay = useGamepad ? DisplayStyle.None : DisplayStyle.Flex;
        StyleEnum<DisplayStyle> gamepadDisplay = useGamepad ? DisplayStyle.Flex : DisplayStyle.None;

        if (keyboardControls != null) keyboardControls.style.display = keyboardDisplay;
        if (keyboardControlsTwo != null) keyboardControlsTwo.style.display = keyboardDisplay;
        if (gamepadControls != null) gamepadControls.style.display = gamepadDisplay;
    }

    private void OnButtonHover(PointerEnterEvent evt)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonHoverSound();
        }
    }

    private void OnPlayClicked()
    {
        Debug.Log($"[MainMenuHandler] Loading gameplay scene: {gameplaySceneName}");
        SceneManager.LoadScene(gameplaySceneName);
    }
}