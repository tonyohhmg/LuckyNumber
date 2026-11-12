using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MassUIHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SoulMassSystem playerMassSystem;
    [SerializeField] private CompanionSoulSystem companionMassSystem; 
    [SerializeField] private Transform companionTransform;             

    private UIDocument uiDocument;
    
    // Nix Mass HUD elements
    private Label currentWeightLabel;
    private Label targetWeightLabel;

    // Duncan Mass HUD elements
    private VisualElement duncanContainer;
    private Label companionCurrentWeightLabel;
    private Label companionTargetWeightLabel;
    
    // Lives & Level HUD elements
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
            
            // Query Nix Mass elements
            currentWeightLabel = root.Q<Label>("CurrentWeightLabel");
            targetWeightLabel = root.Q<Label>("TargetWeightLabel");

            // Query Duncan Mass elements
            duncanContainer = root.Q<VisualElement>("DuncanContainer");
            companionCurrentWeightLabel = root.Q<Label>("CompanionCurrentWeightLabel");
            companionTargetWeightLabel = root.Q<Label>("CompanionTargetWeightLabel");
            
            // Query Lives & Level elements
            levelNameLabel = root.Q<Label>("LevelNameLabel");
            objectiveLabel = root.Q<Label>("ObjectiveLabel");
            
            lifeIcons[0] = root.Q<VisualElement>("Life1");
            lifeIcons[1] = root.Q<VisualElement>("Life2");
            lifeIcons[2] = root.Q<VisualElement>("Life3");

            // Query Speech Bubble elements
            speechBubbleContainer = root.Q<VisualElement>("CompanionSpeechBubble");
            speechBubbleLabel = root.Q<Label>("SpeechBubbleLabel");
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (levelNameLabel != null)
        {
            string formattedLevelName = currentScene switch
            {
                "GameplayLevel1" => "ECHOES OF THE PAST",
                "GameplayLevel2" => "SOULMATES",
                "GameplayLevel3" => "PHANTOM CONVERGENCE",
                _ => currentScene.ToUpper()
            };
            levelNameLabel.text = formattedLevelName;
        }

        UpdateSceneUIState(currentScene);

        // Register Player Weight Events
        if (playerMassSystem != null)
        {
            playerMassSystem.OnWeightChanged += UpdatePlayerWeightUI;
            UpdatePlayerWeightUI(playerMassSystem.CurrentWeight, playerMassSystem.TargetMass);
        }

        // Register Companion Weight Events
        if (companionMassSystem != null)
        {
            companionMassSystem.OnCompanionWeightChanged += UpdateCompanionWeightUI;
            UpdateCompanionWeightUI(companionMassSystem.CurrentMass, 21.0f);
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
            playerMassSystem.OnWeightChanged -= UpdatePlayerWeightUI;
        }

        if (companionMassSystem != null)
        {
            companionMassSystem.OnCompanionWeightChanged -= UpdateCompanionWeightUI;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged -= UpdateLivesDisplay;
        }
    }

    private void Update()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "GameplayLevel2" || currentScene == "GameplayLevel3")
        {
            // Check if Duncan has reached equilibrium (Phase 1 complete)
            if (companionMassSystem != null && !playerHasFreedCompanion && companionMassSystem.IsEquilibrium)
            {
                playerHasFreedCompanion = true;

                if (objectiveLabel != null)
                {
                    // --- PHASE 2 OBJECTIVE FOR LEVEL 3 ---
                    objectiveLabel.text = "Absorb 21.0 echoes to reach equilibrium.";
                }

                if (speechBubbleContainer != null)
                {
                    speechBubbleContainer.style.display = DisplayStyle.None;
                }
            }

            if (currentScene == "GameplayLevel2" && !playerHasFreedCompanion && companionTransform != null && speechBubbleContainer != null)
            {
                UpdateSpeechBubblePosition();
            }
        }
    }

    private void UpdateSpeechBubblePosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Target position 1.8 units above Duncan's world position
        Vector3 worldPos = companionTransform.position + Vector3.up * 1.8f;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z > 0 && uiDocument != null && uiDocument.rootVisualElement != null)
        {
            speechBubbleContainer.style.display = DisplayStyle.Flex;

            // Convert bottom-left screen origin to top-left screen origin
            Vector2 screenPoint = new Vector2(screenPos.x, Screen.height - screenPos.y);

            // Transform screen coordinates into UI Toolkit panel space (respects UI scaling)
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(uiDocument.rootVisualElement.panel, screenPoint);

            speechBubbleContainer.style.left = panelPos.x;
            speechBubbleContainer.style.top = panelPos.y;
        }
        else
        {
            if (speechBubbleContainer != null)
            {
                speechBubbleContainer.style.display = DisplayStyle.None;
            }
        }
    }

    private void UpdateSceneUIState(string sceneName)
    {
        if (objectiveLabel == null) return;

        if (sceneName == "GameplayLevel1")
        {
            objectiveLabel.text = "Absorb 21.0 echoes to reach equilibrium.";
            if (duncanContainer != null) duncanContainer.style.display = DisplayStyle.None;
        }
        else if (sceneName == "GameplayLevel2")
        {
            objectiveLabel.text = "Absorb 21.0 echoes to free your trapped companion.";
            if (duncanContainer != null) duncanContainer.style.display = DisplayStyle.None;
        }
        else if (sceneName == "GameplayLevel3")
        {
            // --- PHASE 1 OBJECTIVE FOR LEVEL 3 ---
            objectiveLabel.text = "Absorb 21.0 echoes and transfer to Duncan.";
            if (duncanContainer != null) duncanContainer.style.display = DisplayStyle.Flex;
            playerHasFreedCompanion = false; // Reset to track phase transition
        }
    }

    private void UpdatePlayerWeightUI(float current, float max)
    {
        if (currentWeightLabel != null) currentWeightLabel.text = $"{current:F1}";
        if (targetWeightLabel != null) targetWeightLabel.text = $"{max:F1}";
    }

    private void UpdateCompanionWeightUI(float current, float max)
    {
        if (companionCurrentWeightLabel != null) companionCurrentWeightLabel.text = $"{current:F1}";
        if (companionTargetWeightLabel != null) companionTargetWeightLabel.text = $"{max:F1}";
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