using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// A black screen over everything, for scene-to-scene moves (the casino doors). Builds its own overlay canvas the
// first time it is used, so nothing has to be placed in a scene. Fade with: yield return ScreenFade.To(1f, 0.3f).
public static class ScreenFade
{
    private static Image overlay;

    // Domain reload is off, so the static must be cleared by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => overlay = null;

    // Fades the screen to the given blackness (0 = clear, 1 = black) over the given time, in unscaled time.
    public static IEnumerator To(float alpha, float duration)
    {
        Image image = Overlay();
        float start = image.color.a;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetAlpha(image, Mathf.Lerp(start, alpha, t / duration));
            yield return null;
        }
        SetAlpha(image, alpha);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        image.color = new Color(0f, 0f, 0f, alpha);
        image.enabled = alpha > 0f;
    }

    private static Image Overlay()
    {
        if (overlay != null)
            return overlay;

        var root = new GameObject("Screen Fade", typeof(Canvas), typeof(CanvasScaler));
        Object.DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var child = new GameObject("Black", typeof(RectTransform), typeof(Image));
        child.transform.SetParent(root.transform, false);
        var rect = (RectTransform)child.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay = child.GetComponent<Image>();
        overlay.raycastTarget = false;
        SetAlpha(overlay, 0f);
        return overlay;
    }
}
