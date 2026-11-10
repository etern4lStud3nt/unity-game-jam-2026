using System.Collections.Generic;
using UnityEngine;

// American roulette (0, 00 and 1-36). Collects bets, picks a winning pocket and works out the payout.
// Pocket 37 is the double zero.
public class Roulette : MonoBehaviour
{
    public const int DoubleZero = 37;



    public Dictionary<int, Color> Pockets { get; private set; } = new()
    {
        { 0, Color.green },
        { 1, Color.red },
        { 2, Color.black },
        { 3, Color.red },
        { 4, Color.black },
        { 5, Color.red },
        { 6, Color.black },
        { 7, Color.red },
        { 8, Color.black },
        { 9, Color.red },
        { 10, Color.black },
        { 11, Color.black },
        { 12, Color.red },
        { 13, Color.black },
        { 14, Color.red },
        { 15, Color.black },
        { 16, Color.red },
        { 17, Color.black },
        { 18, Color.red },
        { 19, Color.red },
        { 20, Color.black },
        { 21, Color.red },
        { 22, Color.black },
        { 23, Color.red },
        { 24, Color.black },
        { 25, Color.red },
        { 26, Color.black },
        { 27, Color.red },
        { 28, Color.black },
        { 29, Color.black },
        { 30, Color.red },
        { 31, Color.black },
        { 32, Color.red },
        { 33, Color.black },
        { 34, Color.red },
        { 35, Color.black },
        { 36, Color.red },
        { DoubleZero, Color.green }
    };

    // Pockets in the order they sit around an American wheel, clockwise from 0.
    public static readonly int[] WheelOrder =
    {
        0, 28, 9, 26, 30, 11, 7, 20, 32, 17, 5, 22, 34, 15, 3, 24, 36, 13, 1,
        DoubleZero, 27, 10, 25, 29, 12, 8, 19, 31, 18, 6, 21, 33, 16, 4, 23, 35, 14, 2
    };

    public struct Bet
    {
        // You can bet on a single number, a color, or a list of numbers.
        public List<int> Numbers { get; private set; }
        public Color? Color { get; private set; }
        public int Chips { get; private set; }

        public Bet(int Number, int Chips)
        {
            Numbers = new List<int> { Number };
            Color = null;
            this.Chips = Chips;
        }

        public Bet(Color Color, int Chips)
        {
            Numbers = new List<int>();
            this.Color = Color;
            this.Chips = Chips;
        }

        public Bet(List<int> Numbers, int Chips)
        {
            this.Numbers = Numbers;
            Color = null;
            this.Chips = Chips;
        }
    }

    private readonly List<Bet> bets = new();

    public IReadOnlyList<Bet> Bets => bets;

    public int TotalBet
    {
        get
        {
            int total = 0;
            foreach (Bet bet in bets)
                total += bet.Chips;
            return total;
        }
    }

    // "00" for the double zero, the number otherwise.
    public static string PocketLabel(int pocket) => pocket == DoubleZero ? "00" : pocket.ToString();

    public void ClearBets() => bets.Clear();

    public void BetNumber(int number, int chips)
    {
        Bet newBet = new(number, chips);
        bets.Add(newBet);
    }

    public void BetColor(Color color, int chips)
    {
        Bet newBet = new(color, chips);
        bets.Add(newBet);
    }

    public void BetLow(int chips) => bets.Add(new Bet(Range(1, 18), chips));

    public void BetHigh(int chips) => bets.Add(new Bet(Range(19, 36), chips));

    // dozen 1 = 1-12, 2 = 13-24, 3 = 25-36.
    public void BetDozen(int dozen, int chips)
    {
        int first = (Mathf.Clamp(dozen, 1, 3) - 1) * 12 + 1;
        bets.Add(new Bet(Range(first, first + 11), chips));
    }

    public void BetOdd(int chips)
    {
        List<int> oddNums = new();
        for (int n = 1; n <= 36; n++)
            if (n % 2 != 0)
                oddNums.Add(n);
        bets.Add(new Bet(oddNums, chips));
    }

    public void BetEven(int chips)
    {
        List<int> evenNums = new();
        for (int n = 1; n <= 36; n++)
            if (n % 2 == 0)
                evenNums.Add(n);
        bets.Add(new Bet(evenNums, chips));
    }

    // Picks the winning pocket. Luck only works for lucky numbers the player bet on: each of those, highest chance
    // first, gets one roll at its own chance (LuckyNumberData.Chance); the first that succeeds wins. If none does (or
    // no lucky number was bet on), the wheel spins normally.
    public int Spin()
    {
        foreach (int number in LuckyNumbersBetOn())
            if (Random.value < LuckyNumbers.ChanceOf(number))
                return number;
        return Random.Range(0, 38);
    }

    // Chance (0-1) that the next spin lands on this pocket, given the bets on the table.
    public float Chance(int pocket)
    {
        float chance = 0f;
        float missedAll = 1f;
        foreach (int number in LuckyNumbersBetOn())
        {
            float c = LuckyNumbers.ChanceOf(number);
            if (number == pocket)
                chance += missedAll * c;
            missedAll *= 1f - c;
        }
        return chance + missedAll / 38f;
    }

    // Lucky numbers with a bet on them, in the order their luck is tried: highest chance first.
    private List<int> LuckyNumbersBetOn()
    {
        var numbers = new List<int>();
        foreach (Bet bet in bets)
            if (bet.Numbers.Count == 1 && LuckyNumbers.Contains(bet.Numbers[0]) && !numbers.Contains(bet.Numbers[0]))
                numbers.Add(bet.Numbers[0]);
        numbers.Sort((a, b) =>
        {
            int byChance = LuckyNumbers.ChanceOf(b).CompareTo(LuckyNumbers.ChanceOf(a));
            return byChance != 0 ? byChance : a.CompareTo(b);
        });
        return numbers;
    }

    // Chance that the winning pocket is one of these numbers (or this color).
    public float Chance(Bet bet)
    {
        float total = 0f;
        for (int p = 0; p < 38; p++)
            if (Covers(bet, p))
                total += Chance(p);
        return total;
    }

    // What a winning bet gives back per chip, stake included. A single lucky number pays its own payout (from its
    // LuckyNumberData asset); everything else pays as in a real casino: color 2x, a bet on N numbers 36/N x.
    // A bet on a lucky number also makes its color, dozen, odd/even... likelier; those still pay normally, on purpose.
    public float ReturnPerChip(Bet bet)
    {
        if (bet.Numbers.Count == 1 && LuckyNumbers.Contains(bet.Numbers[0]))
            return LuckyNumbers.PayoutOf(bet.Numbers[0]);
        return bet.Numbers.Count == 0 ? 2f : 36f / bet.Numbers.Count;
    }

    // Net result of all bets for this winning pocket: winnings minus every chip that was bet.
    public int GetPayout(int winningNumber)
    {
        int total = 0;
        foreach (Bet bet in bets)
        {
            total -= bet.Chips;
            if (Covers(bet, winningNumber))
                total += Mathf.FloorToInt(bet.Chips * ReturnPerChip(bet));
        }

        return total;
    }

    private bool Covers(Bet bet, int pocket) =>
        bet.Numbers.Count == 0 ? bet.Color == Pockets[pocket] : bet.Numbers.Contains(pocket);

    private static List<int> Range(int first, int last)
    {
        List<int> numbers = new();
        for (int n = first; n <= last; n++)
            numbers.Add(n);
        return numbers;
    }
}
