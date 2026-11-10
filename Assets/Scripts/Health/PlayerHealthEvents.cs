using UnityEngine;

// Forwards the player's Health changes to the EventBus, so camera, audio and UI can react without
// a reference to the player. Put it next to the player's Health component.
// - PlayerDamaged: only when damage is taken (sounds, camera shake).
// - PlayerHealthChanged: on every change (damage, healing, respawn, a new max health) and once at start,
//   so displays such as the HUD always know the current and max health.
// - PlayerDied: when health reaches zero.
[RequireComponent(typeof(Health))]
public class PlayerHealthEvents : MonoBehaviour
{
    private Health health;
    private int lastHealth;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.HealthChanged += OnHealthChanged;
        health.Death += OnDeath;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.HealthChanged -= OnHealthChanged;
        health.Death -= OnDeath;
    }

    // After every OnEnable in the scene, so listeners that subscribed there get the starting values.
    private void Start()
    {
        lastHealth = health.CurrentHealth;
        EventBus.Publish(new PlayerHealthChanged { Current = health.CurrentHealth, Max = health.MaxHealth, Delta = 0 });
    }

    private void OnDamaged(int amount)
    {
        EventBus.Publish(new PlayerDamaged { Damage = amount, CurrentHealth = health.CurrentHealth, MaxHealth = health.MaxHealth });
    }

    private void OnHealthChanged(int current, int max)
    {
        int delta = current - lastHealth;
        lastHealth = current;
        EventBus.Publish(new PlayerHealthChanged { Current = current, Max = max, Delta = delta });
    }

    private void OnDeath()
    {
        EventBus.Publish(new PlayerDied());
    }
}
