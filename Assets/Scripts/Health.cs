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
        current = maxHealth;
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

        int dmg = Mathf.Max(0, amount);
        int prev = current;

        current = Mathf.Max(0, current - dmg);

        if (current != prev)
            OnHealthChanged?.Invoke(current, maxHealth);

        // 👇 Buscamos EnemyAI en este objeto o en el padre (por si el Health está en un hijo)
        var ai = GetComponentInParent<EnemyAI>();
        if (ai && dmg > 0)
        {
            if (current > 0)
            {
                // Recibió un tiro y sobrevivió → OnDamage (timer 3s)
                ai.OnDamage(dmg);
            }
            else
            {
                // Lo matamos con este disparo
                ai.OnDeath();
                return;
            }
        }

        // Lógica de muerte genérica (jugador u otros)
        if (current == 0)
        {
            onDeath?.Invoke();

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
