using System.Collections.Generic;
using UnityEngine;

// Leaves fading copies of an object's sprites behind it while code switches it on (Emitting, e.g. a boss's dash
// or dive), or, with When Fast, whenever it moves faster than Min Speed. Reads the speed from how far the object
// moved, so it works for any kind of movement (physics, MovePosition, plain transform). Purely visual.
// Put it on the moving object; Source is the part to copy (usually the Visual child, with all its sprites).
public class AfterimageTrail : MonoBehaviour
{
    [Tooltip("Copied, with every SpriteRenderer under it. Empty = this object.")]
    [SerializeField] private Transform source;
    [Tooltip("Also leave afterimages by itself whenever it moves faster than Min Speed. Off = only while Emitting.")]
    [SerializeField] private bool whenFast;
    [Tooltip("Afterimages appear only while the object moves faster than this (units per second).")]
    [SerializeField, Min(0f)] private float minSpeed = 4f;
    [Tooltip("Multiplies each sprite's own colour (or replaces it, with Replace Color).")]
    [SerializeField] private Color tint = new Color(1f, 1f, 1f, 0.5f);
    [Tooltip("Afterimages take Tint as their colour instead of tinting the sprite's colour.")]
    [SerializeField] private bool replaceColor;
    [SerializeField, Min(0.005f)] private float interval = 0.03f;
    [SerializeField, Min(0.01f)] private float lifetime = 0.2f;
    [Tooltip("Extra width of each afterimage (0.4 = 40% wider, and a little flatter), for a sense of speed.")]
    [SerializeField, Min(0f)] private float stretch;
    [Tooltip("Units each afterimage is left behind the way it is moving sideways.")]
    [SerializeField, Min(0f)] private float backOffset;

    private readonly List<Ghost> live = new();
    private readonly Stack<Ghost> free = new();
    private SpriteRenderer[] sprites;
    private Transform ghostRoot;
    private Vector3 lastPosition;
    private float nextGhost;
    private float moveSign = 1f;

    // Switched on by code for a move (a dash, a dive) and off when it ends.
    public bool Emitting { get; set; }

    private class Ghost
    {
        public SpriteRenderer Renderer;
        public Color Color;
        public float Born;
    }

    private bool Wanted => Emitting || whenFast;

    private void Awake()
    {
        if (source == null)
            source = transform;
        sprites = source.GetComponentsInChildren<SpriteRenderer>(true);
        // Ghosts live outside the object so they stay where they were left.
        ghostRoot = new GameObject(name + " Afterimages").transform;
    }

    // Sets it up from code (for a component added at runtime, e.g. by DashEffects). Leaves afterimages only while
    // Emitting, whatever the speed.
    public void Configure(Transform copyFrom, Color color, bool colorReplaces, float everySeconds, float fadeSeconds,
                          float widthStretch, float behind)
    {
        source = copyFrom != null ? copyFrom : transform;
        sprites = source.GetComponentsInChildren<SpriteRenderer>(true);
        whenFast = false;
        minSpeed = 0f;
        tint = color;
        replaceColor = colorReplaces;
        interval = Mathf.Max(0.005f, everySeconds);
        lifetime = Mathf.Max(0.01f, fadeSeconds);
        stretch = Mathf.Max(0f, widthStretch);
        backOffset = Mathf.Max(0f, behind);
    }

    private void OnEnable() => lastPosition = transform.position;

    private void OnDisable()
    {
        Emitting = false;
        for (int i = live.Count - 1; i >= 0; i--)
            Retire(i);
    }

    private void OnDestroy()
    {
        if (ghostRoot != null)
            Destroy(ghostRoot.gameObject);
    }

    private void LateUpdate()
    {
        // Nothing to spawn and nothing fading: skip the frame.
        if (!Wanted && live.Count == 0)
        {
            lastPosition = transform.position;
            return;
        }

        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            Vector3 moved = transform.position - lastPosition;
            float speed = moved.magnitude / dt;
            lastPosition = transform.position;
            if (Mathf.Abs(moved.x) > 0.0001f)
                moveSign = Mathf.Sign(moved.x);
            if (Wanted && speed >= minSpeed && Time.time >= nextGhost)
            {
                nextGhost = Time.time + interval;
                foreach (SpriteRenderer sprite in sprites)
                    if (sprite != null && sprite.enabled && sprite.gameObject.activeInHierarchy && sprite.sprite != null)
                        Spawn(sprite);
            }
        }

        for (int i = live.Count - 1; i >= 0; i--)
        {
            Ghost ghost = live[i];
            float t = (Time.time - ghost.Born) / lifetime;
            if (t >= 1f)
            {
                Retire(i);
                continue;
            }
            Color c = ghost.Color;
            c.a *= 1f - t;
            ghost.Renderer.color = c;
        }
    }

    private void Spawn(SpriteRenderer from)
    {
        Ghost ghost;
        if (free.Count > 0)
        {
            ghost = free.Pop();
        }
        else
        {
            var go = new GameObject("Afterimage");
            go.transform.SetParent(ghostRoot, false);
            ghost = new Ghost { Renderer = go.AddComponent<SpriteRenderer>() };
        }
        live.Add(ghost);

        SpriteRenderer r = ghost.Renderer;
        r.sprite = from.sprite;
        r.flipX = from.flipX;
        r.flipY = from.flipY;
        r.drawMode = from.drawMode;
        r.size = from.size;
        r.sharedMaterial = from.sharedMaterial;
        r.sortingLayerID = from.sortingLayerID;
        r.sortingOrder = from.sortingOrder - 1;

        Transform t = r.transform;
        t.SetPositionAndRotation(from.transform.position - new Vector3(moveSign * backOffset, 0f, 0f),
                                 from.transform.rotation);
        Vector3 scale = from.transform.lossyScale;
        t.localScale = new Vector3(scale.x * (1f + stretch), scale.y * (1f - stretch * 0.25f), scale.z);

        ghost.Color = replaceColor ? tint : from.color * tint;
        ghost.Born = Time.time;
        r.color = ghost.Color;
        r.gameObject.SetActive(true);
    }

    // Hides a faded ghost and keeps it for reuse.
    private void Retire(int index)
    {
        Ghost ghost = live[index];
        live.RemoveAt(index);
        if (ghost.Renderer != null)
            ghost.Renderer.gameObject.SetActive(false);
        free.Push(ghost);
    }
}
