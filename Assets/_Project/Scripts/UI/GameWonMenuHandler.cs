using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameWonMenuHandler : MonoBehaviour
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
            restartButton = root.Q<Button>("GameWonRestartButton"); 
        }

        if (restartButton != null)
        {
            restartButton.clicked += OnStartOverClicked;

            // --- ADDED: Hover sound listener ---
            restartButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            restartButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
        }
        else
        {
            Debug.LogError("[GameWonMenuHandler] Could not find 'GameWonRestartButton' in UXML!");
        }
    }

    private void OnDisable()
    {
        if (restartButton != null)
        {
            restartButton.clicked -= OnStartOverClicked;
        }
    }

    private void OnStartOverClicked()
    {
        // Unfreeze time
        Time.timeScale = 1f;

        // Take the player back to the Main Menu
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnButtonHover(PointerEnterEvent evt)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonHoverSound();
        }
    }
}