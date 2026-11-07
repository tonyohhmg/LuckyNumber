using UnityEngine;
using System.Collections;
using System;

public class LevelPortalTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SoulMassSystem playerSoul; // Drag your player here
    [SerializeField] private GameObject portalVisuals;    // The actual portal particle effect / mesh (child object)

    [Header("Target Barrier to Remove")]
    [SerializeField] private SimpleBarrier targetBarrier;   // Reference to your SimpleBarrier script
    [SerializeField] private GameObject glowingBarrier;     // Alternative: direct GameObject to SetActive(false)

    [Header("Settings")]
    [SerializeField] private bool requireProximity = false;
    [SerializeField] private float maxActivationDistance = 5.0f;
    [SerializeField] private float portalAnimationDuration = 0.5f; // Speed of scale in/out effect
    [SerializeField] private float delayBeforePortalFade = 2.0f;    // Time to wait after barrier disappears before portal fades out

    private bool portalIsActive = false;
    private bool portalPassed = false;
    private Vector3 originalPortalScale = Vector3.one;

    private void Start()
    {
        if (portalVisuals != null)
        {
            // Cache original scale and start hidden via zero scale
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

        if (playerSoul != null)
        {
            playerSoul.OnEquilibriumReached += HandleEquilibriumReached;
            Debug.Log("[Portal] Subscribed to playerSoul.OnEquilibriumReached successfully.");
        }
        else
        {
            Debug.LogError("[Portal] Could not find SoulMassSystem in the scene! Please assign it in the Inspector.");
        }
    }

    private void OnDestroy()
    {
        if (playerSoul != null)
        {
            playerSoul.OnEquilibriumReached -= HandleEquilibriumReached;
        }
    }

    private void Update()
    {
        // Polling check every frame to activate the portal instantly when 21g equilibrium is reached
        if (!portalIsActive && playerSoul != null && playerSoul.IsEquilibrium)
        {
            CheckAndActivatePortal();
        }
    }

    private void HandleEquilibriumReached()
    {
        CheckAndActivatePortal();
    }

    private void CheckAndActivatePortal()
    {
        if (portalIsActive) return;

        if (requireProximity)
        {
            float distance = Vector3.Distance(transform.position, playerSoul.transform.position);
            if (distance > maxActivationDistance)
            {
                // Too far away yet, wait until player comes closer
                return;
            }
        }

        ActivatePortalVisuals();
    }

    private void ActivatePortalVisuals()
    {
        portalIsActive = true;

        if (portalVisuals != null)
        {
            // Smoothly scale up from zero to original size (Fade In effect)
            StartCoroutine(ScalePortalRoutine(Vector3.zero, originalPortalScale, true, null));
        }

        Debug.Log("[Portal] 21g Equilibrium reached! Portal smoothly materializing. Fly through it to remove the barrier.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!portalIsActive || portalPassed) return;

        if (other.TryGetComponent<SoulMassSystem>(out var soul))
        {
            if (soul.IsEquilibrium)
            {
                portalPassed = true;

                // Play portal trigger sound effect via AudioManager singleton
                AudioManager.Instance?.PlayPortalTriggerSound();

                // 1. Reset player's mass and inventory back to baseline for the next level
                soul.ResetToBaseline();

                // 2. Remove the barrier (SimpleBarrier will shrink and fade out)
                if (targetBarrier != null)
                {
                    targetBarrier.DissolveBarrier();
                }
                else if (glowingBarrier != null)
                {
                    glowingBarrier.SetActive(false);
                }

                Debug.Log("=========================================");
                Debug.Log("[Portal] SUCCESS! Ghost flew through portal at 21g. Barrier dissolving & weight reset!");
                Debug.Log("=========================================");

                // 3. Wait 2 seconds after barrier removal, then smoothly shrink/fade out the portal
                StartCoroutine(WaitAndFadeOutPortalRoutine());
            }
            else
            {
                Debug.Log("[Portal] Portal is active, but your mass is no longer at 21g!");
            }
        }
    }

    private IEnumerator WaitAndFadeOutPortalRoutine()
    {
        // Wait for the specified delay after the barrier is removed
        yield return new WaitForSeconds(delayBeforePortalFade);

        // Smoothly scale down to zero (Fade Out effect) then disable object
        yield return StartCoroutine(ScalePortalRoutine(originalPortalScale, Vector3.zero, false, () => {
            if (portalVisuals != null)
            {
                portalVisuals.SetActive(false);
            }
            Debug.Log("[Portal] Portal successfully faded away and deactivated.");
        }));
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