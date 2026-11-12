using UnityEngine;

namespace Bosses
{
    // One of the Mother's slippers. It first hangs where it will leave from (so the player can read the wave's gap),
    // popping in and wiggling, then flies flat in a straight line until it leaves the arena or reaches a wall,
    // stretched for a moment as it is let go and spinning all the way (Slipper Spin). Hurts on touch (a trigger with DamageOnContact on this object).
    // Pooled: hidden until the Mother needs it. Its first child is the sprite the pop, wiggle and stretch bend.
    // Keep it outside the boss's object (a sibling under the prefab root), or player hits on it would hurt the boss.
    [RequireComponent(typeof(Rigidbody2D))]
    public class ParentSlipper : MonoBehaviour
    {
        private ParentsBoss boss;
        private Rigidbody2D body;
        private Collider2D[] colliders;
        private Transform sprite;
        private Vector3 spriteScale;
        private int direction;
        private bool flying;
        private float shownAt;
        private float launchedAt;

        public bool InUse => gameObject.activeSelf;

        // Called once by the boss in its Awake. Hidden until used.
        public void Init(ParentsBoss owner)
        {
            boss = owner;
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            colliders = GetComponentsInChildren<Collider2D>(true);
            if (transform.childCount > 0)
            {
                sprite = transform.GetChild(0);
                spriteScale = sprite.localScale;
            }
            gameObject.SetActive(false);
        }

        // Appears at point, harmless and still, until Launch.
        public void Hold(Vector2 point, int facing)
        {
            flying = false;
            direction = facing >= 0 ? 1 : -1;
            shownAt = Time.time;
            SetHurting(false);
            transform.SetPositionAndRotation(point, Quaternion.identity);
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
            body.position = point;
            body.rotation = 0f;
            gameObject.SetActive(true);
            Bend();
        }

        public void Launch()
        {
            if (!InUse)
                return;
            flying = true;
            launchedAt = Time.time;
            SetHurting(true);
        }

        public void PutAway()
        {
            flying = false;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void Update() => Bend();

        // Pops in while it appears, wiggles while it hangs, stretches for a moment as it is let go.
        private void Bend()
        {
            if (sprite == null || boss == null)
                return;
            ParentsStats stats = boss.Stats;
            float pop = stats.SlipperPopTime;

            if (!flying)
            {
                float k = pop > 0f ? Mathf.Clamp01((Time.time - shownAt) / pop) : 1f;
                // Eases out past full size a little, then settles.
                float size = 1f + Mathf.Sin(k * Mathf.PI) * 0.25f * k;
                size *= 1f - (1f - k) * (1f - k);
                sprite.localScale = spriteScale * size;
                float wiggle = Mathf.Sin((Time.time - shownAt) * stats.SlipperWiggleRate * Mathf.PI * 2f) * stats.SlipperWiggle;
                sprite.localRotation = Quaternion.Euler(0f, 0f, wiggle);
                return;
            }

            // The object is mirrored when it flies left, so the same local spin rolls forwards both ways.
            sprite.localRotation = Quaternion.Euler(0f, 0f, -stats.SlipperSpin * (Time.time - launchedAt));
            float t = pop > 0f ? Mathf.Clamp01((Time.time - launchedAt) / pop) : 1f;
            Vector2 stretch = Vector2.Lerp(stats.SlipperLaunchStretch, Vector2.one, 1f - (1f - t) * (1f - t));
            sprite.localScale = new Vector3(spriteScale.x * stretch.x, spriteScale.y * stretch.y, spriteScale.z);
        }

        private void FixedUpdate()
        {
            if (!flying || boss == null)
                return;
            ParentsStats stats = boss.Stats;
            float step = stats.SlipperSpeed * Time.fixedDeltaTime;
            Vector2 from = body.position;
            Vector2 next = from + Vector2.right * (direction * step);

            // Walls stop it; one-way platforms do not.
            Collider2D ahead = Physics2D.Raycast(from, Vector2.right * direction, step, boss.GroundLayers).collider;
            bool wall = ahead != null && !ahead.usedByEffector;
            bool outside = boss.Arena != null && (next.x < boss.Arena.bounds.min.x || next.x > boss.Arena.bounds.max.x);
            if (wall || outside)
            {
                PutAway();
                return;
            }

            body.MovePosition(next);
        }

        private void SetHurting(bool on)
        {
            foreach (Collider2D own in colliders)
                if (own != null)
                    own.enabled = on;
        }
    }
}
