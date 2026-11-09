using UnityEngine;

namespace Enemies
{
    // Moves an enemy that flies: no gravity, steers in 2D with a little inertia, gets knocked back in 2D and
    // checks for walls and ground in any direction. Shares facing and knockback timing with EnemyMotor.
    public class FlyingMotor : EnemyMotor
    {
        [Tooltip("How fast it changes velocity, in units/second². Lower = floatier.")]
        [SerializeField, Min(0.1f)] private float acceleration = 25f;

        private readonly RaycastHit2D[] hits = new RaycastHit2D[4];
        private ContactFilter2D groundFilter;

        protected override void Awake()
        {
            base.Awake();
            Body.gravityScale = 0f;
            groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayers, useTriggers = false };
        }

        // Steers towards target, slowing down within slowRadius so it settles instead of overshooting.
        public void FlyTowards(Vector2 target, float speed, float slowRadius = 1f)
        {
            Vector2 offset = target - Body.position;
            float distance = offset.magnitude;
            Vector2 desired = distance > 0.01f ? offset / distance * speed * Mathf.Clamp01(distance / slowRadius) : Vector2.zero;
            Steer(desired);
        }

        // Steers towards a velocity (smoothly).
        public void Steer(Vector2 velocity)
        {
            if (Mathf.Abs(velocity.x) > 0.05f)
                Face(velocity.x > 0f ? 1 : -1);
            if (IsKnockedBack)
                return;
            Body.linearVelocity = Vector2.MoveTowards(Body.linearVelocity, velocity, acceleration * Time.fixedDeltaTime);
        }

        // Sets the velocity straight away, no inertia (the dive).
        public void Fly(Vector2 velocity)
        {
            if (Mathf.Abs(velocity.x) > 0.05f)
                Face(velocity.x > 0f ? 1 : -1);
            if (!IsKnockedBack)
                Body.linearVelocity = velocity;
        }

        public override void Stop()
        {
            if (!IsKnockedBack)
                Body.linearVelocity = Vector2.zero;
        }

        // Pushed away horizontally and a little upwards, so a hit flyer bobs back instead of sliding.
        public override void Knockback(int dir, float force, float time)
        {
            knockbackUntil = Time.time + time;
            Body.linearVelocity = new Vector2(dir * force, force * 0.3f);
        }

        // True if the body would hit ground or a wall moving distance along direction.
        public bool Blocked(Vector2 direction, float distance)
        {
            return Body.Cast(direction.normalized, groundFilter, hits, distance) > 0;
        }
    }
}
