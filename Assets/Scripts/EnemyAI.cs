// EnemyAI.cs
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Stagger, Dead }

    [Header("Referencias")]
    public Transform player;            // asignalo en escena o búscalo en Start
    public EnemyStats stats;            // <- SOLO LECTURA (estático)
    public Health health;               // componente que guarda vida ACTUAL
    CharacterController controller;

    // Cache local (copias inmutables en runtime) — NO escribir al SO
    float moveSpeed;
    float stopDistance;
    float chaseDistance;
    bool useVisionCone;
    float visionAngle;
    float visionDistance;
    float touchDamage;
    LayerMask visionObstacles;

    // Estado dinámico (siempre en escena, nunca en el SO)
    State state = State.Idle;
    float attackCooldownTimer = 0f;
    Vector3 velocity;

    // --- Respawn ---
    static bool enemyAlive = false;        // solo puede haber uno vivo
    Vector3 spawnPos;                    // donde revivimos
    Quaternion spawnRot;

    void Reset()
    {
        health = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
    }

    void Awake()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        if (!health) health = GetComponent<Health>();
    }

    void Start()
    {
        if (stats == null)
        {
            Debug.LogError($"{name}: EnemyStats no asignado.");
            enabled = false;
            return;
        }

        // Copiamos valores ESTÁTICOS desde el SO
        moveSpeed = stats.moveSpeed;
        stopDistance = stats.stopDistance;
        chaseDistance = stats.chaseDistance;
        useVisionCone = stats.useVisionCone;
        visionAngle = stats.visionAngle;
        visionDistance = stats.visionDistance;
        touchDamage = stats.touchDamage;
        visionObstacles = stats.visionObstacles;

        // Inicializamos vida ACTUAL desde el valor base del SO
        if (health != null)
            health.SetMaxAndFill(stats.maxHealth);

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // Guardamos punto de respawn
        spawnPos = transform.position;
        spawnRot = transform.rotation;

        // Hay un enemigo vivo
        enemyAlive = true;
    }

    void Update()
    {
        // Si está muerto, dejamos que este mismo script escuche F3 para revivir
        if (state == State.Dead)
        {
            if (Input.GetKeyDown(KeyCode.F3) && !enemyAlive)
                ReviveAtSpawn();
            return;
        }

        // Timers dinámicos
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        // Lógica simple de persecución por distancia + cono de visión
        bool canSeePlayer = PlayerInSight();
        float dist = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        if (player && (dist <= chaseDistance) && (!useVisionCone || canSeePlayer))
        {
            state = State.Chase;
            ChasePlayer(dist);

            // Avisar al PlayerController que está bajo amenaza: drena a la tasa del SO
            var pc = player.GetComponent<PlayerController>();
            if (pc) pc.SetThreat(stats.staminaDrainPerSecond);
        }
        else
        {
            state = State.Idle;
            Idle();

            // Sin amenaza: no drena, puede regenerar
            var pc = player ? player.GetComponent<PlayerController>() : null;
            if (pc) pc.SetThreat(0f);
        }
    }

    bool PlayerInSight()
    {
        if (!player) return false;
        if (!useVisionCone) return true;

        Vector3 toPlayer = (player.position - transform.position);
        float sqrDist = toPlayer.sqrMagnitude;
        if (sqrDist > visionDistance * visionDistance) return false;

        // Ángulo
        Vector3 fwd = transform.forward;
        toPlayer.y = 0f; fwd.y = 0f;
        if (Vector3.Angle(fwd, toPlayer.normalized) > visionAngle) return false;

        // Raycast opcional para obstáculos
        if (visionObstacles.value != 0)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 1.6f,
                                toPlayer.normalized, out RaycastHit hit, visionDistance, ~0))
            {
                if (hit.collider && ((visionObstacles.value & (1 << hit.collider.gameObject.layer)) != 0))
                    return false;
            }
        }
        return true;
    }

    void ChasePlayer(float dist)
    {
        // Mirar hacia el player
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // Moverse si aún no llegó al “stopDistance”
        if (dist > stopDistance)
        {
            Vector3 step = dir.normalized * moveSpeed;
            controller.SimpleMove(step);
        }
        else
        {
            TryAttack();
        }
    }

    void TryAttack()
    {
        if (attackCooldownTimer > 0f) return;

        // Aplica daño de contacto (ejemplo simple)
        var targetHealth = player ? player.GetComponent<Health>() : null;
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(Mathf.RoundToInt(touchDamage));
        }

        attackCooldownTimer = 1.0f; // cooldown dinámico (no va en el SO)
    }

    void Idle()
    {
        // Podés agregar patrol, anim idles, etc.
    }

    // Llamar esto desde Health cuando la vida actual llegue a 0
    public void OnDeath()
    {
        state = State.Dead;

        // Dejar de presionar al jugador
        if (player)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc) pc.SetThreat(0f);
        }

        // Desactivar movimiento/colisión/visual pero NO destruir (así escucha F3)
        if (controller) controller.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        // Ya no hay enemigo "vivo"
        enemyAlive = false;
    }

    void ReviveAtSpawn()
    {
        // Solo revive si actualmente está muerto y no hay enemigo vivo
        if (state != State.Dead || enemyAlive) return;

        // Resetear transform al punto de spawn
        transform.position = spawnPos;
        transform.rotation = spawnRot;

        // Resetear vida y estado
        if (health != null) health.SetMaxAndFill(stats.maxHealth);
        state = State.Idle;

        // Rehabilitar colisiones, render y controller
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
        if (controller) controller.enabled = true;

        // Marcar que ahora hay uno vivo
        enemyAlive = true;

        Debug.Log("[EnemyAI] Respawn (F3)");
    }
}
