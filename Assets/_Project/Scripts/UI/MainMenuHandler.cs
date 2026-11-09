using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class MainMenuHandler : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene"; // Match your actual gameplay scene name exactly

    private UIDocument uiDocument;
    private Button playButton;

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
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.clicked -= OnPlayClicked;
            playButton.UnregisterCallback<PointerEnterEvent>(OnPlayButtonHover);
        }
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