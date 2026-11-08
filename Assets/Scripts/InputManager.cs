using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions inputActionsObj;
    private InputSystem_Actions.PlayerActions playerActions;

    public event Action Jump;
    public event Action Attack;
    public event Action Interact;
    public Vector2 MoveInput { get; private set; }


    protected override void Awake()
    {
        base.Awake();

        inputActionsObj = new InputSystem_Actions();
        playerActions = inputActionsObj.Player;
        playerActions.AddCallbacks(this);
    }

    private void OnEnable()
    {
        inputActionsObj.Enable();
    }

    void InputSystem_Actions.IPlayerActions.OnAttack(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Attack Input");
            Attack?.Invoke();
        }
    }

    void InputSystem_Actions.IPlayerActions.OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Interact Input");
            Interact?.Invoke();
        }
    }

    void InputSystem_Actions.IPlayerActions.OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Jump?.Invoke();
        }
    }

    void InputSystem_Actions.IPlayerActions.OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
        Debug.Log($"Move Input: {MoveInput}");
    }

    private void OnDisable()
    {
        inputActionsObj.Disable();
    }

    private void OnDestroy()
    {
        inputActionsObj.Dispose();
    }
}
