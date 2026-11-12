using UnityEngine;

namespace Bosses
{
    // A trap the Old Self lays on the floor or in the air in the second half. Blinks for Trap Arm Time (harmless),
    // then hurts on touch (a trigger with DamageOnContact on this object) until Trap Lifetime runs out. A hit breaks
    // it (its own Health, so the player can also pogo off it). Keep it outside the boss's object (a sibling under
    // the prefab root). Its pivot sits on the floor: place the sprite and collider above it, Height tall.
    [RequireComponent(typeof(Health), typeof(Collider2D))]
    public class OldSelfTrap : MonoBehaviour
    {
        [Tooltip("How far the sprite and collider reach above the pivot; a trap in the air is kept this far below a ceiling.")]
        [SerializeField, Min(0f)] private float height = 0.5f;

        private OldSelfBoss boss;
        private Health health;
        private Collider2D hurtArea;
        private SpriteRenderer[] sprites;
        private float placedAt;
        private bool armed;
        private bool visible;

        public float Height => height;

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

        // Appears at point, harmless while it blinks.
        public void Place(Vector2 point)
        {
            transform.position = point;
            placedAt = Time.time;
            gameObject.SetActive(true);
            health.ResetHealth();
            armed = false;
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
            OldSelfStats stats = boss.Stats;
            float age = Time.time - placedAt;
            if (age >= stats.TrapArmTime + stats.TrapLifetime)
            {
                PutAway();
                return;
            }
            if (armed)
                return;

            if (age >= stats.TrapArmTime)
            {
                armed = true;
                hurtArea.enabled = true;
                SetVisible(true);
                return;
            }
            SetVisible(Mathf.Repeat(age * stats.TrapBlinkRate * 2f, 2f) < 1f);
        }

        private void SetVisible(bool on)
        {
            if (on == visible)
                return;
            visible = on;
            foreach (SpriteRenderer sprite in sprites)
                sprite.enabled = on;
        }
    }
}
