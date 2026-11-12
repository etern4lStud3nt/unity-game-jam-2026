using UnityEngine;

namespace Bosses
{
    // A trap the Old Self lays on the floor in the second half. Blinks for Trap Arm Time (harmless), then hurts on
    // touch (a trigger with DamageOnContact on this object) until Trap Lifetime runs out. A hit breaks it (its own
    // Health, so the player can also pogo off it). Keep it outside the boss's object (a sibling under the prefab
    // root). Its pivot sits on the floor: place the sprite and collider above it.
    [RequireComponent(typeof(Health), typeof(Collider2D))]
    public class OldSelfTrap : MonoBehaviour
    {
        private const float BlinkRate = 12f;

        private OldSelfBoss boss;
        private Health health;
        private Collider2D hurtArea;
        private SpriteRenderer[] sprites;
        private float placedAt;

        public void Init(OldSelfBoss owner)
        {
            boss = owner;
            health = GetComponent<Health>();
            hurtArea = GetComponent<Collider2D>();
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
            health.Death += PutAway;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (health != null)
                health.Death -= PutAway;
        }

        // Appears on the floor at point, harmless while it blinks.
        public void Place(Vector2 point)
        {
            transform.position = point;
            placedAt = Time.time;
            gameObject.SetActive(true);
            health.ResetHealth();
            hurtArea.enabled = false;
            SetVisible(true);
        }

        public void PutAway()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void Update()
        {
            if (boss == null)
                return;
            float age = Time.time - placedAt;
            bool armed = age >= boss.Stats.TrapArmTime;
            hurtArea.enabled = armed;
            SetVisible(armed || Mathf.Repeat(age * BlinkRate, 2f) < 1f);
            if (age >= boss.Stats.TrapArmTime + boss.Stats.TrapLifetime)
                PutAway();
        }

        private void SetVisible(bool on)
        {
            foreach (SpriteRenderer sprite in sprites)
                sprite.enabled = on;
        }
    }
}
