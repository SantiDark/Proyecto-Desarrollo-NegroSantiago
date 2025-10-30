using UnityEngine;

[RequireComponent(typeof(CharacterController))]
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
    public float standHeight = 2f;
    public float crouchHeight = 1f;           // 50% de la altura normal
    public float crouchSpeedMultiplier = 0.75f; // velocidad reducida 25%
    bool isCrouching = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        stamina = maxStamina; // arranca llena
        controller.height = standHeight;
    }

    void Update()
    {
        // Si el controller está deshabilitado (p.ej., al morir), no procesar input
        if (controller == null || !controller.enabled) return;

        HandleMovement();
        HandleCrouch();
        HandleStamina();
    }

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.right * x + transform.forward * z).normalized;

        float speed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        controller.SimpleMove(move * speed);
    }

    void HandleCrouch()
    {
        bool crouchInput = Input.GetKey(crouchKey1) || Input.GetKey(crouchKey2);

        if (crouchInput && !isCrouching)
        {
            isCrouching = true;
            controller.height = crouchHeight;
        }
        else if (!crouchInput && isCrouching)
        {
            isCrouching = false;
            controller.height = standHeight;
        }
    }

    void HandleStamina()
    {
        if (threatDrainPerSecond > 0f)
            stamina -= threatDrainPerSecond * Time.deltaTime;
        else
            stamina += staminaRecoverRate * Time.deltaTime;

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }

    // Define el drenaje por “amenaza” en stamina (por segundo)
    public void SetThreat(float drainPerSecond)
    {
        threatDrainPerSecond = Mathf.Max(0f, drainPerSecond);
    }

    public float GetStamina() => stamina;

    public void OnPlayerDeath()
    {
        if (controller) controller.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        Debug.Log("[Player] Muerto");
    }
}
