using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum RouletteBetType
{
    Number,
    Red,
    Black,
    Odd,
    Even,
    Low,
    High,
    Dozen,
}

// One spot on the roulette table, placed by hand in the scene. Says which bet it is and shows the chips on it.
// Left click places the selected chip, right click takes one back (Roulette_UI handles both).
public class RouletteBetSpot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RouletteBetType betType;
    [Tooltip("Number bets: the number (37 = 00). Dozen bets: 1, 2 or 3. Ignored otherwise.")]
    [SerializeField] private int value;
    [SerializeField] private Image background;
    [Tooltip("Shown while chips are on this spot.")]
    [SerializeField] private GameObject chipMarker;
    [SerializeField] private Image chipImage;
    [SerializeField] private TMP_Text chipText;

    private Color baseColor;

    public event Action<RouletteBetSpot, PointerEventData.InputButton> Clicked;

    public RouletteBetType BetType => betType;
    public int Value => value;
    public int Amount { get; private set; }

    private void Awake()
    {
        if (background == null)
            background = GetComponent<Image>();
        if (background != null)
            baseColor = background.color;
        SetAmount(0, Color.white);
    }

    public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this, eventData.button);

    public void SetAmount(int amount, Color chipColor)
    {
        Amount = Mathf.Max(0, amount);
        if (chipMarker != null)
            chipMarker.SetActive(Amount > 0);
        if (chipImage != null && Amount > 0)
            chipImage.color = chipColor;
        // Dark text on light chips, light text on dark ones.
        if (chipText != null && Amount > 0)
            chipText.color = chipColor.r * 0.3f + chipColor.g * 0.59f + chipColor.b * 0.11f > 0.6f
                ? new Color32(0x1C, 0x1A, 0x20, 0xFF) : new Color32(0xF1, 0xE6, 0xD0, 0xFF);
        if (chipText != null)
            chipText.text = Amount.ToString();
    }

    public void SetHighlight(bool on, Color color)
    {
        if (background != null)
            background.color = on ? color : baseColor;
    }

    public void PlaceOn(Roulette roulette)
    {
        if (Amount <= 0)
            return;
        switch (betType)
        {
            case RouletteBetType.Number: roulette.BetNumber(value, Amount); break;
            case RouletteBetType.Red: roulette.BetColor(Color.red, Amount); break;
            case RouletteBetType.Black: roulette.BetColor(Color.black, Amount); break;
            case RouletteBetType.Odd: roulette.BetOdd(Amount); break;
            case RouletteBetType.Even: roulette.BetEven(Amount); break;
            case RouletteBetType.Low: roulette.BetLow(Amount); break;
            case RouletteBetType.High: roulette.BetHigh(Amount); break;
            case RouletteBetType.Dozen: roulette.BetDozen(value, Amount); break;
        }
    }

    public bool Wins(int pocket, Roulette roulette)
    {
        bool numbered = pocket >= 1 && pocket <= 36;
        switch (betType)
        {
            case RouletteBetType.Number: return pocket == value;
            case RouletteBetType.Red: return roulette.Pockets[pocket] == Color.red;
            case RouletteBetType.Black: return roulette.Pockets[pocket] == Color.black;
            case RouletteBetType.Odd: return numbered && pocket % 2 == 1;
            case RouletteBetType.Even: return numbered && pocket % 2 == 0;
            case RouletteBetType.Low: return numbered && pocket <= 18;
            case RouletteBetType.High: return numbered && pocket >= 19;
            case RouletteBetType.Dozen: return numbered && (pocket - 1) / 12 == value - 1;
            default: return false;
        }
    }
}
