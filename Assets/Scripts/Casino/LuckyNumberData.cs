using UnityEngine;

// What spending a lucky number at the casino table gives.
public enum LuckyNumberReward
{
    None,
    Dash,
    WallJump,
    DoubleJump,
}

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
    [Tooltip("Chance that the wheel lands on this number when the player bets on it: 0.9 = nine spins in ten. " +
             "Other lucky numbers do not change it. A normal pocket is about 0.026.")]
    [SerializeField, Range(0.03f, 0.99f)] private float chance = 0.5f;
    [Tooltip("What a winning bet on this number gives back, bet included: 5 = bet 10 chips, get 50 back. " +
             "A normal number pays 36.")]
    [SerializeField, Min(1f)] private float payout = 6f;
    [Tooltip("Read only: what the player wins or loses on average per chip bet on this number (chance x payout - 1).")]
    [SerializeField] private string averagePerChip;
    [Tooltip("A short memory: why this is their lucky number. Empty = no story section on the card.")]
    [SerializeField, TextArea(3, 8)] private string story;
    [Tooltip("What playing this number at the casino table unlocks. None = the table ignores it, unless it has a Table Message.")]
    [SerializeField] private LuckyNumberReward reward;
    [Tooltip("Only for Reward None: the table still spins for this number, then shows this instead of an ability " +
             "(a dud, e.g. \"Not so lucky.\"). Empty = the table ignores the number.")]
    [SerializeField] private string tableMessage;
    [Tooltip("Second, smaller line under the Table Message. Optional.")]
    [SerializeField] private string tableMessageHint;

    public string DisplayName => displayName;
    public string GivenBy => givenBy;
    public int Number => number;
    public float Chance => chance;
    public float Payout => payout;
    public string Story => story;
    public LuckyNumberReward Reward => reward;
    public string TableMessage => tableMessage;
    public string TableMessageHint => tableMessageHint;

    // True if the casino table has something to do with this number: an ability, or a dud message.
    public bool PlayableAtTable => reward != LuckyNumberReward.None || !string.IsNullOrEmpty(tableMessage);

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
