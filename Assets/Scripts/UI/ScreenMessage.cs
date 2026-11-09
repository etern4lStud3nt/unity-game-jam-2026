using System.Collections;
using TMPro;
using UnityEngine;

// A short two-line message in the upper middle of the screen (ability unlocked, area name...). Plain white text
// that fades in, holds and fades out. Creates its own overlay canvas the first time it is used.
public class ScreenMessage : MonoBehaviour
{
    private const float FadeIn = 0.25f;
    private const float Hold = 2f;
    private const float FadeOut = 0.6f;

    private static ScreenMessage instance;

    private TextMeshProUGUI titleText;
    private TextMeshProUGUI hintText;
    private CanvasGroup group;
    private Coroutine running;

    public static void Show(string title, string hint = "")
    {
        if (instance == null)
            instance = Create();
        instance.Play(title, hint);
    }

    private static ScreenMessage Create()
    {
        var root = new GameObject("Screen Message");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var message = root.AddComponent<ScreenMessage>();
        message.group = root.AddComponent<CanvasGroup>();
        message.group.alpha = 0f;
        message.group.blocksRaycasts = false;
        message.titleText = AddLine(root.transform, "Title", 64f, new Vector2(0f, 230f), FontStyles.Normal);
        message.hintText = AddLine(root.transform, "Hint", 30f, new Vector2(0f, 165f), FontStyles.Normal);
        message.hintText.color = new Color(1f, 1f, 1f, 0.75f);
        return message;
    }

    private static TextMeshProUGUI AddLine(Transform parent, string name, float size, Vector2 position, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1400f, size * 1.4f);
        rect.anchoredPosition = position;
        var text = go.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = size;
        text.fontStyle = style;
        text.characterSpacing = 4f;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void Play(string title, string hint)
    {
        titleText.text = title;
        hintText.text = hint;
        if (running != null)
            StopCoroutine(running);
        running = StartCoroutine(Run());
    }

    // Unscaled time, so a hitstop or pause does not freeze the message.
    private IEnumerator Run()
    {
        for (float t = 0f; t < FadeIn; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / FadeIn;
            yield return null;
        }
        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(Hold);
        for (float t = 0f; t < FadeOut; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / FadeOut;
            yield return null;
        }
        group.alpha = 0f;
        running = null;
    }
}
