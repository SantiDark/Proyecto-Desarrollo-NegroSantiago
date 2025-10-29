using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Referencias (Player)")]
    [SerializeField] private Health playerHealth;          // Health del JUGADOR
    [SerializeField] private PlayerController playerController; // Controller del JUGADOR

    [Header("UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;

    void Awake()
    {
        // Si no están asignados por Inspector, buscá SIEMPRE al objeto con Tag "Player"
        if (!playerHealth || !playerController)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO)
            {
                if (!playerHealth) playerHealth = playerGO.GetComponent<Health>();
                if (!playerController) playerController = playerGO.GetComponent<PlayerController>();
            }
            else
            {
                Debug.LogWarning("[PlayerHUD] No hay objeto con Tag 'Player' en escena.");
            }
        }
    }

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += HandleHealthChanged;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    void Start()
    {
        // Inicial salud
        if (playerHealth && healthSlider)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = playerHealth.MaxHealth;     // propiedad del Health del JUGADOR
            healthSlider.value = playerHealth.CurrentHealth;
        }

        // Inicial stamina
        if (playerController && staminaSlider)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = playerController.maxStamina;
            staminaSlider.value = playerController.GetStamina();
        }
    }

    void Update()
    {
        if (playerController && staminaSlider)
            staminaSlider.value = playerController.GetStamina();
    }

    void HandleHealthChanged(int current, int max)
    {
        if (!healthSlider) return;
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }
}
