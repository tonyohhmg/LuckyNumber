using UnityEngine;

public class TimedItemRelocator : MonoBehaviour
{
    [Header("Relocation Settings")]
    [Tooltip("Time in seconds before the unabsorbed item relocates.")]
    [SerializeField] private float relocationInterval = 8.0f;

    [Tooltip("Maximum radius from its current position where the item can reappear.")]
    [SerializeField] private float maxRelocationRadius = 5.0f;

    [Header("Visual Feedback")]
    [Tooltip("Particle system prefab to spawn when the item disappears and reappears.")]
    [SerializeField] private GameObject blinkParticlePrefab;

    private PickupItem pickupItem;
    private float timer = 0f;

    private void Awake()
    {
        pickupItem = GetComponent<PickupItem>();
    }

    private void Update()
    {
        // If the item has already been absorbed, stop the relocation timer
        if (pickupItem != null && !pickupItem.CanBeAbsorbed) return;

        timer += Time.deltaTime;
        if (timer >= relocationInterval)
        {
            timer = 0f;
            RelocateItem();
        }
    }

    private void RelocateItem()
    {
        Vector3 oldPos = transform.position;

        // 1. Generate a random point within a circle radius
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * maxRelocationRadius;
        Vector3 newPos = new Vector3(
            oldPos.x + randomCircle.x,
            oldPos.y,
            oldPos.z + randomCircle.y
        );

        // 2. Cast a ray downward to ensure the item snaps to the floor/terrain
        if (Physics.Raycast(newPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        {
            newPos.y = hit.point.y + 0.2f; // Slight offset above ground
        }

        // 3. Spawn particle effect at the OLD position before teleporting
        if (blinkParticlePrefab != null)
        {
            GameObject vanishEffect = Instantiate(blinkParticlePrefab, oldPos, Quaternion.identity);
            Destroy(vanishEffect, 3f); // Auto-cleanup particle object after 3 seconds
        }

        // --- NEW: Play central audio effect when the item disappears ---
        AudioManager.Instance?.PlayItemRelocateSound();

        // 4. Move to new position
        transform.position = newPos;

        // 5. Spawn particle effect at the NEW position as it appears
        if (blinkParticlePrefab != null)
        {
            GameObject appearEffect = Instantiate(blinkParticlePrefab, newPos, Quaternion.identity);
            Destroy(appearEffect, 3f);
        }

        Debug.Log($"[TimedItemRelocator] Item {gameObject.name} blinked from {oldPos} to {newPos}");
    }
}