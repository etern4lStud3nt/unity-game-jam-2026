using System.Collections;
using UnityEngine;

// A lucky number lying in the level, e.g. the one a boss leaves behind. Touching it gives the player that lucky
// number (LuckyNumberData.Grant) and shows its name on screen.
[RequireComponent(typeof(Collider2D))]
public class LuckyNumberPickup : MonoBehaviour
{
    [SerializeField] private LuckyNumberData luckyNumber;
    [Tooltip("Shown on screen. Empty = the lucky number's Display Name.")]
    [SerializeField] private string title;
    [Tooltip("Second line on screen. Optional.")]
    [SerializeField] private string hint;
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0.01f)] private float popTime = 0.3f;

    private bool taken;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (taken)
            return;
        Rigidbody2D body = other.attachedRigidbody;
        if (!(other.CompareTag(playerTag) || (body != null && body.CompareTag(playerTag))))
            return;
        if (luckyNumber == null)
        {
            Debug.LogWarning($"{name} has no Lucky Number asset.", this);
            return;
        }

        taken = true;
        luckyNumber.Grant();
        ScreenMessage.Show(string.IsNullOrEmpty(title) ? luckyNumber.DisplayName : title, hint);
        StartCoroutine(Pop());
    }

    // Swells and fades, then disappears.
    private IEnumerator Pop()
    {
        GetComponent<Collider2D>().enabled = false;
        var sprite = GetComponent<SpriteRenderer>();
        Vector3 scale = transform.localScale;
        Color color = sprite != null ? sprite.color : Color.white;

        for (float t = 0f; t < popTime; t += Time.deltaTime)
        {
            float k = t / popTime;
            transform.localScale = scale * (1f + 0.6f * k);
            if (sprite != null)
                sprite.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
