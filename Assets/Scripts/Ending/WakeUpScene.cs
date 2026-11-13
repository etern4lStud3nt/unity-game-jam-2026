using System.Collections;
using UnityEngine;

// The wake-up after the fall: the screen fades in on the gambler lying in the grass with the family around him, he
// gets up with a small hop, the family hops too, he asks whether it was all a dream (a Yarn node), then the screen
// goes black and the credits roll.
public class WakeUpScene : MonoBehaviour
{
    [SerializeField] private SpriteRenderer gambler;
    [SerializeField] private Sprite lying;
    [SerializeField] private Sprite standing;
    [Tooltip("The family standing around him. They hop when he gets up.")]
    [SerializeField] private Transform[] family;

    [Header("Timing")]
    [Tooltip("Fade in from black, in seconds.")]
    [SerializeField, Min(0f)] private float fadeIn = 2f;
    [Tooltip("How long he lies there once the screen is clear, in seconds.")]
    [SerializeField, Min(0f)] private float lieTime = 1.5f;
    [Tooltip("Pause after he gets up, before he talks, in seconds.")]
    [SerializeField, Min(0f)] private float beforeTalk = 0.8f;

    [Header("Hop")]
    [SerializeField, Min(0f)] private float hopHeight = 0.4f;
    [SerializeField, Min(0.01f)] private float hopTime = 0.3f;

    [Header("Dialogue")]
    [Tooltip("Yarn node he says after getting up.")]
    [SerializeField] private string dreamNode = "WakeUp_Dream";

    [Header("Credits")]
    [SerializeField] private CreditsRoll credits;
    [Tooltip("Pause after the last line before the screen goes black, in seconds.")]
    [SerializeField, Min(0f)] private float beforeCredits = 1.5f;
    [SerializeField, Min(0f)] private float fadeOut = 1.5f;

    private bool talking;

    private void OnEnable() => EventBus.Subscribe<DialogueEnded>(OnDialogueEnded);

    private void OnDisable() => EventBus.Unsubscribe<DialogueEnded>(OnDialogueEnded);

    private void OnDialogueEnded(DialogueEnded evt) => talking = false;

    private IEnumerator Start()
    {
        if (gambler != null)
            gambler.sprite = lying;

        // Arrives black from the ending; starts black when played on its own too.
        yield return ScreenFade.To(1f, 0f);
        yield return ScreenFade.To(0f, fadeIn);
        yield return new WaitForSeconds(lieTime);

        if (gambler != null)
        {
            gambler.sprite = standing;
            StartCoroutine(Hop(gambler.transform, 0f));
        }
        for (int i = 0; i < family.Length; i++)
            if (family[i] != null)
                StartCoroutine(Hop(family[i], 0.1f + 0.12f * i));

        yield return new WaitForSeconds(beforeTalk);
        if (!string.IsNullOrEmpty(dreamNode))
        {
            talking = DialogueBridge.Run(dreamNode);
            while (talking)
                yield return null;
        }

        yield return new WaitForSeconds(beforeCredits);
        yield return ScreenFade.To(1f, fadeOut);
        if (credits != null)
            yield return credits.Play();
    }

    // One small jump up and back down (a parabola), after a delay.
    private IEnumerator Hop(Transform target, float delay)
    {
        yield return new WaitForSeconds(delay);
        Vector3 home = target.localPosition;
        for (float t = 0f; t < hopTime; t += Time.deltaTime)
        {
            float k = t / hopTime;
            target.localPosition = home + Vector3.up * (4f * hopHeight * k * (1f - k));
            yield return null;
        }
        target.localPosition = home;
    }
}
