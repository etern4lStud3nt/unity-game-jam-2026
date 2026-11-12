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
    [SerializeField] private Color tint = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField, Min(0.005f)] private float interval = 0.03f;
    [SerializeField, Min(0.01f)] private float lifetime = 0.2f;

    private readonly List<Ghost> pool = new();
    private SpriteRenderer[] sprites;
    private Transform ghostRoot;
    private Vector3 lastPosition;
    private float nextGhost;

    // Switched on by code for a move (a dash, a dive) and off when it ends.
    public bool Emitting { get; set; }

    private class Ghost
    {
        public SpriteRenderer Renderer;
        public Color Color;
        public float Born;
    }

    private void Awake()
    {
        if (source == null)
            source = transform;
        sprites = source.GetComponentsInChildren<SpriteRenderer>(true);
        // Ghosts live outside the object so they stay where they were left.
        ghostRoot = new GameObject(name + " Afterimages").transform;
    }

    private void OnEnable() => lastPosition = transform.position;

    private void OnDisable()
    {
        Emitting = false;
        foreach (Ghost ghost in pool)
            if (ghost.Renderer != null)
                ghost.Renderer.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ghostRoot != null)
            Destroy(ghostRoot.gameObject);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            float speed = (transform.position - lastPosition).magnitude / dt;
            lastPosition = transform.position;
            if ((Emitting || whenFast) && speed >= minSpeed && Time.time >= nextGhost)
            {
                nextGhost = Time.time + interval;
                foreach (SpriteRenderer sprite in sprites)
                    if (sprite != null && sprite.enabled && sprite.gameObject.activeInHierarchy && sprite.sprite != null)
                        Spawn(sprite);
            }
        }

        foreach (Ghost ghost in pool)
        {
            if (!ghost.Renderer.gameObject.activeSelf)
                continue;
            float t = (Time.time - ghost.Born) / lifetime;
            if (t >= 1f)
            {
                ghost.Renderer.gameObject.SetActive(false);
                continue;
            }
            Color c = ghost.Color;
            c.a *= 1f - t;
            ghost.Renderer.color = c;
        }
    }

    private void Spawn(SpriteRenderer from)
    {
        Ghost ghost = pool.Find(g => !g.Renderer.gameObject.activeSelf);
        if (ghost == null)
        {
            var go = new GameObject("Afterimage");
            go.transform.SetParent(ghostRoot, false);
            ghost = new Ghost { Renderer = go.AddComponent<SpriteRenderer>() };
            pool.Add(ghost);
        }

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
        t.SetPositionAndRotation(from.transform.position, from.transform.rotation);
        t.localScale = from.transform.lossyScale;

        ghost.Color = from.color * tint;
        ghost.Born = Time.time;
        r.color = ghost.Color;
        r.gameObject.SetActive(true);
    }
}
