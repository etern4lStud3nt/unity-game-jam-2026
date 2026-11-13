using UnityEngine;

// Spins a round sprite as its body moves sideways, as if it rolled along the ground (the walker chip).
// Put it on the sprite's object; the sprite's pivot should be its middle.
public class RollWhileMoving : MonoBehaviour
{
    [Tooltip("The body whose sideways movement rolls the sprite. Empty = the Rigidbody2D in a parent.")]
    [SerializeField] private Rigidbody2D body;
    [Tooltip("The rolling radius in units, about half the sprite's width.")]
    [SerializeField, Min(0.01f)] private float radius = 0.7f;

    private float lastX;
    private float angle;

    private void Awake()
    {
        if (body == null)
            body = GetComponentInParent<Rigidbody2D>();
    }

    private void OnEnable() => lastX = Position();

    private void LateUpdate()
    {
        float x = Position();
        // Moving right turns it clockwise (negative z), like a wheel.
        angle -= (x - lastX) / radius * Mathf.Rad2Deg;
        angle = Mathf.Repeat(angle, 360f);
        lastX = x;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private float Position() => body != null ? body.position.x : transform.position.x;
}
