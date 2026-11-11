using System;
using UnityEngine;

public class CompanionSoulSystem : MonoBehaviour
{
    [Header("Companion Mass Settings")]
    [SerializeField] private float baseMass = 0.0f;
    [SerializeField] private float targetMass = 21.0f;

    [Header("Level 3 Configuration")]
    [Tooltip("Check this box ONLY on the Companion GameObject in the Level 3 Scene so they start unbound and following Nix!")]
    [SerializeField] private bool startFreedInLevel3 = false;

    [Header("Containment Shield Visuals")]
    [SerializeField] private GameObject containmentShieldObject; // Drag your sphere orb/trap GameObject here!

    private float currentMass;
    private PickupItem heldItem;
    private SoulMassSystem playerSoul;
    private bool isFreed = false;

    public event Action OnCompanionEquilibriumReached;
    public event Action<float, float> OnCompanionWeightChanged;

    public float CurrentMass => currentMass;
    public bool IsEquilibrium => Mathf.Approximately(currentMass, targetMass);
    public bool IsFreed => isFreed;

    private void Start()
    {
        currentMass = baseMass;
        playerSoul = FindAnyObjectByType<SoulMassSystem>();

        if (startFreedInLevel3)
        {
            // === LEVEL 3 SETUP ===
            isFreed = true;

            if (containmentShieldObject != null)
            {
                containmentShieldObject.SetActive(false);
            }

            // Ensure companion audio is off
            AudioManager.Instance?.StopCompanionWhisperLoop();

            if (TryGetComponent<CompanionFollower>(out var follower))
            {
                follower.EnableFollow();
            }

            Debug.Log("[Companion] Level 3 Initialized: Companion starting freed and following Nix!");
        }
        else
        {
            // === LEVEL 2 SETUP: Starts trapped inside the containment shield ===
            if (containmentShieldObject != null)
            {
                containmentShieldObject.SetActive(true);
            }

            // Start the whispering loop via AudioManager's dedicated companion audio source
            AudioManager.Instance?.PlayCompanionWhisperLoop();
        }

        OnCompanionWeightChanged?.Invoke(currentMass, targetMass);
    }

    /// <summary>
    /// Receives transferred mass from Nix (used for freeing in Level 2 or mass filling in Level 3).
    /// </summary>
    public void ReceiveTransferredMass(float transferredWeight)
    {
        // If already freed (Level 3), just add the mass directly up to targetMass without re-triggering trap code
        if (isFreed && startFreedInLevel3)
        {
            currentMass = Mathf.Clamp(currentMass + transferredWeight, 0f, targetMass);
            OnCompanionWeightChanged?.Invoke(currentMass, targetMass);

            AudioManager.Instance?.PlayAbsorbSound();
            Debug.Log($"[Companion] Level 3 Received {transferredWeight}g from Nix! Current Companion Mass: {currentMass}g");

            if (IsEquilibrium)
            {
                OnCompanionEquilibriumReached?.Invoke();
                Debug.Log("[Companion] Equilibrium reached! Companion is ready.");
            }
            return;
        }

        // Level 2 Trap-Freeing Logic
        if (isFreed) return;

        isFreed = true;
        currentMass = Mathf.Clamp(transferredWeight, 0f, targetMass);

        // 1. Disable the orb visual trap
        if (containmentShieldObject != null)
        {
            containmentShieldObject.SetActive(false); 
        }

        // 2. Stop the companion whisper audio loop immediately
        AudioManager.Instance?.StopCompanionWhisperLoop();

        // 3. Play centralized orb shatter sound effect
        AudioManager.Instance?.PlayOrbShatterSound();

        if (TryGetComponent<CompanionFollower>(out var follower))
        {
            follower.EnableFollow();
        }

        OnCompanionWeightChanged?.Invoke(currentMass, targetMass);

        Debug.Log($"[Companion] Received {currentMass}g from Nix! Companion freed.");

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

        OnCompanionWeightChanged?.Invoke(currentMass, targetMass);

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
        
        if (!startFreedInLevel3)
        {
            isFreed = false;
            if (containmentShieldObject != null)
            {
                containmentShieldObject.SetActive(true);
            }

            AudioManager.Instance?.PlayCompanionWhisperLoop();
        }

        OnCompanionWeightChanged?.Invoke(currentMass, targetMass);
    }
}