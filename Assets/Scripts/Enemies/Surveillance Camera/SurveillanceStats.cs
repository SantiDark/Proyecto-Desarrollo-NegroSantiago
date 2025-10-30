using UnityEngine;

[CreateAssetMenu(menuName = "Stealth/Surveillance Stats", fileName = "SO_SurveillanceStats")]
public class SurveillanceStats : ScriptableObject
{
    [Header("Vida")]
    public int maxHealth = 100;

    [Header("Detección (vectores)")]
    public bool useVisionCone = true;
    [Range(0, 180)] public float visionAngle = 60f; 
    public float visionDistance = 5f;               
    public LayerMask visionObstacles;               

    [Header("Comportamiento")]
    public float rotateSpeedDegPerSec = 30f; // barrido
}
