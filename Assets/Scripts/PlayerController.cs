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

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        stamina = maxStamina; // arranca llena
    }

    void Update()
    {
        // Si el controller está deshabilitado (p.ej., al morir), no procesar input
        if (controller == null || !controller.enabled) return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        controller.SimpleMove(move * moveSpeed);

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
