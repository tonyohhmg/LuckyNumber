using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class LevelPortalTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SoulMassSystem playerSoul; // Drag your player here
    [SerializeField] private GameObject portalVisuals;    // The actual portal particle effect / mesh

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

        if (playerSoul == null)
        {
            Debug.LogError("[Portal] Could not find SoulMassSystem in the scene! Please assign it in the Inspector.");
        }
    }

    private void Update()
    {
        if (!portalIsActive && playerSoul != null && playerSoul.IsEquilibrium)
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
        Debug.Log("[Portal] 21g Equilibrium reached! Portal smoothly materializing.");
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
                    Debug.Log("[Portal] SUCCESS! Final Level completed at 21g! Triggering Game Won Screen.");
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