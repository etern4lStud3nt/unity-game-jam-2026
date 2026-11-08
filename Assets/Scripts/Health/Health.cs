using System;
using UnityEngine;
using UnityEngine.Events;

// Gives any object hit points: add it to the player, an enemy or a breakable crate and set Max Health.
// Attackers find it with GetComponentInParent<Health>() and call TakeDamage. Other components (audio, UI,
// animation, drops) react through the events instead of the attack code knowing about them.
public class Health : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 3;
    [Tooltip("Seconds after a hit during which further damage is ignored. 0 = every hit counts.")]
    [SerializeField, Min(0f)] private float invincibilityDuration;
    [Tooltip("Destroy this GameObject when health reaches 0. Leave off for the player (respawn) or objects with a death animation.")]
    [SerializeField] private bool destroyOnDeath;

    [Header("Events (wire sounds, effects or UI here in the inspector)")]
    [SerializeField] private UnityEvent<int> onDamaged = new();
    [SerializeField] private UnityEvent<int> onHealed = new();
    [SerializeField] private UnityEvent onDeath = new();

    private int currentHealth;
    private float invincibleUntil;

    // Same moments for code: damage or heal amount, and (current, max) for health bars.
    public event Action<int> Damaged;
    public event Action<int> Healed;
    public event Action Death;
    public event Action<int, int> HealthChanged;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;
    public bool IsInvincible => Time.time < invincibleUntil;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // Returns true if the damage was taken (false while dead or invincible).
    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead || IsInvincible)
            return false;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        invincibleUntil = Time.time + invincibilityDuration;

        Damaged?.Invoke(amount);
        onDamaged.Invoke(amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (IsDead)
            Die();
        return true;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead || currentHealth == maxHealth)
            return;

        int healed = Mathf.Min(amount, maxHealth - currentHealth);
        currentHealth += healed;

        Healed?.Invoke(healed);
        onHealed.Invoke(healed);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Dies immediately, ignoring invincibility (pits, instant-kill hazards).
    public void Kill()
    {
        if (IsDead)
            return;

        currentHealth = 0;
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Die();
    }

    // Back to full health and alive again, e.g. on respawn.
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        invincibleUntil = 0f;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // For upgrades that raise max health; optionally fills up the new amount too.
    public void SetMaxHealth(int value, bool fill)
    {
        maxHealth = Mathf.Max(1, value);
        currentHealth = fill ? maxHealth : Mathf.Min(currentHealth, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Death?.Invoke();
        onDeath.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}
