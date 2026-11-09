using CharacterController = Assets.Scripts.CharacterController;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CharacterController characterController;

    private void Start()
    {
        inputManager.Jump += HandleJump;
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

    private void HandleDash()
    {
        characterController.Dash(inputManager.MoveInput.x);
    }

    private void HandleAttack()
    {
        Debug.Log("Attack");
    }

    private void HandleInteract()
    {
        Debug.Log("Interact");
    }

    private void OnDestroy()
    {
        inputManager.Jump -= HandleJump;
        inputManager.Attack -= HandleAttack;
        inputManager.Interact -= HandleInteract;
        inputManager.Dash -= HandleDash;
    }
}
