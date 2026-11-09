using UnityEngine;
using UnityEngine.UI;

// A chip in the rack. Clicking it selects its value for the next bets (Roulette_UI listens to the Button).
[RequireComponent(typeof(Button))]
public class RouletteChip : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;
    [Tooltip("Shown while this chip is selected.")]
    [SerializeField] private GameObject selectedMark;
    [Tooltip("Tint used for this chip on the table. Empty = the chip image's color.")]
    [SerializeField] private Image chipImage;

    public int Value => value;
    public Button Button => GetComponent<Button>();
    public Color Color => chipImage != null ? chipImage.color : Color.white;

    public void SetSelected(bool selected)
    {
        if (selectedMark != null)
            selectedMark.SetActive(selected);
    }
}
