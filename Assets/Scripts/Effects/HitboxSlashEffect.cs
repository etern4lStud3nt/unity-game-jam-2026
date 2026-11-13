using UnityEngine;

// Goes on a melee hitbox. Its existing attack state turns both damage and the slash on/off together.
[RequireComponent(typeof(BoxCollider2D))]
public class HitboxSlashEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer slash;
    [Tooltip("Slash frames drawn to the right. Visible pixels are fitted to this object's BoxCollider2D.")]
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(0.005f)] private float frameTime = 0.035f;

    private BoxCollider2D hitbox;
    private Bounds animationBounds;
    private float startedAt;
    private bool hasFrames;

    private void Awake()
    {
        hitbox = GetComponent<BoxCollider2D>();
        if (slash == null)
            slash = GetComponentInChildren<SpriteRenderer>(true);
        if (slash == null)
        {
            var visual = new GameObject("Slash");
            visual.transform.SetParent(transform, false);
            slash = visual.AddComponent<SpriteRenderer>();
            slash.sortingOrder = 10;
        }

        if (frames != null)
            foreach (Sprite frame in frames)
            {
                if (frame == null)
                    continue;
                Bounds bounds = SlashSpriteBounds.GetVisibleBounds(frame);
                if (hasFrames)
                    animationBounds.Encapsulate(bounds);
                else
                    animationBounds = bounds;
                hasFrames = true;
            }
        slash.color = Color.white;
        slash.enabled = false;
    }

    private void OnEnable()
    {
        startedAt = Time.time;
        RenderSlash();
    }

    private void LateUpdate() => RenderSlash();

    private void RenderSlash()
    {
        if (slash == null)
            return;
        slash.enabled = hasFrames && hitbox.enabled;
        if (!slash.enabled)
            return;

        // Hold the last frame until damage stops, including longer swings and hitstop.
        int frame = Mathf.Min(Mathf.FloorToInt((Time.time - startedAt) / Mathf.Max(0.005f, frameTime)), frames.Length - 1);
        slash.sprite = frames[frame];
        slash.flipX = false;
        slash.flipY = false;

        // Work in collider space: facing, rotation and every parent scale affect both equally.
        Vector3 scale = new Vector3(
            animationBounds.size.x > 0f ? hitbox.size.x / animationBounds.size.x : 0f,
            animationBounds.size.y > 0f ? hitbox.size.y / animationBounds.size.y : 0f,
            1f);
        Transform visual = slash.transform;
        visual.localRotation = Quaternion.identity;
        visual.localScale = scale;
        visual.localPosition = (Vector3)hitbox.offset - Vector3.Scale(animationBounds.center, scale);
    }

    private void OnDisable()
    {
        if (slash != null)
            slash.enabled = false;
    }
}
