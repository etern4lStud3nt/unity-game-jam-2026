using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The player's lucky numbers as a fanned hand of gold chips. A number earned while the screen is open flies into
// the hand. Clicking a chip glides it to the middle of the screen over a dimmed background, with its stats on its
// right: name, who gave it, its chance when bet on, its payout and its story. Clicking again (or the dimmed
// background) sends it back. Chips are copies of the inactive chip template, laid out under the hand.
public class LuckyNumberHand : MonoBehaviour
{
    [SerializeField] private Roulette roulette;
    [Tooltip("Inactive chip in the scene that every lucky chip is copied from.")]
    [SerializeField] private LuckyNumberChip chipTemplate;
    [Tooltip("Where the chips are laid out (their parent).")]
    [SerializeField] private RectTransform hand;

    [Header("Fan")]
    [Tooltip("Distance between chip centres.")]
    [SerializeField] private float spacing = 72f;
    [Tooltip("Tilt between neighbouring chips, in degrees.")]
    [SerializeField] private float fanAngle = 6f;
    [Tooltip("How far the outer chips drop below the middle ones.")]
    [SerializeField] private float arc = 6f;
    [SerializeField] private float hoverLift = 16f;
    [SerializeField, Min(1f)] private float moveSpeed = 10f;

    [Header("Focus")]
    [Tooltip("Dims the screen and holds the clicked chip; hidden when no chip is focused.")]
    [SerializeField] private GameObject focus;
    [Tooltip("Clicking it sends the chip back.")]
    [SerializeField] private Button dim;
    [Tooltip("Where the clicked chip goes (the middle of the screen).")]
    [SerializeField] private RectTransform focusSlot;
    [SerializeField, Min(1f)] private float focusScale = 2f;
    [Tooltip("The stats next to the focused chip; fades in once the chip arrives.")]
    [SerializeField] private CanvasGroup stats;
    [SerializeField] private RectTransform statsLayout;
    [SerializeField] private TMP_Text statTitle;
    [SerializeField] private TMP_Text statFrom;
    [SerializeField] private TMP_Text statChance;
    [SerializeField] private TMP_Text statPayout;
    [SerializeField] private TMP_Text statStory;

    private readonly List<LuckyNumberChip> chips = new();
    private LuckyNumberChip hovered;
    private LuckyNumberChip focused;

    private void Awake()
    {
        if (dim != null)
            dim.onClick.AddListener(Unfocus);
    }

    private void OnEnable()
    {
        if (chipTemplate != null)
            chipTemplate.gameObject.SetActive(false);
        if (focus != null)
            focus.SetActive(false);
        LuckyNumbers.Changed += OnLuckyNumbersChanged;
        Sync(instant: true);
    }

    private void OnDisable()
    {
        LuckyNumbers.Changed -= OnLuckyNumbersChanged;
        hovered = null;
        Unfocus();
    }

    private void OnLuckyNumbersChanged() => Sync(instant: false);

    // One chip per lucky number, in the order they were earned. New ones fly in from above the hand.
    private void Sync(bool instant)
    {
        if (chipTemplate == null || hand == null)
            return;

        for (int i = chips.Count - 1; i >= 0; i--)
        {
            LuckyNumberChip chip = chips[i];
            if (chip != null && LuckyNumbers.Get(chip.Data.Number) == chip.Data)
                continue;
            if (chip == focused)
                Unfocus();
            if (chip != null)
                Destroy(chip.gameObject);
            chips.RemoveAt(i);
        }

        foreach (LuckyNumberData data in LuckyNumbers.All)
        {
            if (chips.Exists(c => c.Data == data))
                continue;
            LuckyNumberChip chip = Instantiate(chipTemplate, hand);
            chip.gameObject.SetActive(true);
            chip.Bind(data);
            chip.Hovered += OnChipHovered;
            chip.Clicked += OnChipClicked;
            chips.Add(chip);
            if (!instant)
            {
                chip.Rect.anchoredPosition = new Vector2(0f, 220f);
                chip.Rect.localScale = Vector3.zero;
            }
        }

        Layout(instant);
    }

