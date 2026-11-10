using UnityEngine;

// One lucky number the player can earn (a boss's Lucky Chip, a memory item, the starting number...).
// Create one asset per source and hand it to whatever grants it; nothing about the numbers lives in code.
[CreateAssetMenu(menuName = "Lucky Number/Lucky Number", fileName = "Lucky_New")]
public class LuckyNumberData : ScriptableObject
{
    [Tooltip("Shown to the player, e.g. \"Grandma's Chip\".")]
    [SerializeField] private string displayName;
    [Tooltip("Who it came from, shown as \"FROM ...\". Empty = the line is hidden.")]
    [SerializeField] private string givenBy;
    [Tooltip("Roulette pocket: 0-36, or 37 for 00.")]
    [SerializeField, Range(0, 37)] private int number = 7;
    [Tooltip("Chance that the wheel lands on this number, whatever other lucky numbers the player has. " +
             "0.25 = one spin in four. A normal pocket is about 0.026.")]
    [SerializeField, Range(0.03f, 0.9f)] private float chance = 0.2f;
    [Tooltip("What a winning bet on this number gives back, bet included: 5 = bet 10 chips, get 50 back. " +
             "A normal number pays 36.")]
    [SerializeField, Min(1f)] private float payout = 6f;
    [Tooltip("Read only: what the player wins or loses on average per chip bet on this number (chance x payout - 1).")]
    [SerializeField] private string averagePerChip;
    [Tooltip("A short memory: why this is their lucky number. Empty = no story section on the card.")]
    [SerializeField, TextArea(3, 8)] private string story;

    public string DisplayName => displayName;
    public string GivenBy => givenBy;
    public int Number => number;
    public float Chance => chance;
    public float Payout => payout;
    public string Story => story;

    // Gives the player this lucky number.
    public void Grant() => LuckyNumbers.Add(this);

    // For testing in Play mode: right-click the asset's header in the Inspector > Grant Now.
    [ContextMenu("Grant Now")]
    private void GrantNow()
    {
        if (Application.isPlaying)
            Grant();
        else
            Debug.Log("Grant Now only works in Play mode.", this);
    }

    private void OnValidate()
    {
        float average = chance * payout - 1f;
        averagePerChip = (average >= 0f ? "+" : "") + Mathf.RoundToInt(average * 100f) + "%";
    }
}
