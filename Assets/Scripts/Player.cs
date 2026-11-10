using CharacterController = Assets.Scripts.CharacterController;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CharacterController characterController;
    [Tooltip("Empty = the PlayerAttack on the character.")]
    [SerializeField] private PlayerAttack playerAttack;

    private void Start()
    {
        if (playerAttack == null)
            playerAttack = characterController.GetComponent<PlayerAttack>();

        inputManager.Jump += HandleJump;
        inputManager.JumpReleased += HandleJumpReleased;
        inputManager.Attack += HandleAttack;
        inputManager.Interact += HandleInteract;
        inputManager.Dash += HandleDash;
    }

    private void FixedUpdate()
    {
        characterController.Walk(inputManager.MoveInput);
    }

    private void HandleJump()
    {
        // Down + jump drops through a one-way platform instead of jumping.
        if (inputManager.MoveInput.y < -0.5f && characterController.TryDropThrough())
            return;

        characterController.Jump();
    }

    private void HandleJumpReleased()
    {
        characterController.ReleaseJump();
    }

    private void HandleDash()
    {
        characterController.Dash(inputManager.MoveInput.x);
    }

    private void HandleAttack()
    {
        if (playerAttack != null)
            playerAttack.TryAttack(inputManager.MoveInput);
    }

    private void HandleInteract()
    {
        Debug.Log("Interact");
    }

    private void OnDestroy()
    {
        inputManager.Jump -= HandleJump;
        inputManager.JumpReleased -= HandleJumpReleased;
        inputManager.Attack -= HandleAttack;
        inputManager.Interact -= HandleInteract;
        inputManager.Dash -= HandleDash;
    }
}
