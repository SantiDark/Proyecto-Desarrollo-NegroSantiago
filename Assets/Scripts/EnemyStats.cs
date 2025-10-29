using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Stats", fileName = "EnemyStats_Default")]
public class EnemyStats : ScriptableObject
{
    [Header("Atributos base")]
    public int maxHealth = 100;
    public float moveSpeed = 3.5f;
    public float stopDistance = 1.2f;
    public float chaseDistance = 6f;

    [Header("Visión y percepción")]
    public bool useVisionCone = true;
    [Range(1f, 179f)] public float visionAngle = 60f;
    public float visionDistance = 12f;
    public LayerMask visionObstacles; // ✅ campo que faltaba

    [Header("Daño")]
    public float touchDamage = 10f;

    [Header("VFX opcional al morir")]
    public GameObject deathVfxPrefab; // ✅ campo que faltaba

    [Header("Presión sobre el jugador")]
    [Min(0f)] public float staminaDrainPerSecond = 6f; // ✅ configurable desde el SO
}
