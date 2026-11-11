using System;
using System.Collections.Generic;
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
    private readonly List<PickupItem> inventory = new List<PickupItem>();

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

        // Pressing 'Q' drops ONLY the single most recently absorbed item
        if (kb.qKey.wasPressedThisFrame && inventory.Count > 0)
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
        inventory.Add(item);
        
        item.OnAbsorbed();
        
        AudioManager.Instance?.PlayAbsorbSound();
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);

        if (IsEquilibrium)
        {
            OnEquilibriumReached?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// Drops ONLY the single most recently absorbed item (LIFO) when Q is pressed.
    /// </summary>
    public void DropCurrentItem()
    {
        if (inventory.Count == 0)
        {
            Debug.Log("[SoulMassSystem] Drop requested, but inventory is empty.");
            return;
        }

        // Grab ONLY the very last item added to the inventory list
        int lastIndex = inventory.Count - 1;
        PickupItem itemToDrop = inventory[lastIndex];
        
        // Remove ONLY this single item from tracking
        inventory.RemoveAt(lastIndex);

        if (itemToDrop != null && itemToDrop.WeightData != null)
        {
            currentMass -= itemToDrop.WeightData.WeightInGrams; 
            currentMass = Mathf.Max(baseMass, currentMass);

            // Drop 3.0 meters forward
            Vector3 dropDir = transform.forward;
            if (dropDir == Vector3.zero) dropDir = Vector3.forward;

            Vector3 dropPosition = transform.position + dropDir * 3.0f;
            dropPosition.y = transform.position.y;

            itemToDrop.OnDropped(dropPosition, transform.position);
            
            Debug.Log($"[SoulMassSystem] SUCCESS: Dropped 1 item ({itemToDrop.name}). Items remaining in inventory: {inventory.Count}");
        }

        OnWeightChanged?.Invoke(currentMass, maxTargetMass);
    }

    public void DropLastItem() => DropCurrentItem();

    public void ResetToBaseline()
    {
        inventory.Clear();
        currentMass = baseMass;
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);
        Debug.Log("[SoulMassSystem] Weight and items reset to baseline.");
    }

    private void TriggerOverloadPenalty()
    {
        // 1. Play overload sound effect
        AudioManager.Instance?.PlayOverloadSound();

        // 2. Play overload particle burst if assigned
        if (overloadParticles != null)
        {
            overloadParticles.gameObject.SetActive(true);
            overloadParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            overloadParticles.Play(true);
        }

        OnOverloaded?.Invoke();

        Debug.Log($"[SoulMassSystem] OVERLOAD PENALTY: Scattering ALL {inventory.Count} items.");

        // 3. Scatter ALL items currently in inventory out from the ghost's location on overload
        foreach (var item in inventory)
        {
            if (item == null) continue;

            Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
            randomDir.y = 0; 
            if (randomDir == Vector3.zero) randomDir = transform.forward;
            randomDir.Normalize();

            Vector3 targetFloorPos = transform.position + randomDir * UnityEngine.Random.Range(3.5f, 5.0f);
            targetFloorPos.y = transform.position.y;

            item.OnDropped(targetFloorPos, transform.position);
        }

        // Clear the inventory list completely
        inventory.Clear();

        // 4. Reset mass back to base baseline
        currentMass = baseMass;
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);

        // 5. Deduct a life via the persistent GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseLife();
        }
    }

    /// <summary>
    /// Transfers current mass to the companion and resets Nix back to baseline.
    /// </summary>
    public float TransferMassAndReset()
    {
        float transferredWeight = currentMass;
        inventory.Clear();
        currentMass = baseMass; 
        
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);
        Debug.Log($"[SoulMassSystem] Transferred {transferredWeight}g to companion and reset Nix to baseline.");
        return transferredWeight;
    }
}