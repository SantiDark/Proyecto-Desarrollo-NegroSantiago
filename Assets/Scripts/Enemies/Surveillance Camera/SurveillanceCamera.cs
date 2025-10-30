using UnityEngine;

[RequireComponent(typeof(Health))]
public class SurveillanceCamera : MonoBehaviour
{
    public Transform player;
    public SurveillanceStats stats;
    public Transform head;               // opcional: el pivot que rota (si no, usa transform)
    public bool drawGizmos = true;

    float eyeHeight = 1.8f;
    Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        if (!head) head = transform;
    }

    void Update()
    {
        // barrido opcional
        head.Rotate(0f, stats.rotateSpeedDegPerSec * Time.deltaTime, 0f, Space.World);

        DetectPlayer(); // vectores + oclusión
    }

    void DetectPlayer()
    {
        if (!player || !stats) return;

        Vector3 eye = head.position + Vector3.up * eyeHeight;
        Vector3 playerEye = player.position + Vector3.up * eyeHeight;
        Vector3 toTarget = playerEye - eye;

        // 1) Distancia
        if (toTarget.magnitude > stats.visionDistance) return;

        // 2) Ángulo
        if (stats.useVisionCone)
        {
            float angle = Vector3.Angle(head.forward, toTarget);
            if (angle > stats.visionAngle * 0.5f) return;
        }

        // 3) Oclusión
        if (Physics.Raycast(eye, toTarget.normalized, out RaycastHit hit, stats.visionDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != player)
            {
                int mask = 1 << hit.transform.gameObject.layer;
                if ((stats.visionObstacles.value & mask) != 0) return; // bloqueado
            }
        }

        // Detectado
        Debug.Log("[Camera] Player detected!");
        // (Para el 7–10, acá vamos a poner la alerta global a todos los soldiers)
    }

    // --- Gizmos ---
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || stats == null) return;
        Transform h = head ? head : transform;

        Vector3 eye = h.position + Vector3.up * eyeHeight;

        Gizmos.color = new Color(0, 1, 1, 0.15f);
        DrawCircle(eye, stats.visionDistance, 24, h);

        Gizmos.color = new Color(0, 1, 1, 0.9f);
        Vector3 L = DirFromAngle(h, -stats.visionAngle * 0.5f);
        Vector3 R = DirFromAngle(h, +stats.visionAngle * 0.5f);
        Gizmos.DrawLine(eye, eye + L * stats.visionDistance);
        Gizmos.DrawLine(eye, eye + R * stats.visionDistance);
    }

    static Vector3 DirFromAngle(Transform basis, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        Vector3 local = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        return basis.TransformDirection(local);
    }

    static void DrawCircle(Vector3 center, float radius, int segs, Transform basis)
    {
        Vector3 prev = center + basis.forward * radius;
        for (int i = 1; i <= segs; i++)
        {
            float t = (i / (float)segs) * 360f;
            Vector3 next = center + DirFromAngle(basis, t) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
