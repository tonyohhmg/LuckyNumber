using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class LevelPortalTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SoulMassSystem playerSoul; // Drag your player here
    [SerializeField] private GameObject portalVisuals;    // The actual portal particle effect / mesh

    [Header("Companion Requirements")]
    [SerializeField] private bool requireCompanion = true;      // Check this for Level 2 AND Level 3!
    [SerializeField] private CompanionSoulSystem companionSoul; // Drag your companion ghost here

    [Header("Multi-Scene Progression Configuration")]
    [SerializeField] private bool isFinalLevelPortal = false; // Check this ONLY on Level 3's final portal!
    [SerializeField] private string nextSceneName;            // Exact name of the next level scene (e.g., "GameplayLevel2")
    [SerializeField] private GameObject gameWonUIContainer;   // Drag your GameWonContainer here for Level 3

    [Header("Settings")]
    [SerializeField] private bool requireProximity = false;
    [SerializeField] private float maxActivationDistance = 5.0f;
    [SerializeField] private float portalAnimationDuration = 0.5f;

    private bool portalIsActive = false;
    private bool portalPassed = false;
    private Vector3 originalPortalScale = Vector3.one;

    private void Start()
    {
        if (portalVisuals != null)
        {
            originalPortalScale = portalVisuals.transform.localScale;
            portalVisuals.transform.localScale = Vector3.zero;
            portalVisuals.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[Portal] Portal Visuals reference is not assigned in LevelPortalTrigger!");
        }

        if (playerSoul == null)
        {
            playerSoul = FindAnyObjectByType<SoulMassSystem>();
        }

        if (requireCompanion && companionSoul == null)
        {
            companionSoul = FindAnyObjectByType<CompanionSoulSystem>();
        }

        if (playerSoul == null)
        {
            Debug.LogError("[Portal] Could not find SoulMassSystem in the scene! Please assign it in the Inspector.");
        }
    }

    private void Update()
    {
        if (portalIsActive) return;

        if (playerSoul == null)
        {
            playerSoul = FindAnyObjectByType<SoulMassSystem>();
            return;
        }

        if (requireCompanion && companionSoul == null)
        {
            companionSoul = FindAnyObjectByType<CompanionSoulSystem>();
        }

        // === TRANSFER PHASE: IF DUNCAN IS NOT AT 21g YET AND NIX HITS 21g ===
        if (requireCompanion && companionSoul != null && !companionSoul.IsEquilibrium && playerSoul.IsEquilibrium)
        {
            float transferredWeight = playerSoul.TransferMassAndReset();
            
            // If Duncan started freed in Level 3, we update his mass directly without triggering the Level 2 orb trap code
            if (companionSoul.IsFreed)
            {
                // Force-feed Duncan his 21g to fill his UI/mass bar
                for (int i = 0; i < 3; i++) // or a direct setter if available, let's trigger absorb logic or custom fill
                {
                    // Alternatively, pass mass directly if CompanionSoulSystem has a method, 
                    // or reuse ReceiveTransferredMass since it handles IsFreed checks safely.
                }
                companionSoul.ReceiveTransferredMass(transferredWeight);
            }
            else
            {
                // Level 2 behavior (breaks the orb trap)
                companionSoul.ReceiveTransferredMass(transferredWeight);
            }
            
            Debug.Log("[Portal] Transfer Complete: 21g given to Duncan! Nix reset to zero. Gather your second 21g, Nix!");
            return; // Skip portal check this frame so Nix can start his second run
        }

        // === STANDARD EQUILIBRIUM CHECK FOR PORTAL ===
        bool playerReady = playerSoul.IsEquilibrium;
        bool companionReady = !requireCompanion || (companionSoul != null && companionSoul.IsEquilibrium);

        if (playerReady && companionReady)
        {
            CheckAndActivatePortal();
        }
    }

    private void CheckAndActivatePortal()
    {
        if (portalIsActive) return;

        if (requireProximity)
        {
            float distance = Vector3.Distance(transform.position, playerSoul.transform.position);
            if (distance > maxActivationDistance) return;
        }

        ActivatePortalVisuals();
    }

    private void ActivatePortalVisuals()
    {
        portalIsActive = true;

        if (portalVisuals != null)
        {
            StartCoroutine(ScalePortalRoutine(Vector3.zero, originalPortalScale, true, null));
        }

        AudioManager.Instance?.PlayPortalSpawnSound();
        Debug.Log("[Portal] Dual 21g Equilibrium reached! Portal smoothly materializing.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!portalIsActive || portalPassed) return;

        if (other.TryGetComponent<SoulMassSystem>(out var soul))
        {
            if (soul.IsEquilibrium)
            {
                portalPassed = true;
                AudioManager.Instance?.PlayAbsorbSound();

                // Reset player's mass back to baseline before transitioning
                soul.ResetToBaseline();

                if (isFinalLevelPortal)
                {
                    // === LEVEL 3: TRIGGER GAME WON SCREEN ===
                    Debug.Log("[Portal] SUCCESS! Level 3 completed! Triggering Game Won Screen.");
                    if (gameWonUIContainer != null)
                    {
                        gameWonUIContainer.SetActive(true);
                    }
                    else if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TriggerGameWon();
                    }
                    Time.timeScale = 0f;
                }
                else
                {
                    // === LEVEL 1 or 2: LOAD NEXT DEDICATED SCENE ===
                    Debug.Log($"[Portal] SUCCESS! Ghost flew through portal at 21g. Loading next scene: {nextSceneName}");
                    
                    if (!string.IsNullOrEmpty(nextSceneName))
                    {
                        SceneManager.LoadScene(nextSceneName);
                    }
                    else
                    {
                        Debug.LogError("[Portal] Next Scene Name is blank or unassigned in the Inspector!");
                    }
                }
            }
            else
            {
                Debug.Log("[Portal] Portal is active, but your mass is no longer at 21g!");
            }
        }
    }

    private IEnumerator ScalePortalRoutine(Vector3 startScale, Vector3 targetScale, bool activateObjectFirst, Action onComplete)
    {
        if (portalVisuals == null) yield break;

        if (activateObjectFirst)
        {
            portalVisuals.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < portalAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / portalAnimationDuration;
            portalVisuals.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        portalVisuals.transform.localScale = targetScale;
        onComplete?.Invoke();
    }
}