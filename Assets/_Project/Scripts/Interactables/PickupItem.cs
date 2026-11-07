using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupItem : MonoBehaviour
{
    [Header("Data Config")]
    [SerializeField] private ItemWeightData weightData;
    [SerializeField] private bool destroyOnAbsorb = true;

    [Header("Visual Feedback")]
    [SerializeField] private Vector3 floatRotation = new Vector3(0f, 30f, 0f);

    public ItemWeightData WeightData => weightData;

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

    public void OnDropped(Vector3 dropPosition)
    {
        transform.position = dropPosition;
        gameObject.SetActive(true);
    }
}