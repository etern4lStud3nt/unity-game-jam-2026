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

    public int Spin()
    {
        return Random.Range(0, 38);
    }

    // Net result of all bets for this winning pocket: winnings minus every chip that was bet.
    // Payouts as in a real casino: a color pays 1 to 1, and a bet on N numbers pays (36 / N - 1) to 1
    // (single number 35 to 1, dozen 2 to 1, odd/even and low/high 1 to 1).
    public int GetPayout(int winningNumber)
    {
        int total = 0;
        foreach (Bet bet in bets)
        {
            total -= bet.Chips;
            // Color Bet
            if (bet.Numbers.Count == 0)
            {
                if (bet.Color == Pockets[winningNumber])
                    total += bet.Chips * 2;
            }
            // Number bet
            else if (bet.Numbers.Contains(winningNumber))
            {
                total += bet.Chips * 36 / bet.Numbers.Count;
            }
        }

        return total;
    }

    private static List<int> Range(int first, int last)
    {
        List<int> numbers = new();
        for (int n = first; n <= last; n++)
            numbers.Add(n);
        return numbers;
    }
}
