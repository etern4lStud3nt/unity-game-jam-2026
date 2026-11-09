using UnityEngine;

namespace Enemies
{
    // Visual feedback only: a short flash when hit, a shake while winding up an attack, and a fade for death.
    // Tinting multiplies the sprite colour, so the white flash reads best on light sprites; real art may want a
    // flash material later.
    public class EnemyFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] renderers;
        [Tooltip("Shaken while winding up; usually the Visual child.")]
        [SerializeField] private Transform shakeTarget;

        [Header("Hit")]
        [SerializeField] private Color hitColor = Color.white;
        [SerializeField, Min(0f)] private float hitTime = 0.1f;

        [Header("Wind-up warning (shake only, no tint)")]
        [SerializeField, Min(0f)] private float shakeAmount = 0.06f;

        private Color[] baseColors;
        private Vector3 shakeBase;
        private float hitUntil;
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
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].color;
            if (shakeTarget != null)
                shakeBase = shakeTarget.localPosition;
        }

        public void Hit() => hitUntil = Time.time + hitTime;

        public void SetWarning(bool on)
        {
            warning = on;
            if (!on && shakeTarget != null)
                shakeTarget.localPosition = shakeBase;
        }

        private void LateUpdate()
        {
            bool hit = Time.time < hitUntil;
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = hit ? hitColor : baseColors[i];
                c.a = baseColors[i].a * Fade;
                renderers[i].color = c;
            }

            if (warning && shakeTarget != null)
                shakeTarget.localPosition = shakeBase + (Vector3)(Random.insideUnitCircle * shakeAmount);
        }

        private void OnDisable() => SetWarning(false);
    }
}
