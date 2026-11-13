using UnityEngine;

namespace Enemies
{
    // Visual feedback only: a white-red blink when hit, a shake while winding up an attack, and a fade for death.
    // White is the sprite drawn as a flat silhouette (Flash Material); red tints it.
    public class EnemyFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] renderers;
        [Tooltip("Shaken while winding up; usually the Visual child.")]
        [SerializeField] private Transform shakeTarget;

        [Header("Hit: blinks white, then red, Blinks times")]
        [Tooltip("Draws the sprite as a flat white silhouette (Sprite Flash material) for the white blinks.")]
        [SerializeField] private Material flashMaterial;
        [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.25f);
        [SerializeField, Min(1)] private int blinks = 2;
        [Tooltip("Seconds each white or red step shows.")]
        [SerializeField, Min(0.01f)] private float blinkTime = 0.05f;

        [Header("Wind-up warning (shake only, no tint)")]
        [SerializeField, Min(0f)] private float shakeAmount = 0.06f;

        private Color[] baseColors;
        private Material[] baseMaterials;
        private Vector3 shakeBase;
        private float hitStart = float.NegativeInfinity;
        private bool warning;

        // 1 = fully visible, 0 = invisible. Used by the death state.
        public float Fade { get; set; } = 1f;

        private void Reset()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
            shakeTarget = transform.Find("Visual");
        }

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            baseMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                baseColors[i] = renderers[i].color;
                baseMaterials[i] = renderers[i].sharedMaterial;
            }
            if (shakeTarget != null)
                shakeBase = shakeTarget.localPosition;
        }

        public void Hit() => hitStart = Time.time;

        public void SetWarning(bool on)
        {
            warning = on;
            if (!on && shakeTarget != null)
                shakeTarget.localPosition = shakeBase;
        }

        private void LateUpdate()
        {
            // Steps since the hit: even = white, odd = red, then back to normal.
            int step = Mathf.FloorToInt((Time.time - hitStart) / blinkTime);
            bool blinking = step >= 0 && step < blinks * 2;
            bool white = blinking && step % 2 == 0 && flashMaterial != null;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                Color c = white ? Color.white : blinking ? hitColor : baseColors[i];
                c.a = baseColors[i].a * Fade;
                renderers[i].color = c;
                renderers[i].sharedMaterial = white ? flashMaterial : baseMaterials[i];
            }

            if (warning && shakeTarget != null)
                shakeTarget.localPosition = shakeBase + (Vector3)(Random.insideUnitCircle * shakeAmount);
        }

        private void OnDisable() => SetWarning(false);
    }
}
