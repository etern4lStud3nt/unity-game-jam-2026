using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using CharacterController = Assets.Scripts.CharacterController;

// The player's nail swing, Hollow Knight style: sideways by default, up while holding up, down while holding down
// in the air. Anything with a Health in front of the swing takes damage once per swing. A landed hit freezes the
// game for a moment (hitstop), shakes the camera and pushes the player back; a downward hit on an enemy or on
// spikes (or a PogoTarget) bounces the player up instead (pogo) and gives back the air jump and dash.
// Put it on the character next to CharacterController; Player calls TryAttack on the Attack input.
public class PlayerAttack : MonoBehaviour
{
    [Header("Hit")]
    [SerializeField, Min(1)] private int damage = 1;
    [Tooltip("Seconds between swings.")]
    [SerializeField, Min(0f)] private float cooldown = 0.35f;
    [Tooltip("Seconds the swing keeps hitting after it starts.")]
    [SerializeField, Min(0.01f)] private float activeTime = 0.1f;
    [SerializeField] private LayerMask targetLayers = ~0;

    [Header("Hit areas (units, relative to the player, facing right; drag them in the Scene view)")]
    [SerializeField] private Vector2 sideSize = new Vector2(1.6f, 1.1f);
    [SerializeField] private Vector2 sideOffset = new Vector2(1.1f, 0f);
    [SerializeField] private Vector2 upSize = new Vector2(1.3f, 1.6f);
    [SerializeField] private Vector2 upOffset = new Vector2(0f, 1.2f);
    [SerializeField] private Vector2 downSize = new Vector2(1.3f, 1.6f);
    [SerializeField] private Vector2 downOffset = new Vector2(0f, -1.2f);

    [Header("Feel")]
    [Tooltip("Real seconds the game freezes when a swing lands.")]
    [SerializeField, Min(0f)] private float hitStop = 0.05f;
    [Tooltip("Speed the player is pushed back after a sideways hit.")]
    [SerializeField, Min(0f)] private float recoilSpeed = 6f;
    [SerializeField, Min(0f)] private float recoilTime = 0.1f;
    [Tooltip("Upward speed of the bounce after a downward hit.")]
    [SerializeField, Min(0f)] private float pogoSpeed = 17f;
    [SerializeField, Min(0f)] private float shakeForce = 0.15f;

    [Header("Look")]
    [Tooltip("The slash, frame by frame, drawn swinging to the right. Automatically fitted to the swing's hit " +
             "area and played once per swing. Empty = the placeholder sprite below.")]
    [SerializeField] private Sprite[] slashFrames;
    [Tooltip("Seconds each slash frame shows.")]
    [SerializeField, Min(0.005f)] private float slashFrameTime = 0.035f;
    [Tooltip("Placeholder: a sprite stretched over the hit area during the swing, used when there are no frames.")]
    [SerializeField] private Sprite slashSprite;
    [SerializeField] private Color slashColor = new Color(1f, 1f, 1f, 0.5f);

    // Direction of each swing (sound, animation), and every object a swing damaged.
    public event Action<Vector2> Swung;
    public event Action<Health> HitLanded;

    private static bool hitStopRunning;

    private readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
    private CharacterController controller;
    private Rigidbody2D body;
    private Health ownHealth;
    private CinemachineImpulseSource impulse;
    private SpriteRenderer slash;
    private Vector2 direction = Vector2.right;
    private float nextSwing;
    private float activeUntil;
    private float swingStart;
    private bool landed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => hitStopRunning = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        body = GetComponent<Rigidbody2D>();
        ownHealth = GetComponent<Health>();
        impulse = GetComponent<CinemachineImpulseSource>();

