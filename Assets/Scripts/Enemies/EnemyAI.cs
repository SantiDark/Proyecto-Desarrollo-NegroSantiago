using UnityEngine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public enum State { normal, chase, damage, dead }

    [Header("Referencias")]
    public Transform player;         // arrastrar Player
    public EnemyStats stats;         // ScriptableObject con visión/velocidad
    public Health health;            // Health del enemigo

    [Header("UI")]
    public TextMeshPro stateText;    // TMP hijo para mostrar estado

    [Header("Combate")]
    public float fireDistance = 12f; // alcance para disparar al jugador
    public float fireRate = 1.0f;    // disparos por segundo
    public int damagePerShot = 10;   // daño al jugador por tiro
    float fireCooldown = 0f;

    [Header("Debug/Gizmos")]
    public bool drawGizmos = true;   // mostrar cono de visión en la escena
    public Color gizmoVisionColor = new Color(0, 1, 0, 0.15f);
    public Color gizmoEdgeColor = new Color(0, 0.8f, 0, 0.9f);
    public Color gizmoBlockedColor = new Color(1, 0, 0, 0.9f);

    CharacterController controller;
    State state = State.normal;
    bool lockedInChase = false;      // una vez que entra en chase, no sale
    float eyeHeight = 1.6f;

    void Reset()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (health == null) health = GetComponent<Health>();
    }

    void Update()
    {
        if (state == State.dead) return;

        fireCooldown -= Time.deltaTime;

        DetectPlayer();   // visión (vectores) + oclusión
        RunState();       // movimiento

        if (stateText) stateText.text = state.ToString();
    }

    // --- DETECCIÓN CON VECTORES + OCULSIÓN ---
    void DetectPlayer()
    {
        if (lockedInChase) { state = State.chase; return; }
        if (player == null || stats == null) return;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 playerEye = player.position + Vector3.up * eyeHeight;
        Vector3 toTarget = playerEye - eye;

        // 1) Alcance
        float dist = Vector3.Distance(eye, playerEye);
        if (dist > stats.visionDistance) return;

        // 2) Ángulo (cono)
        if (stats.useVisionCone)
        {
            float angle = Vector3.Angle(transform.forward, toTarget); // [0..180]
            if (angle > stats.visionAngle * 0.5f) return;
        }

        // 3) Oclusión por Obstacles
        if (Physics.Raycast(eye, toTarget.normalized, out RaycastHit hit, stats.visionDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != player)
            {
                int hitLayerMask = 1 << hit.transform.gameObject.layer;
                if ((stats.visionObstacles.value & hitLayerMask) != 0)
                    return; // bloqueada la visión
            }
        }

        // visto!
        state = State.chase;
        lockedInChase = true; // ya no vuelve atrás hasta morir
    }

    void RunState()
    {
        switch (state)
        {
            case State.normal:
                controller.SimpleMove(Vector3.zero);
                if (player) Face(player.position);
                break;

            case State.chase:
            case State.damage: // damage cae inmediatamente en chase
                if (!player) return;
                Vector3 to = player.position - transform.position;
                to.y = 0f;

                float stop = stats.stopDistance;
                if (to.sqrMagnitude > stop * stop)
                {
                    Vector3 dir = to.normalized;
                    controller.SimpleMove(dir * stats.moveSpeed);
                    Face(player.position);
                }
                else
                {
                    controller.SimpleMove(Vector3.zero);
                    Face(player.position);
                }

                TryShootAtPlayer(); // dispara sólo en chase/damage
                break;
        }
    }

    void TryShootAtPlayer()
    {
        if (!player || fireCooldown > 0f) return;
        if (state != State.chase && state != State.damage) return;

        Vector3 from = transform.position + Vector3.up * eyeHeight;
        Vector3 to = player.position + Vector3.up * eyeHeight;
        Vector3 dir = (to - from).normalized;

        float dist = Vector3.Distance(from, to);
        if (dist > fireDistance) return;

        // LoS: que no lo tape Obstacles
        if (Physics.Raycast(from, dir, out RaycastHit hit, fireDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != player)
            {
                int mask = 1 << hit.transform.gameObject.layer;
                if ((stats.visionObstacles.value & mask) != 0)
                    return; // tapado por Obstacles
            }
        }

        // aplicar daño al jugador
        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth)
        {
            playerHealth.TakeDamage(damagePerShot);
            Debug.Log($"[Soldier] Hit player for {damagePerShot} dmg");
        }

        fireCooldown = 1f / Mathf.Max(0.01f, fireRate);
    }

    void Face(Vector3 worldPoint)
    {
        Vector3 fwd = (worldPoint - transform.position);
        fwd.y = 0f;
        if (fwd.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(fwd);
    }

    // Llamado desde Health al recibir daño (o desde la bala)
    public void OnDamage(int amount)
    {
        if (state == State.dead) return;
        state = State.damage;
        lockedInChase = true; // si le pegan, queda “lockeado” en chase
    }

    // Llamado desde el UnityEvent OnDeath de Health
    public void OnDeath()
    {
        state = State.dead;
        if (controller) controller.enabled = false;
        Destroy(gameObject, 0f);
    }

    // --- GIZMOS DEL CONO DE VISIÓN ---
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || stats == null) return;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;

        // Disco/radio
        Gizmos.color = gizmoVisionColor;
        DrawCircle(eye, stats.visionDistance, 24);

        // Cono (dos bordes)
        Gizmos.color = gizmoEdgeColor;
        Vector3 left = DirFromAngle(-stats.visionAngle * 0.5f);
        Vector3 right = DirFromAngle(+stats.visionAngle * 0.5f);
        Gizmos.DrawLine(eye, eye + left * stats.visionDistance);
        Gizmos.DrawLine(eye, eye + right * stats.visionDistance);

        // Raycast al jugador (verde si visible, rojo si bloqueado)
        if (player)
        {
            Vector3 playerEye = player.position + Vector3.up * eyeHeight;
            Vector3 toTarget = (playerEye - eye).normalized;

            bool blocked = false;
            if (Physics.Raycast(eye, toTarget, out RaycastHit hit, stats.visionDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != player)
                {
                    int mask = 1 << hit.transform.gameObject.layer;
                    blocked = (stats.visionObstacles.value & mask) != 0;
                }
            }

            Gizmos.color = blocked ? gizmoBlockedColor : gizmoEdgeColor;
            Gizmos.DrawLine(eye, playerEye);
        }
    }

    Vector3 DirFromAngle(float degrees)   // en espacio global (Y up)
    {
        float rad = degrees * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        return transform.TransformDirection(dir);
    }

    void DrawCircle(Vector3 center, float radius, int segments)
    {
        Vector3 prev = center + transform.forward * radius;
        for (int i = 1; i <= segments; i++)
        {
            float t = (i / (float)segments) * 360f;
            Vector3 next = center + DirFromAngle(t) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
