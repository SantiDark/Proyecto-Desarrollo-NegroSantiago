using UnityEngine;
using UnityEngine.Events;
using System;

public class Health : MonoBehaviour
{
    [SerializeField] int maxHealth = 100;
    int current;

    public UnityEvent onDeath;
    public event Action<int, int> OnHealthChanged;

    public int CurrentHealth => current;
    public int MaxHealth => maxHealth;

    void Start()
    {
        current = maxHealth;                     // ✅ arranca lleno
        OnHealthChanged?.Invoke(current, maxHealth);
    }

    public void SetMaxAndFill(int max)
    {
        maxHealth = Mathf.Max(1, max);
        current = maxHealth;
        OnHealthChanged?.Invoke(current, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (current <= 0) return;

        int prev = current;
        current = Mathf.Max(0, current - Mathf.Max(0, amount));  // ✅ resta daño

        if (current != prev)
            OnHealthChanged?.Invoke(current, maxHealth);

        if (current == 0)
        {
            // 🔸 Dispara evento Unity para animaciones u otros
            onDeath?.Invoke();

            // 🔸 Si es enemigo, llamamos a su muerte
            var ai = GetComponent<EnemyAI>();
            if (ai)
            {
                ai.OnDeath();
                return;
            }

            // 🔸 Si es jugador, avisamos a su controlador
            var player = GetComponent<PlayerController>();
            if (player)
            {
                player.OnPlayerDeath();
                return;
            }
        }
    }

    public void Heal(int amount)
    {
        if (current <= 0) return;
        int prev = current;
        current = Mathf.Min(maxHealth, current + Mathf.Max(0, amount));

        if (current != prev)
            OnHealthChanged?.Invoke(current, maxHealth);
    }
}
