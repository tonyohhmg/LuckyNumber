using UnityEngine;

public class TailEmissionController : MonoBehaviour
{
    [SerializeField] private CharacterController playerController; // Drag your player's CharacterController here
    [SerializeField] private float velocityThreshold = 0.1f;
    
    private ParticleSystem tailParticleSystem;
    private ParticleSystem.EmissionModule emissionModule;

    private void Awake()
    {
        tailParticleSystem = GetComponent<ParticleSystem>();
        emissionModule = tailParticleSystem.emission;

        // If not assigned manually, try to find it on parent
        if (playerController == null && transform.parent != null)
        {
            playerController = transform.parent.GetComponent<CharacterController>();
        }
    }

    private void Update()
    {
        if (playerController == null) return;

        // Check if the player is moving based on their character controller velocity
        float currentSpeed = playerController.velocity.magnitude;

        if (currentSpeed > velocityThreshold)
        {
            emissionModule.enabled = true;
        }
        else
        {
            emissionModule.enabled = false;
        }
    }
}