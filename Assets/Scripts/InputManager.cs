using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions inputActionsObj;
    private InputSystem_Actions.PlayerActions playerActions;

    public event Action Jump;
    public event Action JumpReleased;
    public event Action Attack;
    public event Action Interact;
    public event Action Dash;
    // Interact, Jump or Attack pressed while gameplay is blocked (a conversation is running). Dialogue uses it to
    // advance lines.
    public event Action Confirm;

    // Zero while gameplay is blocked.
    public Vector2 MoveInput => gameplayBlocked ? Vector2.zero : moveInput;

    // While true the gameplay events (Jump, Attack, Interact, Dash) are not raised; presses go to Confirm instead.
    // Set by DialogueStarted / DialogueEnded, so the player stands still during a conversation, and by
    // BlockGameplay / UnblockGameplay (casino doors and table).
    private bool gameplayBlocked => dialogueBlocked || codeBlocks > 0;
    private bool dialogueBlocked;
    private int codeBlocks;
    private Vector2 moveInput;

    // Freezes the player's input until the matching UnblockGameplay (a door fade, the roulette spin). Calls stack.
    public void BlockGameplay() => codeBlocks++;

    public void UnblockGameplay() => codeBlocks = Mathf.Max(0, codeBlocks - 1);


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
        EventBus.Subscribe<DialogueStarted>(OnDialogueStarted);
        EventBus.Subscribe<DialogueEnded>(OnDialogueEnded);
    }

    private void OnDialogueStarted(DialogueStarted evt) => dialogueBlocked = true;

    private void OnDialogueEnded(DialogueEnded evt) => dialogueBlocked = false;

    // Raises the gameplay event, or Confirm while gameplay is blocked.
    private void Raise(Action gameplayEvent)
    {
        if (gameplayBlocked)
            Confirm?.Invoke();
        else
            gameplayEvent?.Invoke();
    }

    void InputSystem_Actions.IPlayerActions.OnAttack(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Attack Input");
            Raise(Attack);
        }
    }

    void InputSystem_Actions.IPlayerActions.OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Interact Input");
            Raise(Interact);
        }
    }

    void InputSystem_Actions.IPlayerActions.OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Raise(Jump);
        }
        else if (context.canceled && !gameplayBlocked)
        {
            JumpReleased?.Invoke();
        }
    }

    void InputSystem_Actions.IPlayerActions.OnDash(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started && !gameplayBlocked)
        {
            Dash?.Invoke();
        }
    }

    void InputSystem_Actions.IPlayerActions.OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        Debug.Log($"Move Input: {moveInput}");
    }

    private void OnDisable()
    {
        inputActionsObj.Disable();
        EventBus.Unsubscribe<DialogueStarted>(OnDialogueStarted);
        EventBus.Unsubscribe<DialogueEnded>(OnDialogueEnded);
        dialogueBlocked = false;
        codeBlocks = 0;
    }

    private void OnDestroy()
    {
        inputActionsObj.Dispose();
    }
}
