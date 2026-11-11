using UnityEngine;

public class CompanionFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float followSpeed = 3.5f;     // Speed at which it drifts toward the player
    [SerializeField] private float minDistance = 2.0f;     // Distance it tries to maintain
    [SerializeField] private float rotationSmoothness = 5f;

    [Header("Natural Drift & Independent Floating")]
    [SerializeField] private float heightAbovePlayer = 1.2f; // Base hover height
    [SerializeField] private float floatAmplitude = 0.5f;    // How high/low the independent float goes
    [SerializeField] private float floatFrequency = 0.8f;    // Speed of the independent float cycle

    private bool isFollowing = false;
    private Vector3 currentVelocity; // Used for SmoothDamp lag
    private float noiseSeed;         // Clean unique offset per instance

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("[CompanionFollower] Player transform not assigned and 'Player' tag not found!");
            }
        }

        // Generate a random noise offset so multiple companions (if any) don't float identically
        noiseSeed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (!isFollowing || playerTransform == null) return;

        // 1. Target position is slightly behind/around the player on the floor plane
        Vector3 targetPosition = playerTransform.position - (playerTransform.forward * minDistance);

        // 2. Smoothly lag behind the player's movement (removes the rigid, exact-copy feel)
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref currentVelocity, 
            1f / followSpeed
        );

        // 3. Completely independent vertical floating using Perlin noise 
        // (This ensures the companion's up/down motion doesn't match Nix's jumping or terrain changes)
        float noiseValue = Mathf.PerlinNoise((Time.time * floatFrequency) + noiseSeed, 0.0f);
        float independentFloatY = playerTransform.position.y + heightAbovePlayer + ((noiseValue - 0.5f) * 2f * floatAmplitude);

        smoothedPosition.y = Mathf.Lerp(transform.position.y, independentFloatY, Time.deltaTime * 3f);

        // Apply final position
        transform.position = smoothedPosition;

        // 4. Softly look toward movement direction or player
        Vector3 lookDir = (playerTransform.position - transform.position).normalized;
        lookDir.y = 0; // Keep rotation flat
        
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothness);
        }
    }

    public void EnableFollow()
    {
        isFollowing = true;
        Debug.Log("[CompanionFollower] Companion unbound and now floating independently with loose lag.");
    }
}