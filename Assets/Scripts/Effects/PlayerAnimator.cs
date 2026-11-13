using UnityEngine;
using CharacterController = Assets.Scripts.CharacterController;

// Drives the player's Animator from the movement, attack and life code, so none of them knows about animation:
// Walk while moving on the ground, Attack on every swing, Lying while dead (until the respawn).
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [Tooltip("Horizontal speed above which the character counts as walking.")]
    [SerializeField, Min(0f)] private float walkSpeed = 0.1f;

    private static readonly int WalkId = Animator.StringToHash("Walk");
    private static readonly int AttackId = Animator.StringToHash("Attack");
    private static readonly int LyingId = Animator.StringToHash("Lying");

    private Animator animator;
    private Rigidbody2D body;
    private PlayerAttack attack;
    private bool dead;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        attack = GetComponent<PlayerAttack>();
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (attack != null)
            attack.Swung += OnSwung;
        EventBus.Subscribe<PlayerDied>(OnDied);
        EventBus.Subscribe<PlayerRespawned>(OnRespawned);
    }

    private void OnDisable()
    {
        if (attack != null)
            attack.Swung -= OnSwung;
        EventBus.Unsubscribe<PlayerDied>(OnDied);
        EventBus.Unsubscribe<PlayerRespawned>(OnRespawned);
    }

    private void Update()
    {
        bool walking = !dead && body != null && Mathf.Abs(body.linearVelocityX) > walkSpeed
                       && (characterController == null || characterController.IsGrounded());
        animator.SetBool(WalkId, walking);
    }

    private void OnSwung(Vector2 direction)
    {
        if (!dead)
            animator.SetTrigger(AttackId);
    }

    private void OnDied(PlayerDied evt)
    {
        dead = true;
        animator.ResetTrigger(AttackId);
        animator.SetBool(LyingId, true);
    }

    private void OnRespawned(PlayerRespawned evt)
    {
        dead = false;
        animator.SetBool(LyingId, false);
    }
}
