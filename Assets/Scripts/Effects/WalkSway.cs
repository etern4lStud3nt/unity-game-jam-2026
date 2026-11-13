using UnityEngine;

// A walk without walk frames: while the body moves sideways the sprite rocks from side to side and bobs a little,
// one rock per step; standing still it settles back upright. Put it on the sprite's object, with the sprite's
// pivot at its feet so it rocks on them.
public class WalkSway : MonoBehaviour
{
    [Tooltip("The body whose sideways movement drives the walk. Empty = the Rigidbody2D in a parent.")]
    [SerializeField] private Rigidbody2D body;
    [Tooltip("How far it leans to each side, in degrees.")]
    [SerializeField, Min(0f)] private float angle = 5f;
    [Tooltip("How high it bobs at each step, in units.")]
    [SerializeField, Min(0f)] private float bob = 0.09f;
    [Tooltip("Steps per unit walked.")]
    [SerializeField, Min(0.01f)] private float stepsPerUnit = 1.2f;
    [Tooltip("Slower than this (units per second) counts as standing.")]
    [SerializeField, Min(0f)] private float minSpeed = 0.2f;
    [Tooltip("How fast it settles upright when it stops, per second.")]
    [SerializeField, Min(0.1f)] private float settleSpeed = 8f;

    private Vector3 basePosition;
    private float lastX;
    private float phase;
    private float weight;

    private void Awake()
    {
        if (body == null)
            body = GetComponentInParent<Rigidbody2D>();
        basePosition = transform.localPosition;
    }

    private void OnEnable() => lastX = Position();

    private void LateUpdate()
    {
        float x = Position();
        float moved = Mathf.Abs(x - lastX);
        lastX = x;
        bool walking = Time.deltaTime > 0f && moved / Time.deltaTime > minSpeed;

        // Half a sine per step: lean left on one step, right on the next.
        phase += moved * stepsPerUnit * Mathf.PI;
        float before = weight;
        weight = Mathf.MoveTowards(weight, walking ? 1f : 0f, settleSpeed * Time.deltaTime);
        if (weight <= 0f)
        {
            phase = 0f;
            // Already upright and in place: nothing to write.
            if (before <= 0f)
                return;
        }

        float wave = Mathf.Sin(phase);
        transform.localRotation = Quaternion.Euler(0f, 0f, wave * angle * weight);
        transform.localPosition = basePosition + Vector3.up * (Mathf.Abs(wave) * bob * weight);
    }

    private float Position() => body != null ? body.position.x : transform.position.x;
}
