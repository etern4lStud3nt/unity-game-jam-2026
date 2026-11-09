using UnityEngine;
using CharacterController = Assets.Scripts.CharacterController;

// Movement sounds for a character, read from its physics so the movement code doesn't need to know about audio:
// footsteps while walking on the ground, and a jump sound when the body is suddenly pushed upwards.
// Once the character has animations, footsteps are better triggered by Animation Events (see SoundPlayer).
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterAudio : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private SoundData footstep;
    [SerializeField] private SoundData jump;
    [Tooltip("Played instead of Jump when the push happens in the air. Empty = Jump.")]
    [SerializeField] private SoundData airJump;

    [Header("Footsteps")]
    [SerializeField, Min(0.05f)] private float stepInterval = 0.3f;
    [Tooltip("Horizontal speed below which the character counts as standing still.")]
    [SerializeField, Min(0f)] private float minWalkSpeed = 0.1f;

    [Header("Jump")]
    [Tooltip("An upward change in vertical speed this big within one physics step counts as a jump.")]
    [SerializeField, Min(0.1f)] private float jumpSpeedChange = 3f;

    private Rigidbody2D body;
    private float stepTimer;
    private float lastVerticalSpeed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        if (AudioManager.Instance == null)
            return;

        // Moving up fast after a sudden push is a jump; landing also raises the speed (from falling to 0) but doesn't go up.
        float verticalSpeed = body.linearVelocityY;
        if (verticalSpeed > jumpSpeedChange && verticalSpeed - lastVerticalSpeed > jumpSpeedChange)
        {
            // On the ground the body was still just before the push; in the air it was already rising or falling.
            bool inAir = airJump != null && Mathf.Abs(lastVerticalSpeed) > 0.5f;
            AudioManager.Instance.PlaySFX(inAir ? airJump : jump, transform.position);
        }
        lastVerticalSpeed = verticalSpeed;

        bool walking = Mathf.Abs(body.linearVelocityX) > minWalkSpeed && characterController.IsGrounded();
        if (!walking)
        {
            // Next step plays as soon as the character starts walking again.
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.fixedDeltaTime;
        if (stepTimer <= 0f)
        {
            AudioManager.Instance.PlaySFX(footstep, transform.position);
            stepTimer = stepInterval;
        }
    }
}
