using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameWonMenuHandler : MonoBehaviour
{
    private UIDocument uiDocument;
    private Button restartButton; // Or "PlayAgainButton" depending on your UXML element name

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            // Find your button by its name in the UXML file (e.g., "RestartButton" or "PlayAgainButton")
            restartButton = root.Q<Button>("RestartButton"); 
        }

        if (restartButton != null)
        {
            restartButton.clicked += OnStartOverClicked;
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
        // Unfreeze time if it was paused
        Time.timeScale = 1f;

        // Take the player all the way back to the Main Menu from scratch
        SceneManager.LoadScene("MainMenuScene");
    }
}