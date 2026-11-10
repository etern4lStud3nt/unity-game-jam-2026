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

        [Header("Jump feel")]
        [Tooltip("Upward speed is multiplied by this when the jump button is released early (short hop). 1 = no effect.")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [Tooltip("Seconds after walking off a ledge during which a ground jump still works.")]
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [Tooltip("Seconds a jump press is remembered before landing, so an early press still jumps.")]
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;
        [Tooltip("Gravity multiplier while falling. Above 1 makes jumps feel less floaty.")]
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 1.25f;
        [Tooltip("Fastest the character can fall, in units per second.")]
        [SerializeField, Min(1f)] private float maxFallSpeed = 28f;

        [Header("Steps")]
        [Tooltip("Ledges up to this height (units) are walked up without jumping. 0 = off.")]
        [SerializeField, Min(0f)] private float stepHeight = 0.55f;

        [Header("Wall (needs Wall Jump)")]
        [Tooltip("Fastest fall while pressing into a wall in the air.")]
        [SerializeField, Min(0f)] private float wallSlideSpeed = 4f;
        [Tooltip("Sideways speed a wall jump kicks the character away from the wall.")]
        [SerializeField, Min(0f)] private float wallJumpPush = 7f;
        [Tooltip("Seconds after a wall jump during which walking input is ignored, so the kick is not cancelled at once.")]
        [SerializeField, Min(0f)] private float wallJumpControlLock = 0.12f;

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
        // +1 facing right, -1 facing left.
        public float Facing => facing;

        private Rigidbody2D rigidbody;
        private Collider2D bodyCollider;
        private bool canAirJump = false;
        private bool canDash = true;
        private float nextDashTime;
        private float facing = 1f;
        private float savedGravity;
        private float recoilUntil;
        private float moveInputX;
        private float lastGroundedTime = float.NegativeInfinity;
        private float jumpBufferedUntil = float.NegativeInfinity;
        private bool jumpHeld;
        private bool jumpCuttable;
        private bool wallSliding;

        // True while pressing into a wall and sliding down it.
        public bool IsWallSliding => wallSliding;

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
            if (IsDashing || Time.time < recoilUntil)
                return;

            moveInputX = direction.x;
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

        // Pushes the character sideways for a moment (attack recoil); walking does not override it meanwhile.
        public void Recoil(float velocityX, float time)
        {
            if (IsDashing)
                return;
            recoilUntil = Time.time + time;
            rigidbody.linearVelocityX = velocityX;
        }

        // Bounces upwards (pogo off an enemy or spikes) and gives back the air jump and the dash.
        public void Bounce(float upVelocity)
        {
            if (IsDashing)
                return;
            rigidbody.linearVelocityY = upVelocity;
            jumpCuttable = false;
            canAirJump = true;
            canDash = true;
        }

        // Jump button pressed: ground jump (with coyote time), wall jump, or double jump. If none is possible
        // right now, the press is buffered and fires as soon as one is (landing, touching a wall).
        public void Jump()
        {
            jumpHeld = true;
            jumpBufferedUntil = Time.time + jumpBufferTime;
            if (TryJump())
                jumpBufferedUntil = float.NegativeInfinity;
        }

        // Jump button released: cuts a rising jump short, for a small hop.
        public void ReleaseJump()
        {
            jumpHeld = false;
            if (jumpCuttable && rigidbody.linearVelocityY > 0f && !IsDashing)
                rigidbody.linearVelocityY *= jumpCutMultiplier;
            jumpCuttable = false;
        }

        private bool TryJump()
        {
            if (IsDashing)
                return false;

            if (IsGrounded() || Time.time - lastGroundedTime <= coyoteTime)
            {
                lastGroundedTime = float.NegativeInfinity; // coyote time is used up
                Launch(0f);
                return true;
            }

            int wall = WallSide();
            if (wallJumpUnlocked && wall != 0)
            {
                // Kick away from the wall; the short control lock keeps walking input from cancelling the kick.
                Launch(-wall * wallJumpPush);
                recoilUntil = Time.time + wallJumpControlLock;
                facing = -wall;
                return true;
            }

            if (doubleJumpUnlocked && canAirJump)
            {
                Launch(0f);
                canAirJump = false;
                return true;
            }
            return false;
        }

        private void Launch(float sideways)
        {
            float speed = jumpForce / rigidbody.mass;
            // A buffered jump whose button was already let go is a short hop.
            if (!jumpHeld)
                speed *= jumpCutMultiplier;
            rigidbody.linearVelocityY = speed;
            if (sideways != 0f)
                rigidbody.linearVelocityX = sideways;
            jumpCuttable = jumpHeld;
        }

        private void FixedUpdate()
        {
            bool grounded = IsGrounded();
            int wall = WallSide();
            if (grounded)
                lastGroundedTime = Time.time;

            if (!canDash && !IsDashing && (grounded || (wallJumpUnlocked && wall != 0)))
                canDash = true;
            if (!canAirJump && (grounded || (wallJumpUnlocked && wall != 0)))
                canAirJump = true;

            // Buffered jump: pressed a moment before it became possible.
            if (Time.time <= jumpBufferedUntil && TryJump())
                jumpBufferedUntil = float.NegativeInfinity;

            if (IsDashing)
            {
                wallSliding = false;
                return;
            }

            if (grounded)
                TryStepUp();

            float vy = rigidbody.linearVelocityY;
            wallSliding = wallJumpUnlocked && !grounded && wall != 0 && moveInputX * wall > 0.1f && vy < 0f;
            if (wallSliding)
            {
                vy = Mathf.Max(vy, -wallSlideSpeed);
            }
            else if (vy < 0f)
            {
                // Heavier fall than rise, capped at a top speed.
                vy += Physics2D.gravity.y * rigidbody.gravityScale * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
                vy = Mathf.Max(vy, -maxFallSpeed);
            }
            rigidbody.linearVelocityY = vy;
        }

        public bool IsGrounded()
        {
            RaycastHit2D bottomRaycast = Physics2D.Raycast(transform.position, Vector2.down, groundCheckLength, groundLayer);
            return bottomRaycast.collider != null;
        }

        // Walking into a ledge no higher than stepHeight lifts the character onto it, so small steps and the
        // seams between tiles never stop the walk.
        private void TryStepUp()
        {
            if (stepHeight <= 0f || Mathf.Abs(moveInputX) < 0.1f || Time.time < recoilUntil)
                return;

            float dir = Mathf.Sign(moveInputX);
            Bounds b = bodyCollider.bounds;
            float reach = b.extents.x + 0.08f;
            float feet = b.min.y;

            // Something blocks at foot height...
            RaycastHit2D low = Physics2D.Raycast(new Vector2(b.center.x, feet + 0.05f), Vector2.right * dir, reach, groundLayer);
            if (low.collider == null)
                return;
            // ...but not just above the step height.
            if (Physics2D.Raycast(new Vector2(b.center.x, feet + stepHeight + 0.05f), Vector2.right * dir, reach, groundLayer).collider != null)
                return;

            // Find the top of the step just past the wall and climb onto it.
            var probe = new Vector2(low.point.x + dir * 0.05f, feet + stepHeight + 0.05f);
            RaycastHit2D top = Physics2D.Raycast(probe, Vector2.down, stepHeight + 0.05f, groundLayer);
            if (top.collider == null)
                return;
            float rise = top.point.y - feet;
            if (rise <= 0.01f || rise > stepHeight)
                return;

            rigidbody.position += new Vector2(dir * 0.05f, rise + 0.02f);
            rigidbody.linearVelocity = new Vector2(dir * moveSpeed, Mathf.Max(0f, rigidbody.linearVelocityY));
        }

        // -1 = wall on the left, +1 = wall on the right, 0 = none.
        public int WallSide()
        {
            if (Physics2D.Raycast(transform.position, Vector2.right, groundCheckLength, groundLayer).collider != null)
                return 1;
            if (Physics2D.Raycast(transform.position, Vector2.left, groundCheckLength, groundLayer).collider != null)
                return -1;
            return 0;
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
