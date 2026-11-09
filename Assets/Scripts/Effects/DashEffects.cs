using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using CharacterController = Assets.Scripts.CharacterController;

// Makes the dash feel fast: stretched afterimages, speed streaks behind the player and a small camera kick.
// Put it on the character next to CharacterController. Purely visual; it never touches physics.
// No [RequireComponent(typeof(CharacterController))]: our CharacterController shares its name with Unity's
// built-in 3D CharacterController, and requiring it made Unity re-add the Rigidbody2D on the prefab variant,
// which then dropped this component on load.
public class DashEffects : MonoBehaviour
{
    [Header("Afterimages")]
    [SerializeField] private SpriteRenderer source;
    [SerializeField] private Color ghostColor = new Color(0.45f, 0.95f, 1f, 0.75f);
    [SerializeField, Min(0.005f)] private float ghostInterval = 0.02f;
    [SerializeField, Min(0.01f)] private float ghostLifetime = 0.22f;
    [Tooltip("Extra horizontal stretch of each afterimage (0.4 = 40% wider).")]
    [SerializeField] private float ghostStretch = 0.4f;

    [Header("Speed streaks")]
    [SerializeField] private Color streakColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField, Min(0.01f)] private float streakTime = 0.14f;
    [Tooltip("Vertical offsets of the streaks, relative to the sprite centre, in sprite heights.")]
    [SerializeField] private float[] streakOffsets = { -0.35f, 0f, 0.35f };
    [SerializeField] private float[] streakWidths = { 0.08f, 0.28f, 0.08f };
    [Tooltip("Material for the streaks. Empty = the sprite's own material.")]
    [SerializeField] private Material streakMaterial;

    [Header("Camera kick")]
    [SerializeField] private CinemachineImpulseSource impulse;
    [SerializeField] private float kickForce = 0.25f;

    private CharacterController controller;
    private readonly List<SpriteRenderer> pool = new();
    private readonly List<float> born = new();
    private readonly List<TrailRenderer> streaks = new();
    private Transform ghostRoot;
    private float nextGhost;
    private float direction;
    private bool dashing;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogWarning($"{nameof(DashEffects)} on {name} needs a CharacterController.", this);
            enabled = false;
            return;
        }
        if (source == null) source = GetComponentInChildren<SpriteRenderer>();
        if (impulse == null) impulse = GetComponent<CinemachineImpulseSource>();

        // Ghosts live outside the character so they stay where they were left.
        ghostRoot = new GameObject(name + " Dash Ghosts").transform;
        BuildStreaks();
    }

    private void OnEnable()
    {
        if (controller == null) return;
        controller.DashStarted += OnDashStarted;
        controller.DashEnded += OnDashEnded;
    }

    private void OnDisable()
    {
        if (controller == null) return;
        controller.DashStarted -= OnDashStarted;
        controller.DashEnded -= OnDashEnded;
        OnDashEnded();
    }

    private void OnDestroy()
    {
        if (ghostRoot != null) Destroy(ghostRoot.gameObject);
    }

    private void OnDashStarted(float dir)
    {
        direction = dir;
        dashing = true;
        nextGhost = 0f;
        foreach (TrailRenderer streak in streaks)
        {
            streak.Clear();
            streak.emitting = true;
        }

        if (impulse != null)
            impulse.GenerateImpulseWithVelocity(new Vector3(-dir * kickForce, 0f, 0f));
    }

    private void OnDashEnded()
    {
        dashing = false;
        foreach (TrailRenderer streak in streaks)
            if (streak != null) streak.emitting = false;
    }

    private void LateUpdate()
    {
        if (dashing && source != null && Time.time >= nextGhost)
        {
            nextGhost = Time.time + ghostInterval;
            SpawnGhost();
        }

        for (int i = 0; i < pool.Count; i++)
        {
            SpriteRenderer ghost = pool[i];
            if (!ghost.gameObject.activeSelf) continue;

            float t = (Time.time - born[i]) / ghostLifetime;
            if (t >= 1f)
            {
                ghost.gameObject.SetActive(false);
                continue;
            }

            Color c = ghostColor;
            c.a *= 1f - t;
            ghost.color = c;
        }
    }

    private void SpawnGhost()
    {
        int index = pool.FindIndex(g => !g.gameObject.activeSelf);
        if (index < 0)
        {
            var go = new GameObject("Ghost");
            go.transform.SetParent(ghostRoot, false);
            pool.Add(go.AddComponent<SpriteRenderer>());
            born.Add(0f);
            index = pool.Count - 1;
        }

        SpriteRenderer ghost = pool[index];
        ghost.sprite = source.sprite;
        ghost.flipX = source.flipX;
        ghost.drawMode = source.drawMode;
        ghost.size = source.size;
        ghost.sharedMaterial = source.sharedMaterial;
        ghost.sortingLayerID = source.sortingLayerID;
        ghost.sortingOrder = source.sortingOrder - 1;

        Transform t = ghost.transform;
        t.SetPositionAndRotation(source.transform.position - new Vector3(direction * 0.15f, 0f, 0f), source.transform.rotation);
        Vector3 scale = source.transform.lossyScale;
        t.localScale = new Vector3(scale.x * (1f + ghostStretch), scale.y * (1f - ghostStretch * 0.25f), scale.z);

        born[index] = Time.time;
        ghost.color = ghostColor;
        ghost.gameObject.SetActive(true);
    }

    private void BuildStreaks()
    {
        if (source == null) return;
        Material material = streakMaterial != null ? streakMaterial : source.sharedMaterial;
        float height = source.bounds.size.y;
        Vector3 centre = transform.InverseTransformPoint(source.bounds.center);

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(streakColor, 0f), new GradientColorKey(streakColor, 1f) },
            new[] { new GradientAlphaKey(streakColor.a, 0f), new GradientAlphaKey(0f, 1f) });

        for (int i = 0; i < streakOffsets.Length; i++)
        {
            var go = new GameObject("Dash Streak " + i);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = centre + new Vector3(0f, streakOffsets[i] * height, 0f);

            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = streakTime;
            trail.widthMultiplier = i < streakWidths.Length ? streakWidths[i] : 0.1f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            trail.colorGradient = gradient;
            trail.minVertexDistance = 0.05f;
            trail.numCapVertices = 2;
            trail.sortingLayerID = source.sortingLayerID;
            trail.sortingOrder = source.sortingOrder - 2;
            trail.emitting = false;
            streaks.Add(trail);
        }
    }
}
