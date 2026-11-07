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
    }

    private void FixedUpdate()
    {
        characterController.Walk(inputManager.MoveInput);
    }

    private void HandleJump()
    {
        characterController.Jump();
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
    }
}
