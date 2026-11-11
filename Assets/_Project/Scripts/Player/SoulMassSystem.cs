using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoulMassSystem : MonoBehaviour
{
    [Header("Mass Settings")]
    [SerializeField] private float baseMass = 1.0f;
    [SerializeField] private float maxTargetMass = 21.0f;

    [Header("Overload Visual Feedback")]
    [SerializeField] private ParticleSystem overloadParticles;

    public event Action<float, float> OnWeightChanged;
    public event Action OnEquilibriumReached; 
    public event Action OnOverloaded;

    private float currentMass;
    private PickupItem currentItem; 

    public float CurrentMass => currentMass;
    public float CurrentWeight => currentMass; 
    public float TargetMass => maxTargetMass;
    public bool IsEquilibrium => Mathf.Approximately(currentMass, maxTargetMass);

    private void Start()
    {
        currentMass = baseMass;
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.qKey.wasPressedThisFrame)
        {
            DropCurrentItem();
        }
    }

    public bool TryAbsorbItem(PickupItem item)
    {
        if (item == null || item.WeightData == null || !item.CanBeAbsorbed) return false;

        float itemWeight = item.WeightData.WeightInGrams; 
        float projectedMass = currentMass + itemWeight;

        if (projectedMass > maxTargetMass)
        {
            Debug.LogWarning($"[SoulMassSystem] OVERLOAD! Projected mass ({projectedMass}g) exceeds 21g cap!");
            TriggerOverloadPenalty();
            return false;
        }

        currentMass = projectedMass;
        currentItem = item;
        
        item.OnAbsorbed();
        
        AudioManager.Instance?.PlayAbsorbSound();
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);

        if (IsEquilibrium)
        {
            OnEquilibriumReached?.Invoke();
        }

        return true;
    }

    public void DropCurrentItem()
    {
        if (currentItem == null) return;

        currentMass -= currentItem.WeightData.WeightInGrams; 
        currentMass = Mathf.Max(baseMass, currentMass);

        // Direction forward from the player
        Vector3 dropDir = transform.forward;
        if (dropDir == Vector3.zero) dropDir = Vector3.forward;

        Vector3 dropPosition = transform.position + dropDir * 2.5f;
        dropPosition.y = transform.position.y; // Keep on floor plane

        currentItem.OnDropped(dropPosition, transform.position);
        currentItem = null;

        OnWeightChanged?.Invoke(currentMass, maxTargetMass);
    }

    public void ResetToBaseline()
    {
        currentItem = null;
        currentMass = baseMass;
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);
        Debug.Log("[SoulMassSystem] Weight and items reset to baseline for the next level.");
    }

    private void TriggerOverloadPenalty()
    {
        // 1. Play overload zap sound effect
        AudioManager.Instance?.PlayOverloadSound();

        // 2. Play overload particle burst if assigned
        if (overloadParticles != null)
        {
            overloadParticles.gameObject.SetActive(true);
            overloadParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            overloadParticles.Play(true);
        }

        OnOverloaded?.Invoke();

        // 3. Scatter current item out from center of ghost's world location
        if (currentItem != null)
        {
            Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
            randomDir.y = 0; // Keep horizontal direction clean
            if (randomDir == Vector3.zero) randomDir = transform.forward;
            randomDir.Normalize();

            // Destination target position on floor (3.5m to 5.0m away)
            Vector3 targetFloorPos = transform.position + randomDir * UnityEngine.Random.Range(3.5f, 5.0f);
            targetFloorPos.y = transform.position.y;

            // Pass (targetFloorPos, transform.position) so it flies directly out of the Ghost!
            currentItem.OnDropped(targetFloorPos, transform.position);
            currentItem = null;
        }

        // 4. Reset mass state back to base baseline (1.0g)
        currentMass = baseMass;
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);

        // 5. Delegate life reduction to persistent GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseLife();
        }
    }

    /// <summary>
    /// Transfers current mass to the companion and resets Nix back to 0g baseline.
    /// </summary>
    public float TransferMassAndReset()
    {
        float transferredWeight = currentMass;
        currentItem = null;
        currentMass = baseMass; // Resets back to baseMass (1.0g or 0.0g depending on your setup)
        
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);
        Debug.Log($"[SoulMassSystem] Transferred {transferredWeight}g to companion and reset Nix to baseline.");
        return transferredWeight;
    }
}