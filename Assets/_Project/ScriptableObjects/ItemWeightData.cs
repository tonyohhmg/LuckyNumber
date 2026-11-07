using UnityEngine;

[CreateAssetMenu(fileName = "NewItemWeightData", menuName = "Soul Game/Item Weight Data")]
public class ItemWeightData : ScriptableObject
{
    [SerializeField] private string itemName = "Spectral Artifact";
    [SerializeField] private float weightInGrams = 3.0f;
    [SerializeField] private Color particleColorTint = Color.cyan;

    public string ItemName => itemName;
    public float WeightInGrams => weightInGrams;
    public Color ParticleColorTint => particleColorTint;
}