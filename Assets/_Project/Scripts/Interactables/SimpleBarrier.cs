using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(BoxCollider))]
public class SimpleBarrier : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private BoxCollider barrierCollider;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        barrierCollider = GetComponent<BoxCollider>();
    }

    public void DissolveBarrier()
    {
        if (barrierCollider != null)
        {
            barrierCollider.enabled = false;
        }

        StartCoroutine(ShrinkAndFadeOutRoutine());
    }

    private IEnumerator ShrinkAndFadeOutRoutine()
    {
        Material mat = meshRenderer.material;
        Color startColor = mat.color;
        Vector3 startScale = transform.localScale;
        
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Smoothly fade alpha
            float alpha = Mathf.Lerp(startColor.a, 0f, t);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            // Smoothly lerp scale down to zero
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}