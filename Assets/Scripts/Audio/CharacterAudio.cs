using UnityEngine;
using CharacterController = Assets.Scripts.CharacterController;
using JumpKind = Assets.Scripts.JumpKind;

// Movement sounds for a character. Jumps, pogo bounces, dashes and drops through platforms come from the
// CharacterController's events; footsteps and landings are read from its physics. The movement code doesn't know
// about audio. Once the character has animations, footsteps are better triggered by Animation Events (see SoundPlayer).
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterAudio : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private SoundData footstep;
    [SerializeField] private SoundData jump;
    [Tooltip("The air jump (double jump). Empty = Jump.")]
    [SerializeField] private SoundData airJump;
    [Tooltip("Jumping off a wall. Empty = Jump.")]
    [SerializeField] private SoundData wallJump;
    [Tooltip("Bounced up by a downward attack (pogo).")]
    [SerializeField] private SoundData pogo;
    [SerializeField] private SoundData land;
    [SerializeField] private SoundData dash;
    [Tooltip("Dropping down through a one-way platform.")]
    [SerializeField] private SoundData dropThrough;

    [Header("Footsteps")]
    [SerializeField, Min(0.05f)] private float stepInterval = 0.3f;
    [Tooltip("Horizontal speed below which the character counts as standing still.")]
    [SerializeField, Min(0f)] private float minWalkSpeed = 0.1f;

    [Header("Landing")]
    [Tooltip("Falling speed needed for the landing sound, so stepping off a small ledge stays quiet.")]
    [SerializeField, Min(0f)] private float minLandSpeed = 6f;

    private Rigidbody2D body;
    private float stepTimer;
    private bool wasGrounded = true;
    private float lastVerticalSpeed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (characterController == null)
            return;
        characterController.Jumped += OnJumped;
        characterController.Bounced += OnBounced;
        characterController.DashStarted += OnDashStarted;
        characterController.DroppedThrough += OnDroppedThrough;
    }

    private void OnDisable()
    {
        if (characterController == null)
            return;
        characterController.Jumped -= OnJumped;
        characterController.Bounced -= OnBounced;
        characterController.DashStarted -= OnDashStarted;
        characterController.DroppedThrough -= OnDroppedThrough;
    }

    private void OnJumped(JumpKind kind)
    {
        switch (kind)
        {
            case JumpKind.Air: Play(airJump != null ? airJump : jump); break;
            case JumpKind.Wall: Play(wallJump != null ? wallJump : jump); break;
            default: Play(jump); break;
        }
    }

    private void OnBounced() => Play(pogo);

    private void OnDashStarted(float direction) => Play(dash);

    private void OnDroppedThrough() => Play(dropThrough);

    private void FixedUpdate()
    {
        bool grounded = characterController.IsGrounded();

        // Touching down after a real fall (falling speed is from the step before the ground stopped it).
        if (grounded && !wasGrounded && lastVerticalSpeed < -minLandSpeed)
            Play(land);
        wasGrounded = grounded;
        lastVerticalSpeed = body.linearVelocityY;

        bool walking = Mathf.Abs(body.linearVelocityX) > minWalkSpeed && grounded;
        if (!walking)
        {
            // Next step plays as soon as the character starts walking again.
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.fixedDeltaTime;
        if (stepTimer <= 0f)
        {
            Play(footstep);
            stepTimer = stepInterval;
        }
    }

    private void Play(SoundData sound)
    {
        if (sound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(sound, transform.position);
    }
}
