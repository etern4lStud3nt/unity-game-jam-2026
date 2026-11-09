using System.Collections.Generic;
using UnityEngine;

public class Roulette : MonoBehaviour
{
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
        { 37, Color.green } // 37 is the double zero pocket
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

    private readonly List<Bet> bets;

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

    public void BetLow(int chips)
    {
        List<int> lowNums = new();
        foreach (int n in Pockets.Keys)
        {
            if (n <= 18 && n >= 1)
                lowNums.Add(n);
        }

        Bet newBet = new(lowNums, chips);
        bets.Add(newBet);
    }

    public void BetHigh(int chips)
    {
        List<int> lowNums = new();
        foreach (int n in Pockets.Keys)
        {
            if (n <= 19 && n >= 36)
                lowNums.Add(n);
        }

        Bet newBet = new(lowNums, chips);
        bets.Add(newBet);
    }

    public void BetOdd(int chips)
    {
        List<int> oddNums = new();
        foreach (int n in Pockets.Keys)
        {
            if (n == 0 || n == 37)
                continue;

            if (n % 2 != 0)
                oddNums.Add(n);
        }

        Bet newBet = new(oddNums, chips);
        bets.Add(newBet);
    }

    public void BetEven(int chips)
    {
        List<int> evenNums = new();
        foreach (int n in Pockets.Keys)
        {
            if (n == 0 || n == 37)
                continue;

            if (n % 2 == 0)
                evenNums.Add(n);
        }

        Bet newBet = new(evenNums, chips);
        bets.Add(newBet);
    }

    public int Spin()
    {
        return Random.Range(0, 38);
    }

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
                    total += bet.Chips + bet.Chips * 2;
            }
            // Number bet
            else
            {
                foreach (int n in bet.Numbers)
                {
                    if (n == winningNumber)
                    {
                        total += bet.Chips + (bet.Chips / bet.Numbers.Count) * 35;
                        break;
                    }
                }
            }

        }

        return total;
    }
}
