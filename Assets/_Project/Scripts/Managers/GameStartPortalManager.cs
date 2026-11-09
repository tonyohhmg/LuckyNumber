using UnityEngine;
using UnityEngine.InputSystem; // Using the new Input System package
using System.Collections;

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

    private bool hasStartedMoving = false;
    private bool sequenceFinished = false;

    private void Start()
    {
        // 1. Ensure the portal starts hidden and scaled to zero at game load
        if (startPortalVisuals != null)
        {
            startPortalVisuals.transform.localScale = Vector3.zero;
            startPortalVisuals.SetActive(false);
        }

        // 2. Ensure the ghost player starts hidden so they can emerge from the portal
        if (ghostPlayer != null)
        {
            ghostPlayer.SetActive(false);
        }

        // 3. Automatically trigger the spawn sequence as soon as Level 1 loads!
        TriggerStartSequence();
    }

    private void Update()
    {
        // Check if the spawn sequence is done and player hits any movement key to shrink the portal away
        if (sequenceFinished && !hasStartedMoving)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || 
                    kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
                    kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                    kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
                    kb.spaceKey.wasPressedThisFrame)
                {
                    hasStartedMoving = true;
                    StartCoroutine(FadeOutPortalRoutine());
                }
            }
        }
    }

    public void TriggerStartSequence()
    {
        StopAllCoroutines();
        StartCoroutine(GameStartSequenceRoutine());
    }

    private IEnumerator GameStartSequenceRoutine()
    {
        // Play the portal spawn sound via AudioManager
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
            ghostPlayer.SetActive(true); // Ghost becomes active and visible here!

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

        sequenceFinished = true;
    }

    private IEnumerator ScalePortalRoutine(Vector3 fromScale, Vector3 toScale)
    {
        float elapsed = 0f;
        while (elapsed < portalScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / portalScaleDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (startPortalVisuals != null)
            {
                startPortalVisuals.transform.localScale = Vector3.Lerp(fromScale, toScale, smoothT);
            }
            yield return null;
        }

        if (startPortalVisuals != null)
        {
            startPortalVisuals.transform.localScale = toScale;
        }
    }

    private IEnumerator FadeOutPortalRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(ScalePortalRoutine(Vector3.one, Vector3.zero));
        
        if (startPortalVisuals != null)
        {
            startPortalVisuals.SetActive(false);
        }
    }
}