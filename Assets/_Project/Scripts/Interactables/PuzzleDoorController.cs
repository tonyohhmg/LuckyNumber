using UnityEngine;

public class PuzzleDoorController : MonoBehaviour
{
    [Header("Door Animation / Movement")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, -3f, 0f); // How far the door slides/sinks
    [SerializeField] private float moveSpeed = 2.0f;

    private Vector3 closedPosition;
    private Vector3 targetPosition;

    private void Awake()
    {
        closedPosition = transform.position;
        targetPosition = closedPosition;
    }

    private void Update()
    {
        // Smoothly slide the door toward its target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    // Hook this method directly up to your WeightTriggerPlatform's "On Equilibrium Activated" UnityEvent in the Inspector!
    public void OpenDoor()
    {
        targetPosition = closedPosition + openOffset;
        Debug.Log("[PuzzleDoorController] Door opened via 21g equilibrium!");
    }

    // Hook this up to "On Equilibrium Deactivated" if you want the door to shut when stepping off
    public void CloseDoor()
    {
        targetPosition = closedPosition;
        Debug.Log("[PuzzleDoorController] Equilibrium lost. Door closing.");
    }
}