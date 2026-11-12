using UnityEngine;

// Something in the casino the player uses with Interact while standing in its trigger (a door, the roulette table).
// The prompt object is shown while the player is in range and the thing can be used.
[RequireComponent(typeof(Collider2D))]
public abstract class CasinoInteractable : MonoBehaviour
{
    [Tooltip("Shown while the player is in range (a key icon above the door). Optional.")]
    [SerializeField] private GameObject prompt;
    [SerializeField] private string playerTag = "Player";

    // The player's body while they stand in the trigger, else null.
    protected Rigidbody2D Player { get; private set; }

    // True while a door move or a spin is running; the prompt hides and Interact is ignored.
    protected bool Busy { get; set; }

    private InputManager input;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // In Start, after InputManager's Awake has set its Instance.
    protected virtual void Start()
    {
        if (prompt != null)
            prompt.SetActive(false);
        input = InputManager.Instance;
        if (input != null)
            input.Interact += OnInteract;
    }

    protected virtual void OnDestroy()
    {
        if (input != null)
            input.Interact -= OnInteract;
    }

    protected virtual void Update()
    {
        if (prompt != null)
            prompt.SetActive(Player != null && !Busy);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D body = PlayerBody(other);
        if (body != null)
            Player = body;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (PlayerBody(other) == Player)
            Player = null;
    }

    private void OnDisable()
    {
        Player = null;
    }

    private Rigidbody2D PlayerBody(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null)
            return null;
        return other.CompareTag(playerTag) || body.CompareTag(playerTag) ? body : null;
    }

    private void OnInteract()
    {
        if (Player != null && !Busy && isActiveAndEnabled)
            Use(Player);
    }

    // Called when the player presses Interact in range.
    protected abstract void Use(Rigidbody2D player);
}
