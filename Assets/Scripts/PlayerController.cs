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
    [Range(0.1f, 1f)] public float crouchHeightFactor = 0.5f; // 50% de altura
    [Range(0f, 1f)] public float crouchSpeedMult = 0.75f;    // velocidad -25%
    public float crouchLerp = 15f;                            // suavizado de transición

    bool isCrouching = false;
    float baseHeight;
    Vector3 baseCenter;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        stamina = maxStamina; // arranca llena

        baseHeight = controller.height;
        baseCenter = controller.center;
    }

    void Update()
    {
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

        float targetHeight = isCrouching ? baseHeight * crouchHeightFactor : baseHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchLerp);

        // ajusta el centro para no hundirse visualmente
        float heightRatio = controller.height / baseHeight;
        Vector3 targetCenter = new Vector3(baseCenter.x, baseCenter.y * heightRatio, baseCenter.z);
        controller.center = Vector3.Lerp(controller.center, targetCenter, Time.deltaTime * crouchLerp);
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

    public void OnPlayerDeath()
    {
        if (controller) controller.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        Debug.Log("[Player] Muerto");
    }
}
