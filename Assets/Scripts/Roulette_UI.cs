using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Runs the roulette screen that is laid out in the scene: betting on RouletteBetSpots with the RouletteChip
// selected in the rack, spinning the wheel and paying out. Bets go to Roulette; chips come from ChipWallet.
public class Roulette_UI : MonoBehaviour
{
    [SerializeField] private Roulette roulette;

    [Header("Scene objects")]
    [SerializeField] private RectTransform wheel;
    [SerializeField] private RectTransform ball;
    [SerializeField] private Transform table;
    [SerializeField] private Transform chipRack;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text betText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text deltaText;
    [Tooltip("Parent of the wheel's number labels, named \"Pocket 7\", \"Pocket 00\"...; lucky ones turn gold.")]
    [SerializeField] private Transform wheelNumbers;
    [SerializeField] private Button spinButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button leaveButton;

    [Header("Colors")]
    [SerializeField] private Color winColor = new Color32(0xD9, 0xB4, 0x5A, 0xFF);
    [SerializeField] private Color lossColor = new Color32(0xE0, 0x5A, 0x5A, 0xFF);
    [SerializeField] private Color textColor = new Color32(0xF1, 0xE6, 0xD0, 0xFF);

    [Header("Rules")]
    [Tooltip("Given when the player sits down with no chips (for testing the table on its own). 0 = none.")]
    [SerializeField, Min(0)] private int startingChips = 100;
    [Tooltip("Given when the player has no lucky numbers yet, for testing the table on its own. Leave empty in the real game.")]
    [SerializeField] private LuckyNumberData[] testLuckyNumbers;

    [Header("Spin")]
    [SerializeField, Min(0.5f)] private float spinDuration = 4f;
    [SerializeField, Min(1)] private int wheelTurns = 3;
    [SerializeField, Min(0f)] private float resultHoldTime = 1.5f;
    [Tooltip("Ball distance from the wheel centre while it runs on the rim (x) and once it sits in a pocket (y), as a fraction of the wheel's half size.")]
    [SerializeField] private Vector2 ballRadius = new Vector2(0.92f, 0.64f);

    [Header("Win feel")]
    [Tooltip("Shaken on a win; usually the screen's root.")]
    [SerializeField] private RectTransform shakeTarget;
    [Tooltip("Shake distance in UI units; bigger wins shake a bit harder, up to twice this.")]
    [SerializeField, Min(0f)] private float shakeStrength = 12f;
    [SerializeField, Min(0f)] private float shakeTime = 0.4f;
    [Tooltip("Seconds the win amount takes to count up from 0.")]
    [SerializeField, Min(0.1f)] private float countUpTime = 1f;
    [Tooltip("How much the win amount swells on each beat while counting.")]
    [SerializeField, Min(0f)] private float countPulse = 0.25f;

    [Header("Bet punch (a spot bounces when chips land on it)")]
    [Tooltip("How much bigger a spot gets for a moment. 1.12 = 12% bigger.")]
    [SerializeField, Min(1f)] private float punchScale = 1.12f;
    [SerializeField, Min(0.01f)] private float punchTime = 0.2f;
    [Tooltip("Punch strength when a chip is taken back (1 = same as placing).")]
    [SerializeField, Range(0f, 1f)] private float removePunch = 0.5f;

    [Header("Sounds (optional)")]
    [SerializeField] private SoundData spinSound;
    [SerializeField] private SoundData chipSound;
    [SerializeField] private SoundData winSound;
    [SerializeField] private SoundData loseSound;
    [SerializeField] private SoundData winVoice;
    [SerializeField] private SoundData loseVoice;

    [Header("Events")]
    [Tooltip("Leave pressed: close the screen, go back to the casino.")]
    [SerializeField] private UnityEvent onLeave = new();
    [Tooltip("After every spin: the winning pocket (37 = 00) and the net chips won (negative = lost).")]
    [SerializeField] private UnityEvent<int, int> onSpinFinished = new();

    private readonly List<RouletteBetSpot> spots = new();
    private readonly List<RouletteChip> chips = new();
    private RouletteChip selectedChip;
    private bool spinning;

