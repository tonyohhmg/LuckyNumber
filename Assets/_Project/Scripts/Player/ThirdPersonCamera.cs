using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform playerTransform; // Drag your Player GameObject here

    [Header("Camera Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0.8f, 1.8f, -3.0f); // Right offset, height, and distance back
    [SerializeField] private float smoothSpeed = 10.0f;

    [Header("Rotation")]
    [SerializeField] private bool lookAtPlayer = true;

    private void FixedUpdate() // Corrected with parentheses!
    {
        if (playerTransform == null) return;

        // Calculate desired position based on player's position and rotation
        Vector3 targetPosition = playerTransform.position + playerTransform.TransformDirection(offset);

        // Smoothly interpolate position
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.fixedDeltaTime);

        // Look at the player (slightly above their feet)
        if (lookAtPlayer)
        {
            Vector3 lookTarget = playerTransform.position + Vector3.up * 1.0f;
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.fixedDeltaTime);
        }
    }
}