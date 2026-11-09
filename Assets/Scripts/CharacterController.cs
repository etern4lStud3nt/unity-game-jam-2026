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

        public bool DoubleJumpUnlocked { get => doubleJumpUnlocked; set => doubleJumpUnlocked = value; }
        public bool WallJumpUnlocked { get => wallJumpUnlocked; set => wallJumpUnlocked = value; }

        private Rigidbody2D rigidbody;
        private Collider2D bodyCollider;
        private bool canAirJump = false;

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
            direction.Normalize();
            rigidbody.linearVelocityX = direction.x * moveSpeed;
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