    private void Awake()
    {
        if (roulette == null)
            roulette = GetComponentInParent<Roulette>();

        spots.AddRange((table != null ? table : transform).GetComponentsInChildren<RouletteBetSpot>(true));
        foreach (RouletteBetSpot spot in spots)
            spot.Clicked += OnSpotClicked;

        chips.AddRange((chipRack != null ? chipRack : transform).GetComponentsInChildren<RouletteChip>(true));
        foreach (RouletteChip chip in chips)
        {
            RouletteChip c = chip;
            chip.Button.onClick.AddListener(() => SelectChip(c));
        }
        if (chips.Count > 0)
            SelectChip(chips[Mathf.Min(1, chips.Count - 1)]);

        if (spinButton != null) spinButton.onClick.AddListener(() => StartCoroutine(SpinRoutine()));
        if (clearButton != null) clearButton.onClick.AddListener(ClearBets);
        if (leaveButton != null) leaveButton.onClick.AddListener(() => { if (!spinning) onLeave.Invoke(); });

        SetText(resultText, "");
        SetText(deltaText, "");
        if (ball != null)
            ball.anchoredPosition = BallPosition(0f, ballRadius.x);
    }

    private void OnEnable()
    {
        ChipWallet.Changed += OnBalanceChanged;
        if (startingChips > 0 && ChipWallet.Balance == 0)
            ChipWallet.Set(startingChips);
        OnBalanceChanged(ChipWallet.Balance);

        LuckyNumbers.Changed += RefreshLucky;
        if (LuckyNumbers.Count == 0 && testLuckyNumbers != null)
            foreach (LuckyNumberData lucky in testLuckyNumbers)
                if (lucky != null)
                    lucky.Grant();
        RefreshLucky();
        Refresh();
    }

    private void OnDisable()
    {
        ChipWallet.Changed -= OnBalanceChanged;
        LuckyNumbers.Changed -= RefreshLucky;
        // Leaving mid-round gives the chips on the table back.
        if (!spinning)
            ClearBets();
    }

    private void SelectChip(RouletteChip chip)
    {
        if (spinning)
            return;
        selectedChip = chip;
        foreach (RouletteChip c in chips)
            c.SetSelected(c == chip);
    }

    private void OnSpotClicked(RouletteBetSpot spot, PointerEventData.InputButton button)
    {
        if (spinning || selectedChip == null)
            return;

        int value = selectedChip.Value;
        if (button == PointerEventData.InputButton.Left)
        {
            if (!ChipWallet.TrySpend(value))
                return;
            spot.SetAmount(spot.Amount + value, selectedChip.Color);
            spot.Punch(punchScale, punchTime);
            Play(chipSound);
        }
        else if (button == PointerEventData.InputButton.Right && spot.Amount > 0)
        {
            int back = Mathf.Min(value, spot.Amount);
            spot.SetAmount(spot.Amount - back, selectedChip.Color);
            spot.Punch(1f + (punchScale - 1f) * removePunch, punchTime);
            ChipWallet.Add(back);
        }
        Refresh();
    }

    private void ClearBets()
    {
        if (spinning)
            return;
        int refund = 0;
        foreach (RouletteBetSpot spot in spots)
        {
            refund += spot.Amount;
            spot.SetAmount(0, Color.white);
        }
        ChipWallet.Add(refund);
        Refresh();
    }

    private int TotalOnTable()
    {
        int total = 0;
        foreach (RouletteBetSpot spot in spots)
            total += spot.Amount;
        return total;
    }

    private void Refresh()
    {
        int total = TotalOnTable();
        SetText(betText, "BET " + total);
        if (spinButton != null) spinButton.interactable = total > 0 && !spinning;
        if (clearButton != null) clearButton.interactable = total > 0 && !spinning;
    }

    // Gold frame on lucky number spots and gold lucky numbers on the wheel (the cards are LuckyNumberHand's).
    private void RefreshLucky()
    {
        foreach (RouletteBetSpot spot in spots)
            spot.SetLucky(spot.BetType == RouletteBetType.Number && LuckyNumbers.Contains(spot.Value));

        if (wheelNumbers != null)
        {
            foreach (TMP_Text label in wheelNumbers.GetComponentsInChildren<TMP_Text>(true))
            {
                string name = label.gameObject.name.Replace("Pocket ", "");
                int pocket = name == "00" ? Roulette.DoubleZero : int.TryParse(name, out int n) ? n : -1;
                label.color = LuckyNumbers.Contains(pocket) ? winColor : textColor;
            }
        }
    }

    private void OnBalanceChanged(int balance) => SetText(balanceText, "CHIPS " + balance);

