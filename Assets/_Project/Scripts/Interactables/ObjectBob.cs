using UnityEngine;

public class ObjectBob : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobHeight = 0.25f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        // Smooth sine wave up and down
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}