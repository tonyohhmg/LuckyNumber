using UnityEngine;
using UnityEngine.UIElements;

public class ObjectInteractionPrompt : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform playerTransform; // Drag your ghost player here (auto-finds SoulMassSystem if left empty)
    [SerializeField] private float interactionRadius = 3.5f; // Distance threshold to show the prompt
    [SerializeField] private LayerMask pickupLayer = -1; // Optional layer restriction

    private UIDocument uiDocument;
    private VisualElement promptContainer;
    private Label promptLabel;

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
            promptLabel = root.Q<Label>("PromptLabel");
        }

        HidePrompt();
    }

    private void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        if (playerTransform == null || promptContainer == null || promptLabel == null) return;

        // Check for any colliders within the interaction radius around the player
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
        promptLabel.text = $"{itemName} <color=#00E5FF>{weight:F1}g</color>";
        promptContainer.style.display = DisplayStyle.Flex;
    }

    private void HidePrompt()
    {
        if (promptContainer != null)
        {
            promptContainer.style.display = DisplayStyle.None;
        }
    }

    // Draws a wireframe sphere in the Scene view so you can easily visualize the radius
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerTransform.position, interactionRadius);
        }
    }
}