    private void Update() => Layout(false);

    private void Layout(bool instant)
    {
        float middle = (chips.Count - 1) * 0.5f;
        float t = instant ? 1f : 1f - Mathf.Exp(-moveSpeed * Time.unscaledDeltaTime);
        for (int i = 0; i < chips.Count; i++)
        {
            LuckyNumberChip chip = chips[i];
            bool isFocused = chip == focused;
            float offset = i - middle;

            // In the hand the chip sits in the fan; when focused it glides to the slot (its parent then) and grows.
            Vector2 position = isFocused ? Vector2.zero : new Vector2(offset * spacing, -Mathf.Abs(offset) * arc);
            Quaternion rotation = isFocused ? Quaternion.identity : Quaternion.Euler(0f, 0f, -offset * fanAngle);
            Vector3 scale = Vector3.one * (isFocused ? focusScale : 1f);
            chip.Rect.anchoredPosition = Vector2.Lerp(chip.Rect.anchoredPosition, position, t);
            chip.Rect.localRotation = Quaternion.Slerp(chip.Rect.localRotation, rotation, t);
            chip.Rect.localScale = Vector3.Lerp(chip.Rect.localScale, scale, t);

            if (chip.Visual != chip.Rect)
            {
                bool lift = chip == hovered && !isFocused;
                chip.Visual.anchoredPosition = Vector2.Lerp(chip.Visual.anchoredPosition, new Vector2(0f, lift ? hoverLift : 0f), t);
            }
        }

        // Stats fade in once the focused chip is nearly in place.
        if (stats != null)
        {
            bool show = focused != null && focused.Rect.anchoredPosition.sqrMagnitude < 40f * 40f;
            stats.alpha = instant ? (show ? 1f : 0f) : Mathf.MoveTowards(stats.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        }
    }

    private void OnChipHovered(LuckyNumberChip chip, bool on)
    {
        if (on)
            hovered = chip;
        else if (hovered == chip)
            hovered = null;
    }

    private void OnChipClicked(LuckyNumberChip chip)
    {
        if (chip == focused)
            Unfocus();
        else
            Focus(chip);
    }

    private void Focus(LuckyNumberChip chip)
    {
        if (focused != null)
            Unfocus();
        if (focus == null || focusSlot == null)
            return;

        focused = chip;
        FillStats(chip.Data);
        focus.SetActive(true);
        if (stats != null)
            stats.alpha = 0f;
        // Keeps its place on screen, then glides from there.
        chip.transform.SetParent(focusSlot, true);
    }

    private void Unfocus()
    {
        if (focused != null)
        {
            LuckyNumberChip chip = focused;
            focused = null;
            if (hand != null)
            {
                chip.transform.SetParent(hand, true);
                chip.transform.SetSiblingIndex(chips.IndexOf(chip) + 1); // after the inactive template
            }
        }
        if (focus != null)
            focus.SetActive(false);
    }

    private void FillStats(LuckyNumberData data)
    {
        Set(statTitle, data.DisplayName);
        Set(statFrom, string.IsNullOrWhiteSpace(data.GivenBy) ? null : "FROM " + data.GivenBy.ToUpperInvariant());
        // The chance it lands when the player bets on it; other lucky numbers do not change it.
        Set(statChance, "CHANCE " + (Mathf.Round(data.Chance * 1000f) / 10f).ToString("0.#", CultureInfo.InvariantCulture) + "%");
        Set(statPayout, "PAYS " + data.Payout.ToString("0.#", CultureInfo.InvariantCulture) + "x");
        Set(statStory, string.IsNullOrWhiteSpace(data.Story) ? null : data.Story.Trim());
        if (statsLayout != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(statsLayout);
    }

    // Empty text hides its line, so a chip without a giver or a story leaves no gap.
    private static void Set(TMP_Text text, string value)
    {
        if (text == null)
            return;
        text.gameObject.SetActive(!string.IsNullOrEmpty(value));
        text.text = value ?? "";
    }
}
