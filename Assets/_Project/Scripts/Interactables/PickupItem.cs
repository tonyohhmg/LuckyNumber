using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupItem : MonoBehaviour
{
    [Header("Data Config")]
    [SerializeField] private ItemWeightData weightData;
    [SerializeField] private bool destroyOnAbsorb = true;

    [Header("Visual Feedback")]
    [SerializeField] private Vector3 floatRotation = new Vector3(0f, 30f, 0f);

    private Coroutine flyOutCoroutine;
    private bool canBeAbsorbed = true;

    public ItemWeightData WeightData => weightData;
    public bool CanBeAbsorbed => canBeAbsorbed;

    private void Update()
    {
        // Gentle rotation idle animation for interactables
        transform.Rotate(floatRotation * Time.deltaTime);
    }

    public void OnAbsorbed()
    {
        if (destroyOnAbsorb)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Launches the item from startPos (Ghost position) to targetPos with a height arc.
    /// Temporarily disables absorption so the ghost cannot re-absorb it during drop flight.
    /// </summary>
    public void OnDropped(Vector3 targetPos, Vector3 startPos)
    {
        canBeAbsorbed = false; // Prevent instant re-absorption!
        transform.position = startPos;
        gameObject.SetActive(true);

        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = true;
        }

        if (flyOutCoroutine != null)
        {
            StopCoroutine(flyOutCoroutine);
        }
        flyOutCoroutine = StartCoroutine(FlyToPosition(startPos, targetPos, 0.4f));
    }

    public void OnDropped(Vector3 targetPos)
    {
        OnDropped(targetPos, transform.position);
    }

    private IEnumerator FlyToPosition(Vector3 startPos, Vector3 targetPos, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Lerp from startPos to targetPos
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // Parabolic Arc
            float arcHeight = Mathf.Sin(t * Mathf.PI) * 1.5f;
            currentPos.y += arcHeight;

            transform.position = currentPos;
            transform.Rotate(Vector3.up, 360f * Time.deltaTime * 2f);

            yield return null;
        }

        transform.position = targetPos;
        flyOutCoroutine = null;

        // Brief safety buffer after landing before allowing pickup again
        yield return new WaitForSeconds(0.2f);
        canBeAbsorbed = true;
    }
}