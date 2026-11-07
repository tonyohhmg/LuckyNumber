using UnityEngine;
using UnityEngine.UIElements;

public class MassUIHandler : MonoBehaviour
{
    [SerializeField] private SoulMassSystem playerMassSystem;
    
    private UIDocument uiDocument;
    private Label massDisplayLabel;
    private VisualElement indicatorElement;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            massDisplayLabel = root.Q<Label>("MassLabel");
            indicatorElement = root.Q<VisualElement>("MassIndicator"); // Optional element in UI Builder
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
        if (massDisplayLabel != null)
        {
            massDisplayLabel.text = $"{current:F1}g / {max:F1}g";
        }

        if (indicatorElement != null)
        {
            if (playerMassSystem != null && playerMassSystem.IsEquilibrium)
            {
                indicatorElement.style.backgroundColor = new StyleColor(new Color(1f, 0.85f, 0.2f)); // Golden 21g
            }
            else
            {
                indicatorElement.style.backgroundColor = new StyleColor(Color.black); // Default color
            }
        }
    }
}