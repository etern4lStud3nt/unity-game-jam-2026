using UnityEngine;

// Makes a character with no animations feel alive by bending its sprite: stretches when it takes off or moves
// fast, squashes when it lands (harder landings squash more), leans towards where it is going, bobs while it
// walks, flinches when hit and can breathe while idle or asleep. Reads the movement from how far the object
// moved, so it works with any kind of movement (physics, MovePosition). Purely visual: it only changes Target's
// local scale, rotation and position, so put the sprite on its own child (Target) and let other code flip or
// squash the parent above it.
public class SpriteJuice : MonoBehaviour
{
    [Tooltip("The sprite child it bends. Its scale, rotation and position in the prefab are the rest pose.")]
    [SerializeField] private Transform target;
    [Tooltip("Flinches when this takes damage. Empty = the Health on this object, if any.")]
    [SerializeField] private Health health;

    [Header("Stretch with speed")]
    [Tooltip("Stretch per unit of speed along the way it moves (0.01 = 10% longer at 10 units per second).")]
    [SerializeField, Min(0f)] private float stretchPerSpeed = 0.01f;
    [SerializeField, Min(0f)] private float maxStretch = 0.3f;

    [Header("Take-off and landing")]
    [Tooltip("Rising faster than this (units per second) after standing counts as a take-off.")]
    [SerializeField, Min(0.1f)] private float takeOffSpeed = 4f;
    [SerializeField] private Vector2 takeOffScale = new Vector2(0.8f, 1.25f);
    [Tooltip("Falling faster than this and then stopping counts as a landing.")]
    [SerializeField, Min(0.1f)] private float landSpeed = 4f;
    [Tooltip("Squash of a landing at Land Speed; faster landings squash more, up to Max Land Squash.")]
    [SerializeField] private Vector2 landScale = new Vector2(1.25f, 0.8f);
    [SerializeField] private Vector2 maxLandScale = new Vector2(1.45f, 0.6f);

    [Header("Lean")]
    [Tooltip("Degrees of lean per unit of sideways speed.")]
    [SerializeField, Min(0f)] private float leanPerSpeed = 1.2f;
    [SerializeField, Min(0f)] private float maxLean = 18f;
    [SerializeField, Min(0.01f)] private float leanSmoothing = 0.08f;

    [Header("Walk bob")]
    [Tooltip("Height of the little hop of each step while walking. 0 = no bob.")]
    [SerializeField, Min(0f)] private float bobHeight;
    [Tooltip("Steps per unit walked.")]
    [SerializeField, Min(0f)] private float bobFrequency = 1.2f;

    [Header("Hit")]
    [SerializeField] private Vector2 hitScale = new Vector2(1.15f, 0.85f);

    [Header("Punches (take-off, landing, hit, attacks)")]
    [Tooltip("Seconds a punch takes to settle back.")]
    [SerializeField, Min(0.01f)] private float punchTime = 0.18f;

    [Header("Breathing (while Breathing is on)")]
    [SerializeField, Min(0f)] private float breathAmount = 0.05f;
    [SerializeField, Min(0f)] private float breathRate = 1.5f;

    private Vector3 restScale;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 lastPosition;
    private Vector2 velocity;
    private Vector2 punch = Vector2.one;
    private float punchStarted = -1f;
    private float lean;
    private float leanVelocity;
    private float walked;
    private bool wasFalling;
    private bool wasStanding = true;
    private float lastFallSpeed;

    // A slow up-and-down swell, e.g. while asleep.
    public bool Breathing { get; set; }

    private void Awake()
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(SpriteJuice)} on {name} has no Target.", this);
            enabled = false;
            return;
        }
        if (health == null)
            health = GetComponent<Health>();
        restScale = target.localScale;
        restPosition = target.localPosition;
        restRotation = target.localRotation;
        lastPosition = transform.position;
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
        if (health != null)
            health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Damaged -= OnDamaged;
        if (target != null)
        {
            target.localScale = restScale;
            target.localPosition = restPosition;
            target.localRotation = restRotation;
        }
    }

    private void OnDamaged(int amount) => Punch(hitScale);

    // Squashes or stretches the sprite by scale (x, y) and lets it spring back over Punch Time.
    public void Punch(Vector2 scale)
    {
        punch = scale;
        punchStarted = Time.time;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector2 moved = transform.position - lastPosition;
        lastPosition = transform.position;
        velocity = moved / dt;

        DetectTakeOffAndLanding();

        // Punch: eases back to 1.
        Vector2 punchNow = Vector2.one;
        if (punchStarted >= 0f)
        {
            float k = Mathf.Clamp01((Time.time - punchStarted) / punchTime);
            float ease = 1f - (1f - k) * (1f - k);
            punchNow = Vector2.Lerp(punch, Vector2.one, ease);
            if (k >= 1f)
                punchStarted = -1f;
        }

        // Stretch along the main direction of movement, keeping the area about the same.
        float speed = velocity.magnitude;
        float stretch = 1f + Mathf.Min(speed * stretchPerSpeed, maxStretch);
        Vector2 stretchNow = Mathf.Abs(velocity.y) > Mathf.Abs(velocity.x)
            ? new Vector2(1f / stretch, stretch)
            : new Vector2(stretch, 1f / stretch);

        float breath = Breathing ? 1f + Mathf.Sin(Time.time * breathRate * Mathf.PI * 2f) * breathAmount : 1f;

        Vector3 scale = restScale;
        scale.x *= punchNow.x * stretchNow.x / Mathf.Sqrt(breath);
        scale.y *= punchNow.y * stretchNow.y * breath;
        target.localScale = scale;

        // Lean towards the way it moves sideways. A flipped parent mirrors the angle, so undo that.
        float parentSign = target.parent != null && target.parent.lossyScale.x < 0f ? -1f : 1f;
        float wantedLean = Mathf.Clamp(-velocity.x * leanPerSpeed, -maxLean, maxLean);
        lean = Mathf.SmoothDamp(lean, wantedLean, ref leanVelocity, leanSmoothing, Mathf.Infinity, dt);
        target.localRotation = restRotation * Quaternion.Euler(0f, 0f, lean * parentSign);

        // Walk bob: little hops while moving sideways on the level.
        Vector3 position = restPosition;
        if (bobHeight > 0f && Mathf.Abs(velocity.x) > 0.5f && Mathf.Abs(velocity.y) < 0.5f)
        {
            walked += Mathf.Abs(moved.x);
            position.y += Mathf.Abs(Mathf.Sin(walked * bobFrequency * Mathf.PI)) * bobHeight;
        }
        target.localPosition = position;
    }

    private void DetectTakeOffAndLanding()
    {
        bool standing = Mathf.Abs(velocity.y) < 0.5f;
        if (velocity.y > takeOffSpeed && wasStanding)
            Punch(takeOffScale);

        if (wasFalling && velocity.y > -0.5f)
        {
            float hardness = Mathf.InverseLerp(landSpeed, landSpeed * 5f, Mathf.Abs(lastFallSpeed));
            Punch(Vector2.Lerp(landScale, maxLandScale, hardness));
        }

        if (velocity.y < -landSpeed)
            lastFallSpeed = velocity.y;
        wasFalling = velocity.y < -landSpeed;
        wasStanding = standing;
    }
}
