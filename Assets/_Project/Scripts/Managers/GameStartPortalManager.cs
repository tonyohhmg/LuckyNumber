using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class GameStartPortalManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your StartPortal GameObject (or its visual particle child) here.")]
    [SerializeField] private GameObject startPortalVisuals;

    [Tooltip("Drag your Ghost Player GameObject from the scene here.")]
    [SerializeField] private GameObject ghostPlayer;

    [Header("Spawn Settings")]
    [SerializeField] private float portalScaleDuration = 0.8f;  // Time for portal to scale from 0 to 1
    [SerializeField] private float ghostRiseDuration = 0.6f;    // Time for ghost to emerge
    [SerializeField] private float riseHeight = 1.5f;           // How high up the ghost rises from the ground

    private bool sequenceCompleted = false;
    private bool portalDisappearing = false;

    private void Start()
    {
        // Ensure the portal starts hidden and scaled to zero at game load
        if (startPortalVisuals != null)
        {
            startPortalVisuals.transform.localScale = Vector3.zero;
            startPortalVisuals.SetActive(false);
        }

        // Ensure the ghost player starts hidden so they can emerge from the portal
        if (ghostPlayer != null)
        {
            ghostPlayer.SetActive(false);
        }
    }

    private void Update()
    {
        // Once the spawn sequence is fully finished, watch for any movement key press (W, A, S, D, or Arrow keys)
        if (sequenceCompleted && !portalDisappearing && ghostPlayer != null && ghostPlayer.activeSelf)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                bool pressedW = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                bool pressedS = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
                bool pressedA = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
                bool pressedD = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;

                if (pressedW || pressedS || pressedA || pressedD)
                {
                    StartCoroutine(ReversePortalSequenceRoutine());
                }
            }
        }
    }

    /// <summary>
    /// Called automatically by GameManager when Play is clicked.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        sequenceCompleted = false;
        portalDisappearing = false;
        StartCoroutine(GameStartSequenceRoutine());
    }

    private IEnumerator GameStartSequenceRoutine()
    {
        // Play sound effect when the portal and ghost spawn sequence starts
        AudioManager.Instance?.PlayPortalSpawnSound();

        // 1. Activate and scale the StartPortal outward from 0 to 1
        if (startPortalVisuals != null)
        {
            startPortalVisuals.SetActive(true);
            yield return StartCoroutine(ScalePortalRoutine(Vector3.zero, Vector3.one));
        }

        // 2. Make the ghost appear and rise up from the ground/portal position
        if (ghostPlayer != null)
        {
            Vector3 targetPos = ghostPlayer.transform.position;
            Vector3 startPos = targetPos - new Vector3(0f, riseHeight, 0f);

            ghostPlayer.transform.position = startPos;
            ghostPlayer.SetActive(true);

            float elapsed = 0f;
            while (elapsed < ghostRiseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / ghostRiseDuration;
                
                // Smoothly rise up into position out of the ground
                ghostPlayer.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            ghostPlayer.transform.position = targetPos;
        }

        // Mark sequence as complete so we can start listening for player movement input
        sequenceCompleted = true;
    }

    private IEnumerator ReversePortalSequenceRoutine()
    {
        portalDisappearing = true;

        // Scale the StartPortal inward from 1 back down to 0
        if (startPortalVisuals != null)
        {
            yield return StartCoroutine(ScalePortalRoutine(Vector3.one, Vector3.zero));
            startPortalVisuals.SetActive(false);
        }

        Debug.Log("[GameStartPortalManager] Player moved; portal disappeared.");
    }

    private IEnumerator ScalePortalRoutine(Vector3 startScale, Vector3 targetScale)
    {
        float elapsed = 0f;

        while (elapsed < portalScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / portalScaleDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (startPortalVisuals != null)
            {
                startPortalVisuals.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            }

            yield return null;
        }

        if (startPortalVisuals != null)
        {
            startPortalVisuals.transform.localScale = targetScale;
        }
    }
}