    private IEnumerator SpinRoutine()
    {
        if (spinning || TotalOnTable() == 0)
            yield break;

        spinning = true;
        Refresh();
        foreach (RouletteBetSpot spot in spots)
            spot.SetHighlight(false, winColor);
        SetText(resultText, "");
        SetText(deltaText, "");

        roulette.ClearBets();
        foreach (RouletteBetSpot spot in spots)
            spot.PlaceOn(roulette);
        int stake = roulette.TotalBet;
        int winning = roulette.Spin();
        int net = roulette.GetPayout(winning);

        Play(spinSound);
        yield return AnimateWheel(winning);

        SetText(resultText, Roulette.PocketLabel(winning));
        foreach (RouletteBetSpot spot in spots)
            if (spot.Amount > 0 && spot.Wins(winning, roulette))
                spot.SetHighlight(true, winColor);

        ChipWallet.Add(net + stake);
        if (net > 0)
        {
            Play(winSound);
            Play(winVoice);
            StartCoroutine(Shake(Mathf.Clamp01(net / 200f)));
            yield return CountUp(net);
        }
        else if (net < 0)
        {
            SetText(deltaText, "-" + (-net), lossColor);
            Play(loseSound);
            Play(loseVoice);
        }
        else
        {
            SetText(deltaText, "EVEN", textColor);
        }
        onSpinFinished.Invoke(winning, net);

        yield return new WaitForSeconds(resultHoldTime);

        foreach (RouletteBetSpot spot in spots)
        {
            spot.SetAmount(0, Color.white);
            spot.SetHighlight(false, winColor);
        }
        roulette.ClearBets();
        spinning = false;
        Refresh();
    }

    // Shakes the screen, snapped to whole art pixels (4 UI units) so the pixel art stays crisp.
    private IEnumerator Shake(float size)
    {
        if (shakeTarget == null || shakeStrength <= 0f)
            yield break;
        Vector2 home = shakeTarget.anchoredPosition;
        float strength = shakeStrength * (1f + size);
        for (float t = 0f; t < shakeTime; t += Time.deltaTime)
        {
            float fade = 1f - t / shakeTime;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * strength * fade;
            shakeTarget.anchoredPosition = home + new Vector2(Mathf.Round(offset.x / 4f) * 4f, Mathf.Round(offset.y / 4f) * 4f);
            yield return null;
        }
        shakeTarget.anchoredPosition = home;
    }

    // The win amount counts up from 0, beating bigger and smaller as it climbs, then pops once at the end.
    private IEnumerator CountUp(int amount)
    {
        if (deltaText == null)
            yield break;
        deltaText.color = winColor;
        Transform rect = deltaText.transform;
        const float beats = 6f;
        for (float t = 0f; t < countUpTime; t += Time.deltaTime)
        {
            float k = t / countUpTime;
            int shown = Mathf.RoundToInt(amount * (1f - Mathf.Pow(1f - k, 2f)));
            deltaText.text = "+" + shown;
            rect.localScale = Vector3.one * (1f + countPulse * Mathf.Abs(Mathf.Sin(k * Mathf.PI * beats)));
            yield return null;
        }
        deltaText.text = "+" + amount;

        // Final pop.
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            float k = t / 0.25f;
            rect.localScale = Vector3.one * (1f + countPulse * 2f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    // The wheel turns counter-clockwise and stops with the winning pocket at the top, under the pointer;
    // the ball runs the other way on the rim, then drops into that pocket.
    private IEnumerator AnimateWheel(int winning)
    {
        float step = 360f / Roulette.WheelOrder.Length;
        float target = Array.IndexOf(Roulette.WheelOrder, winning) * step;
        float start = wheel.localEulerAngles.z;
        float end = Mathf.Ceil((start + wheelTurns * 360f - target) / 360f) * 360f + target;
        float ballTurns = wheelTurns + 2;

        for (float t = 0f; t < spinDuration; t += Time.deltaTime)
        {
            float k = t / spinDuration;
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            wheel.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(start, end, eased));
            if (ball != null)
            {
                float drop = Mathf.Clamp01((k - 0.7f) / 0.3f);
                ball.anchoredPosition = BallPosition((1f - eased) * ballTurns * 360f, Mathf.Lerp(ballRadius.x, ballRadius.y, drop * drop));
            }
            yield return null;
        }
        wheel.localRotation = Quaternion.Euler(0f, 0f, end % 360f);
        if (ball != null)
            ball.anchoredPosition = BallPosition(0f, ballRadius.y);
    }

    // A point on the wheel, clockwise from the top, as a fraction of the wheel's half size.
    // The ball sits next to the wheel (same parent), so it does not turn with it.
    private Vector2 BallPosition(float degrees, float radius)
    {
        if (wheel == null)
            return Vector2.zero;
        float half = wheel.rect.width * 0.5f;
        float a = degrees * Mathf.Deg2Rad;
        return wheel.anchoredPosition + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * half * radius;
    }

    private void SetText(TMP_Text text, string value) => SetText(text, value, textColor);

    private void SetText(TMP_Text text, string value, Color color)
    {
        if (text == null)
            return;
        text.text = value;
        if (text == deltaText)
            text.color = color;
    }

    private void Play(SoundData sound)
    {
        if (sound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(sound);
    }
}
