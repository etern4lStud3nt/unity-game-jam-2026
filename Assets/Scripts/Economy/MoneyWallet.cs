using UnityEngine;

// The player's money, picked up in the level (enemy drops, coins lying around). Separate from the casino's
// ChipWallet; money is what the player brings to the casino. Every change goes out as MoneyChanged on the EventBus,
// so the HUD, sounds or a casino cashier can react without knowing about each other.
public static class MoneyWallet
{
    public static int Balance { get; private set; }

    // Domain reload is off, so statics must be reset by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Balance = 0;

    public static void Add(int amount)
    {
        if (amount <= 0)
            return;
        Balance += amount;
        Publish(amount);
    }

    // Takes the money if there is enough; false otherwise.
    public static bool TrySpend(int amount)
    {
        if (amount <= 0 || amount > Balance)
            return false;
        Balance -= amount;
        Publish(-amount);
        return true;
    }

    public static void Set(int amount)
    {
        int old = Balance;
        Balance = Mathf.Max(0, amount);
        Publish(Balance - old);
    }

    private static void Publish(int delta) => EventBus.Publish(new MoneyChanged { Balance = Balance, Delta = delta });
}
