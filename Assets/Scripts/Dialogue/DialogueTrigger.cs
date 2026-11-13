using UnityEngine;

// Starts a Yarn node when the player presses Interact inside this trigger (an NPC, a sign...), or as soon as the
// player walks in. Put it on an object with a trigger collider; the prompt object is shown while the player can talk.
[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("Node of the Yarn project to run.")]
    [SerializeField] private string node = "Start";
    [Tooltip("Start as soon as the player walks in, without pressing Interact.")]
    [SerializeField] private bool startOnEnter;
    [Tooltip("Run only the first time. Later visits do nothing.")]
    [SerializeField] private bool once;
    [Tooltip("Shown while the player is in range (a key icon, \"...\" above the NPC). Optional.")]
    [SerializeField] private GameObject prompt;
    [Tooltip("Arrow above the NPC until their first conversation starts. Hidden in interaction range.")]
    [SerializeField] private GameObject marker;
    [SerializeField] private string playerTag = "Player";

    private bool playerInside;
    private bool used;
    private bool talking;
    private bool hasTalked;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // In Start, after InputManager's Awake has set its Instance.
    private void Start()
    {
        if (prompt != null)
            prompt.SetActive(false);
        UpdatePrompt();
        if (!startOnEnter && InputManager.Instance != null)
            InputManager.Instance.Interact += OnInteract;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.Interact -= OnInteract;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<DialogueStarted>(OnDialogueStarted);
        EventBus.Subscribe<DialogueEnded>(OnDialogueEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DialogueStarted>(OnDialogueStarted);
        EventBus.Unsubscribe<DialogueEnded>(OnDialogueEnded);
        SetPlayerInside(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;
        SetPlayerInside(true);
        if (startOnEnter)
            Talk();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
            SetPlayerInside(false);
    }

    private bool IsPlayer(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        return other.CompareTag(playerTag) || (body != null && body.CompareTag(playerTag));
    }

    private void SetPlayerInside(bool inside)
    {
        playerInside = inside;
        UpdatePrompt();
    }

    private void OnInteract()
    {
        if (playerInside && isActiveAndEnabled)
            Talk();
    }

    private void Talk()
    {
        if (used || !DialogueBridge.Run(node))
            return;
        used = once;
    }

    private void OnDialogueStarted(DialogueStarted evt)
    {
        talking = true;
        if (evt.Node == node)
            hasTalked = true;
        UpdatePrompt();
    }

    private void OnDialogueEnded(DialogueEnded evt)
    {
        talking = false;
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (prompt != null)
            prompt.SetActive(playerInside && !used && !startOnEnter && !talking);
        if (marker != null)
            marker.SetActive(!playerInside && !hasTalked && !used && !startOnEnter && !talking);
    }
}
