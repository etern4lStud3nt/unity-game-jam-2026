using System.Collections.Generic;
using UnityEngine;

// Shared by player and enemy slashes: transparent padding must never shorten the visible attack range.
public static class SlashSpriteBounds
{
    private static readonly Dictionary<Sprite, Bounds> Cache = new Dictionary<Sprite, Bounds>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Cache.Clear();

    public static Bounds GetVisibleBounds(Sprite sprite)
    {
        if (Cache.TryGetValue(sprite, out Bounds bounds))
            return bounds;

        // Mesh bounds are a fallback for textures that cannot be read (or are atlas-packed).
        Vector2[] vertices = sprite.vertices;
        bounds = vertices.Length > 0 ? new Bounds(vertices[0], Vector3.zero) : sprite.bounds;
        foreach (Vector2 vertex in vertices)
            bounds.Encapsulate(vertex);

        Texture2D texture = sprite.texture;
        if (texture.isReadable && !sprite.packed)
        {
            Color32[] pixels = texture.GetPixels32();
            Rect rect = sprite.rect;
            int xStart = Mathf.RoundToInt(rect.x);
            int yStart = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (pixels[(yStart + y) * texture.width + xStart + x].a == 0)
                        continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }

            if (maxX >= minX && maxY >= minY)
            {
                Vector2 min = (new Vector2(minX, minY) - sprite.pivot) / sprite.pixelsPerUnit;
                Vector2 max = (new Vector2(maxX + 1, maxY + 1) - sprite.pivot) / sprite.pixelsPerUnit;
                bounds = new Bounds((min + max) * 0.5f, max - min);
            }
        }

        Cache.Add(sprite, bounds);
        return bounds;
    }
}
