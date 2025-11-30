using System;
using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public enum State { normal, patrol, alert, chase, damage, dead }

    [Header("Referencias")]
    public Transform player;         // arrastrar Player (root, con Health)
    public EnemyStats stats;         // ScriptableObject con visión/velocidad
    public Health health;            // Health del enemigo

    [Header("UI")]
    public TextMeshPro stateText;    // TMP hijo para mostrar estado

    [Header("Combate")]
    public float fireDistance = 12f; // alcance para disparar al jugador
    public float fireRate = 1.0f;    // disparos por segundo
    public int damagePerShot = 10;   // daño al jugador por tiro
    float fireCooldown = 0f;

    [Header("Patrulla")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2.0f;
    public float patrolArriveThreshold = 0.3f;
    int patrolIndex = 0;

    [Header("Debug/Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoVisionColor = new Color(0, 1, 0, 0.15f);
    public Color gizmoEdgeColor = new Color(0, 0.8f, 0, 0.9f);
    public Color gizmoBlockedColor = new Color(1, 0, 0, 0.9f);

    CharacterController controller;
    State state = State.normal;
    float eyeHeight = 1.6f;

    // --- ALERTA GLOBAL ---
    public static event Action OnGlobalAlert;
    bool alertAfterHitCoroutineRunning = false;

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

    void OnEnable()
    {
        OnGlobalAlert += HandleGlobalAlert;
    }

    void OnDisable()
    {
        OnGlobalAlert -= HandleGlobalAlert;
    }

    void Start()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
            SetState(State.patrol);
        else
            SetState(State.normal);
    }

    void Update()
    {
        if (state == State.dead) return;

        fireCooldown -= Time.deltaTime;

        RunState();

        // DISPARO: solo ataca si YA está en alert/chase
        if (player && (state == State.alert || state == State.chase))
        {
            if (CanSeePlayer())
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= fireDistance)
                {
                    SetState(State.chase);   // sigue en persecución mientras dispara
                    TryShootAtPlayer();
                }
            }
        }

        if (stateText) stateText.text = state.ToString();
    }

    // ------------------------------------------------------
    // CAMBIO DE ESTADO
    // ------------------------------------------------------
    void SetState(State newState)
    {
        if (state == newState) return;

        state = newState;
        Debug.Log($"[EnemyAI] {name} state -> {state}");

        if (stateText)
            stateText.text = state.ToString();
    }

    // ------------------------------------------------------
    //  VISIÓN (distancia + cono + oclusión)
    // ------------------------------------------------------
    bool CanSeePlayer()
    {
        if (!player || !stats) return false;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 playerEye = player.position + Vector3.up * eyeHeight;
        Vector3 toTarget = playerEye - eye;

        float dist = toTarget.magnitude;
        if (dist > stats.visionDistance) return false;

        if (stats.useVisionCone)
        {
            float angle = Vector3.Angle(transform.forward, toTarget);
            if (angle > stats.visionAngle * 0.5f) return false;
        }

        if (Physics.Raycast(eye, toTarget.normalized, out RaycastHit hit,
            stats.visionDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != player)
            {
                int mask = 1 << hit.transform.gameObject.layer;
                if ((stats.visionObstacles.value & mask) != 0)
                    return false;
            }
        }

        return true;
    }

    // ------------------------------------------------------
    //  LÓGICA POR ESTADO
    // ------------------------------------------------------
    void RunState()
    {
        switch (state)
        {
            case State.normal:
                controller.SimpleMove(Vector3.zero);
                if (player) Face(player.position);

                // Descubrimiento visual directo → ALERT inmediato
                if (CanSeePlayer())
                {
                    RaiseGlobalAlertFromEnemy();
                    SetState(State.alert);
                }
                break;

            case State.patrol:
                UpdatePatrol();

                // Descubrimiento visual desde patrulla → ALERT inmediato
                if (CanSeePlayer())
                {
                    RaiseGlobalAlertFromEnemy();
                    SetState(State.alert);
                }
                break;

            case State.alert:
            case State.chase:
                // En alerta o persecución → se mueve hacia el jugador
                UpdateMoveTowardsPlayer();
                break;

            case State.damage:
                // Acaba de recibir un disparo pero todavía no entró en alerta.
                // Se queda quieto (feedback de impacto) hasta que la corrutina lo pase a alert.
                controller.SimpleMove(Vector3.zero);
                if (player) Face(player.position);
                break;

            case State.dead:
                controller.SimpleMove(Vector3.zero);
                break;
        }
    }

    void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            controller.SimpleMove(Vector3.zero);
            return;
        }

        Transform targetPoint = patrolPoints[patrolIndex];
        Vector3 currentPos = transform.position;
        Vector3 targetPos = targetPoint.position;

        Vector3 toTarget = targetPos - currentPos;
        toTarget.y = 0f;

        float sqrDist = toTarget.sqrMagnitude;
        float sqrThreshold = patrolArriveThreshold * patrolArriveThreshold;

        if (sqrDist <= sqrThreshold)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            return;
        }

        Vector3 dir = toTarget.normalized;
        controller.SimpleMove(dir * patrolSpeed);
        Face(targetPos);
    }

    void UpdateMoveTowardsPlayer()
    {
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
    }

    // ------------------------------------------------------
    //  DISPARO
    // ------------------------------------------------------
    void TryShootAtPlayer()
    {
        if (!player) return;
        if (fireCooldown > 0f) return;

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damagePerShot);
            Debug.Log($"[Soldier] Hit PLAYER for {damagePerShot} dmg");
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

    // ------------------------------------------------------
    //  DAÑO Y MUERTE DEL ENEMIGO
    // ------------------------------------------------------
    public void OnDamage(int amount)
    {
        if (state == State.dead) return;

        // Si ya estaba en alerta o persecución, no arrancamos el timer
        // (ya cumplió los otros disparadores de alerta).
        if (state == State.alert || state == State.chase)
            return;

        // Feedback inmediato de haber sido herido
        SetState(State.damage);

        // Timer de 3 segundos: si sobrevive, pasa a ALERT
        if (!alertAfterHitCoroutineRunning)
            StartCoroutine(AlertIfAliveAfterDelay(3f));
    }

    IEnumerator AlertIfAliveAfterDelay(float delay)
    {
        alertAfterHitCoroutineRunning = true;
        yield return new WaitForSeconds(delay);

        // Si después de 3 segundos sigue vivo:
        if (state != State.dead)
        {
            RaiseGlobalAlertFromEnemy();
            SetState(State.alert);
        }

        alertAfterHitCoroutineRunning = false;
    }

    public void OnDeath()
    {
        SetState(State.dead);
        if (controller) controller.enabled = false;
        Destroy(gameObject, 0f);
    }

    // ------------------------------------------------------
    //  ALERTA GLOBAL
    // ------------------------------------------------------
    public static void AlertAllFromCamera()
    {
        Debug.Log("[EnemyAI] Global alert from camera");
        OnGlobalAlert?.Invoke();
    }

    void RaiseGlobalAlertFromEnemy()
    {
        Debug.Log($"[EnemyAI] {name} raising global alert");
        OnGlobalAlert?.Invoke();
    }

    void HandleGlobalAlert()
    {
        if (state == State.dead) return;
        SetState(State.alert);
    }

    // --- GIZMOS ---
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || stats == null) return;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;

        Gizmos.color = gizmoVisionColor;
        DrawCircle(eye, stats.visionDistance, 24);

        Gizmos.color = gizmoEdgeColor;
        Vector3 left = DirFromAngle(-stats.visionAngle * 0.5f);
        Vector3 right = DirFromAngle(+stats.visionAngle * 0.5f);
        Gizmos.DrawLine(eye, eye + left * stats.visionDistance);
        Gizmos.DrawLine(eye, eye + right * stats.visionDistance);

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

    Vector3 DirFromAngle(float degrees)
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
