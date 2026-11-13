using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Moonlit pixel sky: a quiet mouse-driven background behind the menu's stationary controls.
[RequireComponent(typeof(RectTransform))]
public class MenuMouseParallax : MonoBehaviour
{
    [Serializable]
    private class Layer
    {
        public RawImage image;
        public Vector2 movement;
        public Vector2 offset;
    }

    [SerializeField] private Layer[] layers;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;

    private RectTransform viewport;
    private Canvas canvas;
    private Vector2 offset;
    private Vector2 velocity;

    private void Awake()
    {
        viewport = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        offset = Vector2.zero;
        velocity = Vector2.zero;
        FitLayers();
    }

    private void LateUpdate()
    {
        if (layers == null || canvas == null)
            return;

        FitLayers();
        Vector2 pointer = Vector2.zero;
        if (Application.isFocused && Mouse.current != null)
        {
            Vector2 screenPoint = Mouse.current.position.ReadValue();
            if (screenPoint.x >= 0f && screenPoint.x <= Screen.width
                && screenPoint.y >= 0f && screenPoint.y <= Screen.height)
            {
                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, camera, out Vector2 local))
                {
                    Rect rect = viewport.rect;
                    pointer = new Vector2(
                        rect.width > 0f ? Mathf.Clamp((local.x - rect.center.x) * 2f / rect.width, -1f, 1f) : 0f,
                        rect.height > 0f ? Mathf.Clamp((local.y - rect.center.y) * 2f / rect.height, -1f, 1f) : 0f);
                }
            }
        }

        ApplyPointer(pointer, Time.unscaledDeltaTime);
    }

    private void ApplyPointer(Vector2 pointer, float deltaTime)
    {
        offset = Vector2.SmoothDamp(offset, pointer, ref velocity, Mathf.Max(0.01f, smoothTime),
            Mathf.Infinity, deltaTime);
        foreach (Layer layer in layers)
            if (layer.image != null)
                layer.image.rectTransform.anchoredPosition = layer.offset - Vector2.Scale(offset, layer.movement);
    }

    private void FitLayers()
    {
        if (layers == null)
            return;
        if (viewport == null)
            viewport = (RectTransform)transform;
        foreach (Layer layer in layers)
        {
            if (layer.image == null || layer.image.texture == null)
                continue;
            Vector2 source = new Vector2(layer.image.texture.width, layer.image.texture.height);
            Vector2 margin = new Vector2(Mathf.Abs(layer.movement.x) + Mathf.Abs(layer.offset.x),
                Mathf.Abs(layer.movement.y) + Mathf.Abs(layer.offset.y));
            Vector2 required = viewport.rect.size + margin * 2f + Vector2.one * 4f;
            float scale = Mathf.Max(required.x / source.x, required.y / source.y);
            layer.image.rectTransform.sizeDelta = source * scale;
        }
    }

    private void OnDisable()
    {
        if (layers != null)
            foreach (Layer layer in layers)
                if (layer.image != null)
                    layer.image.rectTransform.anchoredPosition = layer.offset;
        offset = Vector2.zero;
        velocity = Vector2.zero;
    }
}
