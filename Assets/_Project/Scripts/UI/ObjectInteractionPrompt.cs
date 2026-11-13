using UnityEngine;
using UnityEngine.UIElements;

public class ObjectInteractionPrompt : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform playerTransform; // Drag your ghost player here (auto-finds SoulMassSystem if left empty)
    [SerializeField] private float interactionRadius = 4.0f; // Pure distance threshold around the ghost
    [SerializeField] private LayerMask pickupLayer = -1; // Set to Everything (-1) to catch items easily

    private UIDocument uiDocument;
    private VisualElement promptContainer;
    private Label nameLabel;
    private Label weightLabel;
    private Label echoesLabel;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();

        // Automatically find the player if not assigned in the inspector
        if (playerTransform == null)
        {
            SoulMassSystem soul = FindAnyObjectByType<SoulMassSystem>();
            if (soul != null)
            {
                playerTransform = soul.transform;
            }
            else
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }
        }
    }

    private void OnEnable()
    {
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            promptContainer = root.Q<VisualElement>("PromptContainer");
            nameLabel = root.Q<Label>("ArtifactNameLabel");
            weightLabel = root.Q<Label>("ArtifactWeightLabel");
            echoesLabel = root.Q<Label>("ArtifactWeightEchoes");
        }

        HidePrompt();
    }

    private void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        if (playerTransform == null || promptContainer == null || nameLabel == null || weightLabel == null || echoesLabel == null) return;

        // Pure proximity check around the player's world position — no looking at the object required!
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, interactionRadius, pickupLayer);

        PickupItem closestItem = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (var hit in hits)
        {
            PickupItem pickupItem = hit.GetComponent<PickupItem>();
            if (pickupItem != null && pickupItem.WeightData != null)
            {
                float sqrDist = (hit.transform.position - playerTransform.position).sqrMagnitude;
                if (sqrDist < closestDistanceSqr)
                {
                    closestDistanceSqr = sqrDist;
                    closestItem = pickupItem;
                }
            }
        }

        // Display prompt for the closest item found within range, or hide if none
        if (closestItem != null)
        {
            ShowPrompt(closestItem.WeightData.ItemName, closestItem.WeightData.WeightInGrams);
        }
        else
        {
            HidePrompt();
        }
    }

    private void ShowPrompt(string itemName, float weight)
    {
        nameLabel.text = itemName;
        weightLabel.text = $"{weight:F1}";
        echoesLabel.text = "echoes";
        promptContainer.style.display = DisplayStyle.Flex;
    }

    private void HidePrompt()
    {
        if (promptContainer != null)
        {
            promptContainer.style.display = DisplayStyle.None;
        }
    }

    // Draws a cyan wireframe sphere around your ghost in Scene view to visualize the proximity range
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerTransform.position, interactionRadius);
        }
    }
}