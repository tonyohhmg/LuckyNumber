using UnityEngine;
using UnityEngine.UIElements;

public class GameOverMenuHandler : MonoBehaviour
{
    private UIDocument uiDocument;
    private Button restartButton;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            restartButton = root.Q<Button>("GameOverRestartButton");
        }

        if (restartButton != null)
        {
            // Unregister first to avoid duplicate bindings
            restartButton.clicked -= OnRestartClicked;
            restartButton.clicked += OnRestartClicked;

            // Hover sound listener
            restartButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            restartButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
        }
    }

    private void OnDisable()
    {
        if (restartButton != null)
        {
            restartButton.clicked -= OnRestartClicked;
            restartButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
        }
    }

    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
        {
            // Returns to the main menu screen to restart the full game run from the beginning
            GameManager.Instance.GoToMainMenu(); 
        }
    }

    private void OnButtonHover(PointerEnterEvent evt)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonHoverSound();
        }
    }
}