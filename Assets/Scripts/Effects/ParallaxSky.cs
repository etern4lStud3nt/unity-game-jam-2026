using System;
using UnityEngine;
using UnityEngine.Rendering;

// A sky that always fills the screen behind the level (orthographic or perspective camera). Each layer is a sprite (Draw Mode: Tiled, the texture's Wrap
// Mode: Repeat) scaled to the camera's height (or a share of it, see Height and Offset, e.g. a band of clouds on the
// horizon) and repeated sideways forever. When the camera moves, a layer slides
// by Scroll times as much, so far layers (small Scroll) seem to move less; Drift moves it on its own (wind).
// Vertically the sky stays with the camera, so it never runs out on a tall map.
// Runs just before the camera renders, after the Cinemachine brain has moved it, so the layers never lag a frame.
public class ParallaxSky : MonoBehaviour
{
    [Serializable]
    private class Layer
    {
        public SpriteRenderer sprite;
        [Tooltip("How much it slides when the camera moves: 0 = not at all (infinitely far), 1 = like the level.")]
        [Range(0f, 1f)] public float scroll = 0.1f;
        [Tooltip("Sideways speed of its own, in sky pixels per second (negative = to the left).")]
        public float drift;
        [Tooltip("How tall it is, as a share of the screen's height: 1 = fills the screen.")]
        [Min(0.05f)] public float height = 1f;
        [Tooltip("Where its middle sits, in screen heights from the screen's middle: 0 = centred, -0.25 = a " +
                 "quarter of the screen lower.")]
        public float offset;
    }

    [Tooltip("The camera the sky fills. Empty = the main camera.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Back to front.")]
    [SerializeField] private Layer[] layers;
    [Tooltip("How many screen widths each layer is wide, so its repeats always cover the screen.")]
    [SerializeField, Min(2)] private int repeats = 3;

    private void OnEnable() => RenderPipelineManager.beginCameraRendering += Place;

    private void OnDisable() => RenderPipelineManager.beginCameraRendering -= Place;

    private void Place(ScriptableRenderContext context, Camera cam)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (cam != targetCamera)
            return;

        Vector3 at = cam.transform.position;
        foreach (Layer layer in layers)
        {
            if (layer.sprite == null || layer.sprite.sprite == null)
                continue;
            // The height the camera sees at the layer's depth (the game camera is a perspective one).
            float viewHeight = cam.orthographic
                ? cam.orthographicSize * 2f
                : 2f * Mathf.Abs(layer.sprite.transform.position.z - at.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            Sprite art = layer.sprite.sprite;
            Vector2 size = art.rect.size / art.pixelsPerUnit;
            float scale = viewHeight * layer.height / size.y;
            float pixel = scale / art.pixelsPerUnit;
            float width = size.x * scale;

            // Slide by the camera (and the drift), snapped to whole sky pixels so the art stays crisp.
            float slide = at.x * layer.scroll - Time.time * layer.drift * pixel;
            slide = Mathf.Round(Mathf.Repeat(slide, width) / pixel) * pixel;

            Transform t = layer.sprite.transform;
            t.position = new Vector3(at.x - slide + width * 0.5f, at.y + layer.offset * viewHeight, t.position.z);
            t.localScale = new Vector3(scale, scale, 1f);
            layer.sprite.drawMode = SpriteDrawMode.Tiled;
            layer.sprite.size = new Vector2(size.x * repeats, size.y);
        }
    }
}
