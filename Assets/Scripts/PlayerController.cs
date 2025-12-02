using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 6f;
    CharacterController controller;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRecoverRate = 10f;

    float stamina;
    float threatDrainPerSecond = 0f;

    // --- AGACHARSE ---
    [Header("Crouch")]
    public KeyCode crouchKey1 = KeyCode.C;
    public KeyCode crouchKey2 = KeyCode.LeftControl;
    [Range(0.1f, 1f)] public float crouchHeightFactor = 0.5f; // 50% de altura
    [Range(0f, 1f)] public float crouchSpeedMult = 0.75f;    // velocidad -25%
    public float crouchLerp = 15f;                            // suavizado de transición

    bool isCrouching = false;
    float baseHeight;
    Vector3 baseCenter;

    // --- Vida / muerte / respawn ---
    Health health;
    bool isDead = false;
    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();

        stamina = maxStamina; // arranca llena

        baseHeight = controller.height;
        baseCenter = controller.center;

        // punto inicial de respawn
        spawnPos = transform.position;
        spawnRot = transform.rotation;
    }

    void Update()
    {
        // F1: respawn con valores iniciales
        if (Input.GetKeyDown(KeyCode.F1))
            RespawnPlayer();

        // F2: reiniciar escena
        if (Input.GetKeyDown(KeyCode.F2))
            ReloadScene();

        // si está muerto, no procesa movimiento ni stamina
        if (isDead) return;

        if (controller == null || !controller.enabled) return;

        HandleCrouch();
        HandleMovement();
        HandleStamina();
    }

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.right * x + transform.forward * z).normalized;

        float speed = isCrouching ? moveSpeed * crouchSpeedMult : moveSpeed;
        controller.SimpleMove(move * speed);
    }

    void HandleCrouch()
    {
        bool crouchInput = Input.GetKey(crouchKey1) || Input.GetKey(crouchKey2);
        isCrouching = crouchInput;

        float targetFactor = isCrouching ? crouchHeightFactor : 1f;

        float targetHeight = baseHeight * targetFactor;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchLerp);

        float heightDiff = baseHeight - controller.height;
        float centerOffset = heightDiff * 0.5f;
        Vector3 targetCenter = new Vector3(baseCenter.x,
                                           baseCenter.y - centerOffset,
                                           baseCenter.z);
        controller.center = Vector3.Lerp(controller.center, targetCenter, Time.deltaTime * crouchLerp);

        Vector3 scale = transform.localScale;
        float targetScaleY = targetFactor;
        scale.y = Mathf.Lerp(scale.y, targetScaleY, Time.deltaTime * crouchLerp);
        transform.localScale = scale;
    }

    void HandleStamina()
    {
        if (threatDrainPerSecond > 0f)
            stamina -= threatDrainPerSecond * Time.deltaTime;
        else
            stamina += staminaRecoverRate * Time.deltaTime;

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }

    public void SetThreat(float drainPerSecond)
    {
        threatDrainPerSecond = Mathf.Max(0f, drainPerSecond);
    }

    public float GetStamina() => stamina;

    // =========================================================
    // LLAMADO DESDE Health CUANDO current == 0
    // =========================================================
    public void OnPlayerDeath()
    {
        isDead = true;

        if (controller) controller.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        // 🔫 Desactivar el arma para que no pueda disparar muerto
        Gun gun = GetComponentInChildren<Gun>();
        if (gun != null)
            gun.enabled = false;

        Debug.Log("[Player] Muerto. F1 = respawn, F2 = reiniciar escena.");
    }

    // =========================================================
    // F1 – Respawn con valores iniciales
    // =========================================================
    void RespawnPlayer()
    {
        // reposicionar en spawn
        controller.enabled = false;
        transform.position = spawnPos;
        transform.rotation = spawnRot;
        controller.enabled = true;

        // restaurar tamaño del CharacterController
        controller.height = baseHeight;
        controller.center = baseCenter;

        // reactivar colliders y renders
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;

        // resetear vida y stamina
        if (health != null)
            health.SetMaxAndFill(health.MaxHealth);

        stamina = maxStamina;
        threatDrainPerSecond = 0f;
        isDead = false;

        // 🔫 Rehabilitar arma y resetear ammo al respawn
        Gun gun = GetComponentInChildren<Gun>();
        if (gun != null)
        {
            gun.enabled = true;
            gun.ResetAmmo();
        }

        Debug.Log("[Player] Respawn realizado.");
    }

    // =========================================================
    // F2 – Reiniciar escena actual
    // =========================================================
    void ReloadScene()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
