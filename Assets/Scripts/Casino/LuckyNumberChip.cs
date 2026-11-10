using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// One gold lucky number chip in the player's hand (LuckyNumberHand). Reports hovering (the hand lifts it a little)
// and clicks (the hand brings it to the middle of the screen with its stats).
public class LuckyNumberChip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private TMP_Text numberText;
    [Tooltip("The chip's look; lifted on hover while this object (the hover area) stays put, so it doesn't flicker.")]
    [SerializeField] private RectTransform visual;

    public event Action<LuckyNumberChip, bool> Hovered;
    public event Action<LuckyNumberChip> Clicked;

    public LuckyNumberData Data { get; private set; }
    public RectTransform Rect => (RectTransform)transform;
    public RectTransform Visual => visual != null ? visual : Rect;

    public void Bind(LuckyNumberData data)
    {
        Data = data;
        if (numberText != null)
            numberText.text = Roulette.PocketLabel(data.Number);
        name = "Chip " + Roulette.PocketLabel(data.Number);
    }

    public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(this, true);

    public void OnPointerExit(PointerEventData eventData) => Hovered?.Invoke(this, false);

    public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);
}
