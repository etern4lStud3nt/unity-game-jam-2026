using UnityEngine;

// Forwards the player's Health changes to the EventBus, so camera, audio and UI can react without
// a reference to the player. Put it next to the player's Health component.
[RequireComponent(typeof(Health))]
public class PlayerHealthEvents : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Death += OnDeath;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Death -= OnDeath;
    }

    private void OnDamaged(int amount)
    {
        EventBus.Publish(new PlayerDamaged { Damage = amount, CurrentHealth = health.CurrentHealth, MaxHealth = health.MaxHealth });
    }

    private void OnDeath()
    {
        EventBus.Publish(new PlayerDied());
    }
}