        if (HasFrames || slashSprite != null)
        {
            var go = new GameObject("Slash");
            go.transform.SetParent(transform, false);
            slash = go.AddComponent<SpriteRenderer>();
            slash.sprite = HasFrames ? slashFrames[0] : slashSprite;
            slash.color = HasFrames ? Color.white : slashColor;
            slash.sortingOrder = 10;
            slash.enabled = false;
        }
    }

    // Swings in the held direction. Returns false while on cooldown, dashing or dead.
    public bool TryAttack(Vector2 input)
    {
        if (Time.time < nextSwing || (controller != null && controller.IsDashing) || (ownHealth != null && ownHealth.IsDead))
            return false;

        bool grounded = controller == null || controller.IsGrounded();
        float facing = controller != null ? controller.Facing : 1f;
        if (input.y > 0.5f)
            direction = Vector2.up;
        else if (input.y < -0.5f && !grounded)
            direction = Vector2.down;
        else
            direction = new Vector2(facing, 0f);

        nextSwing = Time.time + cooldown;
        activeUntil = Time.time + activeTime;
        hitThisSwing.Clear();
        landed = false;

        ShowSlash();
        Swung?.Invoke(direction);
        Sweep();
        return true;
    }

    private void FixedUpdate()
    {
        if (Time.time < activeUntil)
            Sweep();
        else if (slash != null && slash.enabled && !HasFrames)
            slash.enabled = false;
    }

    // Steps the slash through its frames, then hides it.
    private void LateUpdate()
    {
        if (slash == null || !slash.enabled || !HasFrames)
            return;
        int frame = Mathf.FloorToInt((Time.time - swingStart) / slashFrameTime);
        if (frame >= slashFrames.Length)
            slash.enabled = false;
        else
            slash.sprite = slashFrames[frame];
    }

    private bool HasFrames => slashFrames != null && slashFrames.Length > 0;

    private void Sweep()
    {
        GetArea(direction, out Vector2 centre, out Vector2 size);
        bool hitSomething = false;

        foreach (Collider2D other in Physics2D.OverlapBoxAll(centre, size, 0f, targetLayers))
        {
            if (other.attachedRigidbody == body)
                continue;

            Health health = other.GetComponentInParent<Health>();
            if (health != null && health != ownHealth)
            {
                if (hitThisSwing.Add(health))
                {
                    if (health.TakeDamage(damage))
                        HitLanded?.Invoke(health);
                    hitSomething = true;
                }
            }
            // Spikes (and other pogo targets) cannot be damaged, but a downward swing can still bounce off them.
            else if (direction == Vector2.down
                     && (other.GetComponentInParent<Hazard>() != null || other.GetComponentInParent<PogoTarget>() != null))
            {
                hitSomething = true;
            }
        }

        if (hitSomething && !landed)
        {
            landed = true;
            OnLanded();
        }
    }

    private void OnLanded()
    {
        if (controller != null)
        {
            if (direction == Vector2.down)
                controller.Bounce(pogoSpeed);
            else if (direction.y == 0f)
                controller.Recoil(-direction.x * recoilSpeed, recoilTime);
        }

        if (impulse != null && shakeForce > 0f)
            impulse.GenerateImpulseWithVelocity(-direction * shakeForce);

        if (hitStop > 0f && !hitStopRunning && isActiveAndEnabled)
            StartCoroutine(HitStop());
    }

    private IEnumerator HitStop()
    {
        hitStopRunning = true;
        float previous = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStop);
        Time.timeScale = previous > 0f ? previous : 1f;
        hitStopRunning = false;
    }

    private void OnDisable()
    {
        // Never leave the game frozen if the player is disabled mid-hitstop.
        if (hitStopRunning)
        {
            Time.timeScale = 1f;
            hitStopRunning = false;
        }
        if (slash != null)
            slash.enabled = false;
    }

    private void ShowSlash()
    {
        if (slash == null)
            return;
        swingStart = Time.time;
        GetArea(direction, out Vector2 centre, out Vector2 size);
        Bounds bounds;
        if (HasFrames)
        {
            slash.sprite = slashFrames[0];
            // Use the whole animation's bounds so changing frames never changes its scale or position.
            bounds = SlashSpriteBounds.GetVisibleBounds(slashFrames[0]);
            foreach (Sprite frame in slashFrames)
                if (frame != null)
                    bounds.Encapsulate(SlashSpriteBounds.GetVisibleBounds(frame));
        }
        else
        {
            slash.sprite = slashSprite;
            bounds = SlashSpriteBounds.GetVisibleBounds(slashSprite);
        }

        Transform t = slash.transform;
        t.rotation = HasFrames
            ? Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
            : Quaternion.identity;
        slash.flipY = HasFrames && direction.x < 0f;

        // Up/down rotate the sprite 90 degrees, so width and height exchange roles.
        Vector2 localSize = HasFrames && direction.y != 0f ? new Vector2(size.y, size.x) : size;
        t.localScale = Vector3.one;
        float parentX = Vector3.Dot(t.TransformVector(Vector3.right), t.right);
        float parentY = Vector3.Dot(t.TransformVector(Vector3.up), t.up);
        t.localScale = new Vector3(
            bounds.size.x > 0f && Mathf.Abs(parentX) > 0.0001f ? localSize.x / bounds.size.x / parentX : 0f,
            bounds.size.y > 0f && Mathf.Abs(parentY) > 0.0001f ? localSize.y / bounds.size.y / parentY : 0f,
            1f);

        // Centre the art on the hit area even when the sprite uses an off-centre pivot.
        Vector3 spriteCentre = bounds.center;
        if (slash.flipY)
            spriteCentre.y = -spriteCentre.y;
        t.position = (Vector3)centre - t.TransformVector(spriteCentre);
        slash.enabled = true;
    }

    private void GetArea(Vector2 dir, out Vector2 centre, out Vector2 size)
    {
        Vector2 offset;
        if (dir == Vector2.up)
        {
            offset = upOffset;
            size = upSize;
        }
        else if (dir == Vector2.down)
        {
            offset = downOffset;
            size = downSize;
        }
        else
        {
            offset = new Vector2(sideOffset.x * Mathf.Sign(dir.x), sideOffset.y);
            size = sideSize;
        }
        centre = (Vector2)transform.position + offset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
        float facing = controller != null ? controller.Facing : 1f;
        foreach (Vector2 dir in new[] { new Vector2(facing, 0f), Vector2.up, Vector2.down })
        {
            GetArea(dir, out Vector2 centre, out Vector2 size);
            Gizmos.DrawWireCube(centre, size);
        }
    }
}
