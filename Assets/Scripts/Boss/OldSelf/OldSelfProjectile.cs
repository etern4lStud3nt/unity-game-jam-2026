using UnityEngine;

namespace Bosses
{
    // What the Old Self throws (a playing card, or anything: swap the sprite). Flies out Throw Distance towards
    // where it was aimed, slowing down at the far end (or turning back early at a wall), then flies back to the
    // boss's hand, spinning all the way. Hurts on touch (a trigger with DamageOnContact on this object).
    // Keep it outside the boss's object (a sibling under the prefab root). Its size in the prefab times Throw Scale.
    [RequireComponent(typeof(Rigidbody2D))]
    public class OldSelfProjectile : MonoBehaviour
    {
        private const float CatchDistance = 0.5f;

        private OldSelfBoss boss;
        private Rigidbody2D body;
        private Vector3 baseScale;
        private Vector2 origin;
        private Vector2 direction;
        private float distance;
        private float outTimer;
        private bool returning;

        // Out or on the way back; false once caught (or put away).
        public bool IsFlying => gameObject.activeSelf;

        // Called once by the boss in its Awake. Hidden until thrown.
        public void Init(OldSelfBoss owner)
        {
            boss = owner;
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            baseScale = transform.localScale;
            gameObject.SetActive(false);
        }

        public void Launch(Vector2 from, Vector2 aim)
        {
            OldSelfStats stats = boss.Stats;
            origin = from;
            direction = aim.sqrMagnitude > 0.001f ? aim.normalized : Vector2.right * boss.Motor.Facing;
            outTimer = 0f;
            returning = false;

            // Turns back early if a wall is in the way.
            RaycastHit2D wall = Physics2D.Raycast(from, direction, stats.ThrowDistance, boss.GroundLayers);
            distance = wall.collider != null ? Mathf.Max(0f, wall.distance - 0.3f) : stats.ThrowDistance;

            transform.localScale = baseScale * stats.ThrowScale;
            transform.position = from;
            body.position = from;
            gameObject.SetActive(true);
        }

        public void PutAway()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (boss == null)
                return;
            OldSelfStats stats = boss.Stats;
            float dt = Time.fixedDeltaTime;
            body.MoveRotation(body.rotation + stats.ThrowSpin * dt);

            if (!returning)
            {
                outTimer += dt;
                float k = Mathf.Clamp01(outTimer / stats.ThrowOutTime);
                float eased = 1f - (1f - k) * (1f - k);
                body.MovePosition(origin + direction * (distance * eased));
                if (k >= 1f)
                    returning = true;
                return;
            }

            Vector2 hand = boss.HandPoint;
            Vector2 next = Vector2.MoveTowards(body.position, hand, stats.ThrowReturnSpeed * dt);
            body.MovePosition(next);
            if (Vector2.Distance(next, hand) <= CatchDistance)
                gameObject.SetActive(false);
        }
    }
}
