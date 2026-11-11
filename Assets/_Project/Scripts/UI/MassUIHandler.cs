using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MassUIHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SoulMassSystem playerMassSystem;
    [SerializeField] private CompanionSoulSystem companionMassSystem; // Drag companion here for Level 2
    [SerializeField] private Transform companionTransform;             // Drag companion transform here for position tracking

    private UIDocument uiDocument;
    
    // Mass HUD elements
    private Label currentWeightLabel;
    private Label targetWeightLabel;
    
    // Lives, Level & Objective HUD elements
    private Label levelNameLabel;
    private Label objectiveLabel;
    private VisualElement[] lifeIcons = new VisualElement[3];

    // Speech Bubble Prompt Element
    private VisualElement speechBubbleContainer;
    private Label speechBubbleLabel;

    private bool playerHasFreedCompanion = false;
    private Camera mainCamera;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        mainCamera = Camera.main;

        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            
            // Query Mass elements
            currentWeightLabel = root.Q<Label>("CurrentWeightLabel");
            targetWeightLabel = root.Q<Label>("TargetWeightLabel");
            
            // Query Lives, Level & Objective elements
            levelNameLabel = root.Q<Label>("LevelNameLabel");
            objectiveLabel = root.Q<Label>("ObjectiveLabel");
            
            lifeIcons[0] = root.Q<VisualElement>("Life1");
            lifeIcons[1] = root.Q<VisualElement>("Life2");
            lifeIcons[2] = root.Q<VisualElement>("Life3");

            // Query Speech Bubble elements
            speechBubbleContainer = root.Q<VisualElement>("CompanionSpeechBubble");
            speechBubbleLabel = root.Q<Label>("SpeechBubbleLabel");
        }

        // Setup Level Name and Dynamic Level 2 Objectives based on active scene
        string currentScene = SceneManager.GetActiveScene().name;
        if (levelNameLabel != null)
        {
            string formattedLevelName = currentScene switch
            {
                "GameplayLevel1" => "TWENTY-ONE ECHOES",
                "GameplayLevel2" => "THE VEILED CORRIDOR",
                "GameplayLevel3" => "ETHEREAL ZENITH",
                _ => currentScene.ToUpper()
            };
            levelNameLabel.text = formattedLevelName;
        }

        // Set initial objective text
        UpdateObjectiveText(currentScene);

        // Register Weight Events
        if (playerMassSystem != null)
        {
            playerMassSystem.OnWeightChanged += UpdateWeightUI;
            UpdateWeightUI(playerMassSystem.CurrentWeight, 21.0f);
        }

        // Register Lives Events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged += UpdateLivesDisplay;
            UpdateLivesDisplay(GameManager.Instance.CurrentLives);
        }
    }

    private void OnDisable()
    {
        if (playerMassSystem != null)
        {
            playerMassSystem.OnWeightChanged -= UpdateWeightUI;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged -= UpdateLivesDisplay;
        }
    }

    private void Update()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Dynamic objective switching for Level 2
        if (currentScene == "GameplayLevel2")
        {
            if (playerMassSystem != null && !playerHasFreedCompanion && playerMassSystem.IsEquilibrium)
            {
                playerHasFreedCompanion = true;

                if (objectiveLabel != null)
                {
                    objectiveLabel.text = "Help your companion absorb to 21.0 echoes.";
                }

                // Hide speech bubble prompt once companion is freed
                if (speechBubbleContainer != null)
                {
                    speechBubbleContainer.style.display = DisplayStyle.None;
                }
            }

            // Screen-space position tracking for trapped companion prompt
            if (!playerHasFreedCompanion && companionTransform != null && speechBubbleContainer != null)
            {
                UpdateSpeechBubblePosition();
            }
        }
    }

    private void UpdateSpeechBubblePosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Target position 1.8 meters above companion center
        Vector3 worldPos = companionTransform.position + Vector3.up * 1.8f;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z > 0)
        {
            speechBubbleContainer.style.display = DisplayStyle.Flex;

            // Convert World-to-Screen Y (bottom-left origin) to UI Toolkit Y (top-left origin)
            float uiX = screenPos.x;
            float uiY = Screen.height - screenPos.y;

            speechBubbleContainer.style.left = uiX;
            speechBubbleContainer.style.top = uiY;
        }
        else
        {
            speechBubbleContainer.style.display = DisplayStyle.None;
        }
    }

    private void UpdateObjectiveText(string sceneName)
    {
        if (objectiveLabel == null) return;

        if (sceneName == "GameplayLevel2")
        {
            objectiveLabel.text = "Absorb 21.0 echoes to free your trapped companion.";
        }
        else if (sceneName == "GameplayLevel3")
        {
            objectiveLabel.text = "Balance the shifting weights & reach the exit.";
        }
        else
        {
            objectiveLabel.text = "Absorb 21.0 echoes for equilibrium.";
        }
    }

    private void UpdateWeightUI(float current, float max)
    {
        if (currentWeightLabel != null)
        {
            currentWeightLabel.text = $"{current:F1}";
        }

        if (targetWeightLabel != null)
        {
            targetWeightLabel.text = $"{max:F1}";
        }
    }

    private void UpdateLivesDisplay(int remainingLives)
    {
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;

            if (i < remainingLives)
            {
                lifeIcons[i].RemoveFromClassList("life-lost");
                lifeIcons[i].AddToClassList("life-active");
            }
            else
            {
                lifeIcons[i].RemoveFromClassList("life-active");
                lifeIcons[i].AddToClassList("life-lost");
            }
        }
    }
}