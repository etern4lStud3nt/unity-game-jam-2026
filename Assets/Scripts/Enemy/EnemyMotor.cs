using UnityEngine;

namespace Enemies
{
    // Moves a ground enemy: walks, turns, gets knocked back, and tells the states whether a wall or a ledge
    // is ahead. States only ask the motor to move; they never touch the Rigidbody2D themselves.
    // FlyingMotor derives from it for enemies without gravity.
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class EnemyMotor : MonoBehaviour
    {
        [Tooltip("Layers that count as ground and walls (the tilemap's Ground layer).")]
        [SerializeField] protected LayerMask groundLayers;
        [Tooltip("Child flipped to face the movement direction (sprite, hurtbox).")]
        [SerializeField] private Transform visual;
        [Tooltip("Faces left when the level starts (a guard watching the left side, a walker starting leftwards).")]
        [SerializeField] private bool startFacingLeft;
        [Tooltip("How far ahead of the body a wall still blocks.")]
        [SerializeField, Min(0f)] private float wallProbe = 0.1f;
        [Tooltip("How deep the ground may drop ahead before it counts as a ledge.")]
        [SerializeField, Min(0f)] private float ledgeDepth = 0.6f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        protected float knockbackUntil;

        public Rigidbody2D Body => body;
        public int Facing { get; private set; } = 1;
        public bool IsKnockedBack => Time.time < knockbackUntil;

        private void Reset()
        {
            groundLayers = LayerMask.GetMask("Ground");
            visual = transform.Find("Visual");
        }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            Facing = startFacingLeft ? -1 : 1;
            ApplyFacing();
        }

        // Walks in dir (-1 or 1) at speed. Ignored while knocked back so the hit can push the enemy.
        public void Move(int dir, float speed)
        {
            Face(dir);
            if (IsKnockedBack)
                return;
            body.linearVelocityX = dir * speed;
        }

        public virtual void Stop()
        {
            if (!IsKnockedBack)
                body.linearVelocityX = 0f;
        }

        public void Face(int dir)
        {
            if (dir == 0 || dir == Facing)
                return;
            Facing = dir > 0 ? 1 : -1;
            ApplyFacing();
        }

        public void TurnAround() => Face(-Facing);

        public virtual void Knockback(int dir, float force, float time)
        {
            knockbackUntil = Time.time + time;
            body.linearVelocityX = dir * force;
        }

        public bool IsGrounded
        {
            get
            {
                Bounds b = bodyCollider.bounds;
                var origin = new Vector2(b.center.x, b.min.y + 0.05f);
                return Physics2D.Raycast(origin, Vector2.down, 0.15f, groundLayers).collider != null;
            }
        }

        public bool WallAhead
        {
            get
            {
                Bounds b = bodyCollider.bounds;
                return Physics2D.Raycast(b.center, Vector2.right * Facing, b.extents.x + wallProbe, groundLayers).collider != null;
            }
        }

        // True when the ground ends just ahead. speed widens the look-ahead so a fast lunge stops in time.
        public bool LedgeAhead(float speed = 0f)
        {
            if (!IsGrounded)
                return false;
            Bounds b = bodyCollider.bounds;
            float lookAhead = Mathf.Max(0.05f, speed * Time.fixedDeltaTime * 2f);
            var origin = new Vector2(b.center.x + Facing * (b.extents.x + lookAhead), b.min.y + 0.1f);
            return Physics2D.Raycast(origin, Vector2.down, ledgeDepth + 0.1f, groundLayers).collider == null;
        }

        // Wall or ledge ahead: the usual "can't keep walking this way" check.
        public bool Blocked(float speed = 0f) => WallAhead || LedgeAhead(speed);

        private void ApplyFacing()
        {
            if (visual == null)
                return;
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * Facing;
            visual.localScale = scale;
        }

        private void OnDrawGizmosSelected()
        {
            var col = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
            if (col == null)
                return;
            Bounds b = col.bounds;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(b.center, b.center + Vector3.right * Facing * (b.extents.x + wallProbe));
            var ledge = new Vector3(b.center.x + Facing * (b.extents.x + 0.05f), b.min.y + 0.1f);
            Gizmos.DrawLine(ledge, ledge + Vector3.down * (ledgeDepth + 0.1f));
        }
    }
}
