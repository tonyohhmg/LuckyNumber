using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class WeightTriggerPlatform : MonoBehaviour
{
    [Header("Target Weight")]
    [SerializeField] private float requiredWeight = 21.0f;
    [SerializeField] private float tolerance = 0.05f;

    [Header("Events")]
    [SerializeField] private UnityEvent onEquilibriumActivated;
    [SerializeField] private UnityEvent onEquilibriumDeactivated;

    private bool isActivated = false;

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<SoulMassSystem>(out var soul))
        {
            bool matchesEquilibrium = Mathf.Abs(soul.CurrentWeight - requiredWeight) <= tolerance;

            if (matchesEquilibrium && !isActivated)
            {
                isActivated = true;
                onEquilibriumActivated?.Invoke();
                
                // Debug log to confirm equilibrium activation
                Debug.Log($"[WeightTriggerPlatform] EQUILIBRIUM ACHIEVED! Platform activated at {soul.CurrentWeight}g (Target: {requiredWeight}g).");
            }
            else if (!matchesEquilibrium && isActivated)
            {
                isActivated = false;
                onEquilibriumDeactivated?.Invoke();
                
                Debug.Log($"[WeightTriggerPlatform] Equilibrium lost. Current weight: {soul.CurrentWeight}g.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<SoulMassSystem>() != null && isActivated)
        {
            isActivated = false;
            onEquilibriumDeactivated?.Invoke();
            
            Debug.Log("[WeightTriggerPlatform] Player left platform. Deactivated.");
        }
    }
}