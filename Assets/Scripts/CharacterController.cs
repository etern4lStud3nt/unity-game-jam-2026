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

        private Rigidbody2D rigidbody;
        private bool canAirJump = false;

        public void Awake()
        {
            rigidbody = GetComponent<Rigidbody2D>();
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
                if (canAirJump)
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
                if (IsGrounded() || IsTouchingWall())
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
