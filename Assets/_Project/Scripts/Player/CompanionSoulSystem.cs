using System;
using UnityEngine;

public class CompanionSoulSystem : MonoBehaviour
{
    [Header("Companion Mass Settings")]
    [SerializeField] private float baseMass = 0.0f;
    [SerializeField] private float targetMass = 21.0f;

    [Header("Containment Shield Visuals")]
    [SerializeField] private GameObject containmentShieldObject; // Drag your sphere orb/trap GameObject here!

    private float currentMass;
    private PickupItem heldItem;
    private SoulMassSystem playerSoul;
    private bool isFreed = false;

    public event Action OnCompanionEquilibriumReached;

    public float CurrentMass => currentMass;
    public bool IsEquilibrium => Mathf.Approximately(currentMass, targetMass);

    private void Start()
    {
        currentMass = baseMass;
        playerSoul = FindAnyObjectByType<SoulMassSystem>();

        // Ensure shield starts active, trapping the companion
        if (containmentShieldObject != null)
        {
            containmentShieldObject.SetActive(true);
        }
    }

    /// <summary>
    /// Receives the initial 21g transferred directly from Nix to break the orb trap.
    /// </summary>
    public void ReceiveTransferredMass(float transferredWeight)
    {
        if (isFreed) return;

        isFreed = true;
        currentMass = Mathf.Clamp(transferredWeight, 0f, targetMass);

        if (containmentShieldObject != null)
        {
            containmentShieldObject.SetActive(false); // Shatters the orb trap!
        }

        // Enable the follower component so it trails Nix around the level!
        if (TryGetComponent<CompanionFollower>(out var follower))
        {
            follower.EnableFollow();
        }

        Debug.Log($"[Companion] Received {currentMass}g from Nix! Companion equilibrium reached.");

        if (IsEquilibrium)
        {
            OnCompanionEquilibriumReached?.Invoke();
        }
    }


    public bool TryAbsorbItem(PickupItem item)
    {
        if (!isFreed)
        {
            Debug.Log("[Companion] Still trapped behind the shield! Nix must reach 21g first.");
            return false;
        }

        if (item == null || item.WeightData == null || !item.CanBeAbsorbed) return false;

        float itemWeight = item.WeightData.WeightInGrams;
        float projectedMass = currentMass + itemWeight;

        if (projectedMass > targetMass)
        {
            Debug.Log("[Companion] Mass exceeds 21g! Cannot absorb.");
            return false;
        }

        currentMass = projectedMass;
        heldItem = item;
        item.OnAbsorbed();

        AudioManager.Instance?.PlayAbsorbSound();
        Debug.Log($"[Companion] Absorbed item. Current Companion Mass: {currentMass}g");

        if (IsEquilibrium)
        {
            OnCompanionEquilibriumReached?.Invoke();
            Debug.Log("[Companion] Equilibrium reached! Companion is ready.");
        }

        return true;
    }

    public void ResetCompanion()
    {
        currentMass = baseMass;
        heldItem = null;
        isFreed = false;

        if (containmentShieldObject != null)
        {
            containmentShieldObject.SetActive(true);
        }
    }
}