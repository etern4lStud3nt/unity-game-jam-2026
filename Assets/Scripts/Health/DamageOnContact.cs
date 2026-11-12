using UnityEngine;

// Damages any object with a Health component that touches this one: spikes, enemy bodies, projectiles.
// Works with both trigger and solid 2D colliders, and keeps hurting while the object stays in contact
// (the target's invincibility time decides how often a hit actually lands). Disabling the component stops the
// damage but keeps the collider, so the object can still be hit or pogoed.
[RequireComponent(typeof(Collider2D))]
public class DamageOnContact : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    [Tooltip("Only objects with this tag are hurt. Leave empty to hurt anything with Health.")]
    [SerializeField] private string targetTag = "Player";
    [Tooltip("Destroy this object after it hits something (projectiles).")]
    [SerializeField] private bool destroyOnHit;

    private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

    private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);

    private void TryDamage(Collider2D other)
    {
        // Unity sends contact messages to disabled components too.
        if (!enabled)
            return;

        // The collider can sit on a child, so look up from it; the Rigidbody2D object usually carries the tag.
        Health health = other.GetComponentInParent<Health>();
        if (health == null || health.gameObject == gameObject || !HasTargetTag(other, health))
            return;

        if (health.TakeDamage(damage) && destroyOnHit)
            Destroy(gameObject);
    }

    private bool HasTargetTag(Collider2D other, Health health)
    {
        return string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag) || health.CompareTag(targetTag);
    }
}
