using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(SoulMassSystem))]
public class SpectralController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SoulMassSystem soulMassSystem;
    [SerializeField] private Transform ghostMeshTransform; // Drag your 3D Ghost Character mesh object here!
    [SerializeField] private Transform cameraTransformOverride; // Optional: Drag your camera here if Camera.main fails

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float floatHeightSpeed = 3f;

    [Header("Mass Scaling Visuals")]
    [SerializeField] private Vector3 minScale = Vector3.one;                   
    [SerializeField] private Vector3 maxScale = new Vector3(1.4f, 1.4f, 1.4f); 
    [SerializeField] private float scaleLerpSpeed = 5f;

    [Header("Overload Feedback")]
    [SerializeField] private ParticleSystem overloadParticles; 

    private CharacterController controller;
    private Vector3 targetScale;
    private Coroutine particleRoutine;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (soulMassSystem == null)
        {
            soulMassSystem = GetComponent<SoulMassSystem>();
        }
        targetScale = minScale;

        if (overloadParticles != null)
        {
            overloadParticles.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (soulMassSystem != null)
        {
            soulMassSystem.OnOverloaded += HandleOverloaded;
        }
    }

    private void OnDisable()
    {
        if (soulMassSystem != null)
        {
            soulMassSystem.OnOverloaded -= HandleOverloaded;
        }
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float moveX = 0f;
        float moveZ = 0f;
        float moveY = 0f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX = -1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX = 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveZ = -1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveZ = 1f;

        if (kb.spaceKey.isPressed) moveY = 1f;
        if (kb.leftShiftKey.isPressed) moveY = -1f;

        Vector3 moveDir = new Vector3(moveX, 0f, moveZ).normalized;

        // Safely resolve active camera reference without crashing
        Transform activeCam = cameraTransformOverride;
        if (activeCam == null && Camera.main != null)
        {
            activeCam = Camera.main.transform;
        }

        if (activeCam != null)
        {
            Vector3 camForward = activeCam.forward;
            Vector3 camRight = activeCam.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDir = (camForward * moveDir.z + camRight * moveDir.x).normalized;
        }

        Vector3 targetVelocity = moveDir * moveSpeed;
        targetVelocity.y = moveY * floatHeightSpeed;

        controller.Move(targetVelocity * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.001f && ghostMeshTransform != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            ghostMeshTransform.rotation = Quaternion.Slerp(ghostMeshTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (soulMassSystem != null)
        {
            float weightRatio = Mathf.InverseLerp(1.0f, soulMassSystem.TargetMass, soulMassSystem.CurrentWeight);
            targetScale = Vector3.Lerp(minScale, maxScale, weightRatio);
        }

        if (ghostMeshTransform != null)
        {
            ghostMeshTransform.localScale = Vector3.Lerp(ghostMeshTransform.localScale, targetScale, scaleLerpSpeed * Time.deltaTime);
        }
    }

    private void HandleOverloaded()
    {
        if (overloadParticles != null)
        {
            if (particleRoutine != null)
            {
                StopCoroutine(particleRoutine);
            }
            particleRoutine = StartCoroutine(PlayOneShotParticlesRoutine());
        }
    }

    private IEnumerator PlayOneShotParticlesRoutine()
    {
        overloadParticles.gameObject.SetActive(true);
        overloadParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        overloadParticles.Play(true);

        float duration = overloadParticles.main.duration;
        yield return new WaitForSeconds(duration);

        overloadParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        overloadParticles.gameObject.SetActive(false);
        particleRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PickupItem>(out var item))
        {
            soulMassSystem.TryAbsorbItem(item);
        }
    }
}