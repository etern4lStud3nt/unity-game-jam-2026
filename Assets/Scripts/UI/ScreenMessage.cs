using System.Collections;
using TMPro;
using UnityEngine;

// A short two-line message on screen (ability unlocked, area name...): plain text that fades in, holds and fades
// out. Lives on the ScreenMessage prefab in the scene; position, font and size are set on the prefab's texts,
// timings here. Call ScreenMessage.Show from anywhere.
[RequireComponent(typeof(CanvasGroup))]
public class ScreenMessage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField, Min(0f)] private float fadeIn = 0.25f;
    [SerializeField, Min(0f)] private float hold = 2f;
    [SerializeField, Min(0f)] private float fadeOut = 0.6f;

    private static ScreenMessage instance;

    private CanvasGroup group;
    private Coroutine running;

    // Domain reload is off, so the static must be cleared by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    // holdOverride > 0 keeps this message up that many seconds instead of the usual hold.
    public static void Show(string title, string hint = "", float holdOverride = 0f)
    {
        if (instance == null)
            instance = FindAnyObjectByType<ScreenMessage>(FindObjectsInactive.Include);
        if (instance == null)
        {
            Debug.LogWarning($"ScreenMessage.Show(\"{title}\"): no ScreenMessage prefab in the scene.");
            return;
        }
        instance.Play(title, hint, holdOverride);
    }

    private void Awake()
    {
        instance = this;
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    private void Play(string title, string hint, float holdFor)
    {
        gameObject.SetActive(true);
        if (titleText != null)
            titleText.text = title;
        if (hintText != null)
            hintText.text = hint;
        if (running != null)
            StopCoroutine(running);
        running = StartCoroutine(Run(holdFor > 0f ? holdFor : hold));
    }

    // Unscaled time, so a hitstop or pause does not freeze the message.
    private IEnumerator Run(float holdFor)
    {
        for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / fadeIn;
            yield return null;
        }
        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(holdFor);
        for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / fadeOut;
            yield return null;
        }
        group.alpha = 0f;
        running = null;
    }
}
