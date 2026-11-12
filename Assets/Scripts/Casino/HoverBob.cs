using UnityEngine;

// Bobs the object up and down around where it was placed (a "come here" arrow above the roulette table).
// Steps in whole pixels so pixel art stays crisp.
public class HoverBob : MonoBehaviour
{
    [Tooltip("How far it moves up and down from its spot, in pixels of the sprite.")]
    [SerializeField, Min(0f)] private float height = 2f;
    [Tooltip("Up-and-down trips per second.")]
    [SerializeField, Min(0.01f)] private float speed = 1.5f;
    [SerializeField, Min(1f)] private float pixelsPerUnit = 16f;

    private Vector3 home;

    private void Awake()
    {
        home = transform.localPosition;
    }

    private void OnEnable()
    {
        transform.localPosition = home;
    }

    private void Update()
    {
        float pixels = Mathf.Round(Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * height);
        // In the parent's space one sprite pixel is localScale / pixelsPerUnit.
        transform.localPosition = home + Vector3.up * pixels * transform.localScale.y / pixelsPerUnit;
    }
}
