using UnityEngine;
using UnityEngine.UIElements;

public class MassUIHandler : MonoBehaviour
{
    [SerializeField] private SoulMassSystem playerMassSystem;
    
    private UIDocument uiDocument;
    private Label currentWeightLabel;
    private Label targetWeightLabel;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            currentWeightLabel = root.Q<Label>("CurrentWeightLabel");
            targetWeightLabel = root.Q<Label>("TargetWeightLabel");
        }

        if (playerMassSystem != null)
        {
            playerMassSystem.OnWeightChanged += UpdateUI;
            // Initialize with starting value
            UpdateUI(playerMassSystem.CurrentWeight, 21.0f);
        }
    }

    private void OnDisable()
    {
        if (playerMassSystem != null)
        {
            playerMassSystem.OnWeightChanged -= UpdateUI;
        }
    }

    private void UpdateUI(float current, float max)
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
}