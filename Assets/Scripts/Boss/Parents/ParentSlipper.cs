using UnityEngine;

namespace Bosses
{
    // One of the Mother's slippers. It first hangs where it will leave from (so the player can read the wave's gap),
    // then flies flat in a straight line until it leaves the arena or reaches a wall. Hurts on touch (a trigger with
    // DamageOnContact on this object). Pooled: hidden until the Mother needs it.
    // Keep it outside the boss's object (a sibling under the prefab root), or player hits on it would hurt the boss.
    [RequireComponent(typeof(Rigidbody2D))]
    public class ParentSlipper : MonoBehaviour
    {
        private ParentsBoss boss;
        private Rigidbody2D body;
        private Collider2D[] colliders;
        private int direction;
        private bool flying;

        public bool InUse => gameObject.activeSelf;

        // Called once by the boss in its Awake. Hidden until used.
        public void Init(ParentsBoss owner)
        {
            boss = owner;
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            colliders = GetComponentsInChildren<Collider2D>(true);
            gameObject.SetActive(false);
        }

        // Appears at point, harmless and still, until Launch.
        public void Hold(Vector2 point, int facing)
        {
            flying = false;
            direction = facing >= 0 ? 1 : -1;
            SetHurting(false);
            transform.SetPositionAndRotation(point, Quaternion.identity);
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
            body.position = point;
            body.rotation = 0f;
            gameObject.SetActive(true);
        }

        public void Launch()
        {
            if (!InUse)
                return;
            flying = true;
            SetHurting(true);
        }

        public void PutAway()
        {
            flying = false;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
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
            if (stats.SlipperSpin != 0f)
                body.MoveRotation(body.rotation - direction * stats.SlipperSpin * Time.fixedDeltaTime);
        }

        private void SetHurting(bool on)
        {
            foreach (Collider2D own in colliders)
                if (own != null)
                    own.enabled = on;
        }
    }
}
