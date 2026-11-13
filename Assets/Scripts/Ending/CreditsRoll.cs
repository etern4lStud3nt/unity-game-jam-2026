using System.Collections;
using TMPro;
using UnityEngine;

// End credits: the text rises from below the screen and stops when its last line reaches the middle
// ("Thanks for playing"). The text comes from a plain .txt asset so names can be edited without touching the scene.
public class CreditsRoll : MonoBehaviour
{
    [Tooltip("The credits canvas. Hidden until Play.")]
    [SerializeField] private GameObject screen;
    [Tooltip("The text that rolls. Its RectTransform is anchored to the bottom middle with the pivot at its bottom.")]
    [SerializeField] private TMP_Text text;
    [Tooltip("The credits, one line per line. Edit this file to change names.")]
    [SerializeField] private TextAsset credits;
    [Tooltip("Rolling speed, in canvas pixels per second (the canvas is 1080 high).")]
    [SerializeField, Min(1f)] private float speed = 70f;
    [Tooltip("Roll from the start every time this object is shown (a credits panel in the menu). Off = only when " +
             "someone calls Play (the ending).")]
    [SerializeField] private bool playOnEnable;

    private void Awake()
    {
        if (screen != null && !playOnEnable)
            screen.SetActive(false);
    }

    private void OnEnable()
    {
        if (playOnEnable)
            StartCoroutine(Play());
    }

    public IEnumerator Play()
    {
        if (text == null)
            yield break;
        if (credits != null)
            text.text = credits.text;
        if (screen != null)
            screen.SetActive(true);

        var rect = text.rectTransform;
        var canvas = (RectTransform)rect.parent;
        text.ForceMeshUpdate();
        float height = text.preferredHeight;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);

        // From fully under the screen to the last line in the middle.
        float y = -height;
        float stop = canvas.rect.height * 0.5f - text.fontSize;
        while (y < stop)
        {
            y = Mathf.Min(stop, y + speed * Time.deltaTime);
            rect.anchoredPosition = new Vector2(0f, y);
            yield return null;
        }
    }
}
