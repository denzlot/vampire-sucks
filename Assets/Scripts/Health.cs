using UnityEngine;
using UnityEngine.Events;

// Универсальное здоровье. Вешается и на игрока, и на врага.
public class Health : MonoBehaviour
{
    [Header("Здоровье")]
    public int maxHealth = 100;
    public int currentHealth;

    [Tooltip("Пока true — урон не проходит (используется дэшем для i-frames).")]
    public bool isInvulnerable = false;

    [Header("События (можно оставить пустыми)")]
    [Tooltip("Сработает при получении урона")]
    public UnityEvent onDamaged;
    [Tooltip("Сработает при смерти (перед удалением объекта)")]
    public UnityEvent onDeath;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    // Нанести урон. Этот метод вызывают атаки игрока и врага.
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;   // уже мёртв
        if (isInvulnerable) return;       // неуязвим (например, во время дэша)

        currentHealth -= amount;
        onDamaged?.Invoke();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        onDeath?.Invoke();
        Destroy(gameObject);
    }
}
