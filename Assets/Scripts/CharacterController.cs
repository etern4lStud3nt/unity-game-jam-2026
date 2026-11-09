using System.Collections;
using UnityEngine;
namespace Assets.Scripts
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckLength = 1f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float dropThroughTime = 0.3f;

        [Header("Abilities (metroidvania unlocks)")]
        [SerializeField] private bool doubleJumpUnlocked = true;
        [SerializeField] private bool wallJumpUnlocked = true;
        [SerializeField] private bool dashUnlocked = true;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 28f;
        [SerializeField] private float dashTime = 0.25f;
        [SerializeField] private float dashCooldown = 0.4f;
        [Tooltip("Short hold in the air before the burst, so the dash reads as a snap instead of a slide.")]
        [SerializeField] private float dashFreezeTime = 0.04f;
        [Tooltip("Speed multiplier over the dash (0 = start, 1 = end). Starts hard, then eases out.")]
        [SerializeField] private AnimationCurve dashSpeedCurve = new AnimationCurve(
            new Keyframe(0f, 1.9f, 0f, -1.5f),
            new Keyframe(0.55f, 0.95f, -2.2f, -2.2f),
            new Keyframe(1f, 0.3f, -0.6f, 0f));

        // Direction (+1 right / -1 left). Visual effects such as afterimages listen to these.
        public event System.Action<float> DashStarted;
        public event System.Action DashEnded;

        public bool DoubleJumpUnlocked { get => doubleJumpUnlocked; set => doubleJumpUnlocked = value; }
        public bool WallJumpUnlocked { get => wallJumpUnlocked; set => wallJumpUnlocked = value; }
        public bool DashUnlocked { get => dashUnlocked; set => dashUnlocked = value; }
        public bool IsDashing { get; private set; }

        private Rigidbody2D rigidbody;
        private Collider2D bodyCollider;
        private bool canAirJump = false;
        private bool canDash = true;
        private float nextDashTime;
        private float facing = 1f;
        private float savedGravity;

        public void Awake()
        {
            rigidbody = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
        }

        // Drops through the one-way platform the character stands on. Returns false when there is none.
        public bool TryDropThrough()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckLength, groundLayer);
            if (hit.collider == null || !hit.collider.TryGetComponent(out PlatformEffector2D effector) || !effector.useOneWay)
                return false;

            StartCoroutine(IgnorePlatformFor(hit.collider));
            return true;
        }

        private IEnumerator IgnorePlatformFor(Collider2D platform)
        {
            Physics2D.IgnoreCollision(bodyCollider, platform, true);
            yield return new WaitForSeconds(dropThroughTime);
            if (platform != null)
                Physics2D.IgnoreCollision(bodyCollider, platform, false);
        }

        public void Walk(Vector2 direction)
        {
            if (IsDashing)
                return;

            direction.Normalize();
            if (Mathf.Abs(direction.x) > 0.1f)
                facing = Mathf.Sign(direction.x);
            rigidbody.linearVelocityX = direction.x * moveSpeed;
        }

        // Horizontal dash in the input direction (or the facing direction when there is no input).
        // One dash in the air; it comes back on touching the ground or a wall.
        public bool Dash(float inputX)
        {
            if (!dashUnlocked || IsDashing || !canDash || Time.time < nextDashTime)
                return false;

            float direction = Mathf.Abs(inputX) > 0.1f ? Mathf.Sign(inputX) : facing;
            facing = direction;
            StartCoroutine(DashRoutine(direction));
            return true;
        }

        private IEnumerator DashRoutine(float direction)
        {
            IsDashing = true;
            canDash = false;
            savedGravity = rigidbody.gravityScale;
            rigidbody.gravityScale = 0f;

            // Freeze for a moment, then burst.
            float freezeEnd = Time.time + dashFreezeTime;
            while (Time.time < freezeEnd)
            {
                rigidbody.linearVelocity = Vector2.zero;
                yield return new WaitForFixedUpdate();
            }

            DashStarted?.Invoke(direction);
            float start = Time.time;
            while (Time.time - start < dashTime)
            {
                float t = (Time.time - start) / dashTime;
                rigidbody.linearVelocity = new Vector2(direction * dashSpeed * dashSpeedCurve.Evaluate(t), 0f);
                yield return new WaitForFixedUpdate();
            }

            EndDash();
            rigidbody.linearVelocityX = direction * moveSpeed;
        }

        private void EndDash()
        {
            if (!IsDashing)
                return;
            rigidbody.gravityScale = savedGravity;
            IsDashing = false;
            nextDashTime = Time.time + dashCooldown;
            DashEnded?.Invoke();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            EndDash();
        }

        public void Jump()
        {
            if (IsGrounded())
            {
                rigidbody.linearVelocityY = 0f;
                rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
            else
            {
                // Air jump: double jump anywhere, or a wall jump while touching a wall.
                bool allowed = doubleJumpUnlocked || (wallJumpUnlocked && IsTouchingWall());
                if (canAirJump && allowed)
                {
                    rigidbody.linearVelocityY = 0f;
                    rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                    canAirJump = false;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!canDash && !IsDashing && (IsGrounded() || (wallJumpUnlocked && IsTouchingWall())))
                canDash = true;

            if (!canAirJump)
            {
                if (IsGrounded() || (wallJumpUnlocked && IsTouchingWall()))
                {
                    canAirJump = true;
                }
            }
        }

        public bool IsGrounded()
        {
            RaycastHit2D bottomRaycast = Physics2D.Raycast(transform.position, Vector2.down, groundCheckLength, groundLayer);
            return bottomRaycast.collider != null;
        }

        public bool IsTouchingWall()
        {
            RaycastHit2D leftRaycast = Physics2D.Raycast(transform.position, Vector2.left, groundCheckLength, groundLayer);
            RaycastHit2D rightRaycast = Physics2D.Raycast(transform.position, Vector2.right, groundCheckLength, groundLayer);

            return leftRaycast.collider != null || rightRaycast.collider != null;
        }

        private void OnDrawGizmos()
        {
            // Draw a small line to visualize the ground check
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckLength);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.left * groundCheckLength);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * groundCheckLength);
        }
    }
}
