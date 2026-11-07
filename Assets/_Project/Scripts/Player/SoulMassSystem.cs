using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoulMassSystem : MonoBehaviour
{
    [Header("Mass Settings")]
    [SerializeField] private float baseMass = 1.0f;
    [SerializeField] private float maxTargetMass = 21.0f; 

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
        if (item == null || item.WeightData == null) return false;

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
        
        // Trigger absorption sound effect
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

        Vector3 dropPosition = transform.position + transform.forward * 1.5f;
        currentItem.OnDropped(dropPosition);
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
        // Trigger overload penalty sound effect
        AudioManager.Instance?.PlayOverloadSound();

        OnOverloaded?.Invoke();

        if (currentItem != null)
        {
            Vector3 scatterPos = transform.position + (transform.forward + UnityEngine.Random.insideUnitSphere) * 2f;
            scatterPos.y = transform.position.y;
            currentItem.OnDropped(scatterPos);
            currentItem = null;
        }

        currentMass = baseMass;
        OnWeightChanged?.Invoke(currentMass, maxTargetMass);
    }
}