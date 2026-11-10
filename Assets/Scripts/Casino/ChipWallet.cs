using System;
using UnityEngine;

// The player's casino chips, shared by every casino game (roulette, slots, the casino store).
public static class ChipWallet
{
    public static int Balance { get; private set; }

    // New balance after every change.
    public static event Action<int> Changed;

    // Domain reload is off, so statics must be reset by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Balance = 0;
        Changed = null;
    }

    public static void Add(int chips)
    {
        if (chips <= 0)
            return;
        Balance += chips;
        Changed?.Invoke(Balance);
    }

    // Takes the chips if there are enough; false otherwise.
    public static bool TrySpend(int chips)
    {
        if (chips <= 0 || chips > Balance)
            return false;
        Balance -= chips;
        Changed?.Invoke(Balance);
        return true;
    }

    public static void Set(int chips)
    {
        Balance = Mathf.Max(0, chips);
        Changed?.Invoke(Balance);
    }
}
