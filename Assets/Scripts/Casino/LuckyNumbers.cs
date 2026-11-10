using System;
using System.Collections.Generic;

// The player's lucky numbers, as the LuckyNumberData assets that granted them (a boss's Lucky Chip, a memory item,
// the starting number), in the order they were earned. Each asset sets its number's fixed chance and payout.
// Roulette pockets: 0-36, and 37 for 00. Roulette works out the rest: normal pockets share what is left, and group
// bets that include lucky numbers are capped (Roulette.LuckyEdge).
public static class LuckyNumbers
{
    private static readonly List<LuckyNumberData> Earned = new();

    // Raised after every change.
    public static event Action Changed;

    // In the order they were earned.
    public static IReadOnlyList<LuckyNumberData> All => Earned;
    public static int Count => Earned.Count;

    // Domain reload is off, so statics must be reset by hand between Play sessions.
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Earned.Clear();
        Changed = null;
    }

    // The asset that made this pocket lucky, or null.
    public static LuckyNumberData Get(int number)
    {
        foreach (LuckyNumberData data in Earned)
            if (data.Number == number)
                return data;
        return null;
    }

    public static bool Contains(int number) => Get(number) != null;

    // The chance this lucky number was given (0 if it is not lucky).
    public static float ChanceOf(int number)
    {
        LuckyNumberData data = Get(number);
        return data != null ? data.Chance : 0f;
    }

    // What a winning bet on this lucky number gives back per chip, bet included (0 if it is not lucky).
    public static float PayoutOf(int number)
    {
        LuckyNumberData data = Get(number);
        return data != null ? data.Payout : 0f;
    }

    // Adds a lucky number. A later asset for the same pocket replaces the earlier one. False if already earned.
    public static bool Add(LuckyNumberData data)
    {
        if (data == null || Earned.Contains(data))
            return false;
        LuckyNumberData samePocket = Get(data.Number);
        if (samePocket != null)
            Earned.Remove(samePocket);
        Earned.Add(data);
        Changed?.Invoke();
        return true;
    }

    public static bool Remove(LuckyNumberData data)
    {
        if (!Earned.Remove(data))
            return false;
        Changed?.Invoke();
        return true;
    }

    public static void Clear()
    {
        if (Earned.Count == 0)
            return;
        Earned.Clear();
        Changed?.Invoke();
    }
}
