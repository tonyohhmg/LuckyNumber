using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UIDocument))]
public class MainMenuHandler : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene"; // Match your actual gameplay scene name exactly

    private UIDocument uiDocument;
    private Button playButton;

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
            playButton = root.Q<Button>("PlayButton"); // Matches name in MainMenu.uxml
            
            // Query control hint groups
            keyboardControls = root.Q<VisualElement>("KeyboardControls");
            keyboardControlsTwo = root.Q<VisualElement>("KeyboardControlsTwo");
            gamepadControls = root.Q<VisualElement>("GamepadControls");
        }

        if (playButton != null)
        {
            playButton.clicked -= OnPlayClicked;
            playButton.clicked += OnPlayClicked;

            playButton.UnregisterCallback<PointerEnterEvent>(OnPlayButtonHover);
            playButton.RegisterCallback<PointerEnterEvent>(OnPlayButtonHover);

            Debug.Log("[MainMenuHandler] PlayButton hooked up successfully.");
        }
        else
        {
            Debug.LogError("[MainMenuHandler] Could not find 'PlayButton' in UXML!");
        }

        Time.timeScale = 1f; // Ensure time is unpaused at the menu
        CheckActiveController(); // Force initial check
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.clicked -= OnPlayClicked;
            playButton.UnregisterCallback<PointerEnterEvent>(OnPlayButtonHover);
        }
    }

    private void Update()
    {
        // Continuously monitor if the player switches between Keyboard/Mouse and Gamepad
        CheckActiveController();
    }

    private void CheckActiveController()
    {
        bool gamepadPressed = false;

        // Check if a connected gamepad is currently providing input
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
        if (isUsingGamepad == useGamepad) return; // Avoid redundant style updates

        isUsingGamepad = useGamepad;

        StyleEnum<DisplayStyle> keyboardDisplay = useGamepad ? DisplayStyle.None : DisplayStyle.Flex;
        StyleEnum<DisplayStyle> gamepadDisplay = useGamepad ? DisplayStyle.Flex : DisplayStyle.None;

        if (keyboardControls != null) keyboardControls.style.display = keyboardDisplay;
        if (keyboardControlsTwo != null) keyboardControlsTwo.style.display = keyboardDisplay;
        if (gamepadControls != null) gamepadControls.style.display = gamepadDisplay;
    }

    private void OnPlayButtonHover(PointerEnterEvent evt)
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