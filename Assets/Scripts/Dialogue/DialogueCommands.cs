using System;
using UnityEngine;
using Yarn.Unity;
using CharacterController = Assets.Scripts.CharacterController;

// What Yarn scripts can do to the game. Lives on the Dialogue prefab next to the DialogueRunner.
//
// Commands:
//   <<give_money 20>>           adds money to MoneyWallet
//   <<unlock_ability Dash>>     Dash, WallJump or DoubleJump: unlocks it like an ability pickup does
//   <<give_lucky_number 7>>     gives the lucky number with that pocket from the list below
//   <<signal open_gate>>        publishes DialogueSignal; a DialogueSignalListener in the scene reacts to it
// Functions (in <<if>> and {}):
//   money()                     the player's money
//   has_ability("Dash")         true if that ability is unlocked
//   has_lucky_number(7)         true if the player has that lucky number
[RequireComponent(typeof(DialogueRunner))]
public class DialogueCommands : MonoBehaviour
{
    [Tooltip("Lucky numbers a conversation can give with <<give_lucky_number N>>, found by their pocket number.")]
    [SerializeField] private LuckyNumberData[] luckyNumbers;
    private const string PlayerTag = "Player";

    private void Awake()
    {
        DialogueRunner runner = GetComponent<DialogueRunner>();
        runner.AddCommandHandler("give_money", (Action<int>)GiveMoney);
        runner.AddCommandHandler("unlock_ability", (Action<string>)UnlockAbility);
        runner.AddCommandHandler("give_lucky_number", (Action<int>)GiveLuckyNumber);
        runner.AddCommandHandler("signal", (Action<string>)Signal);
    }

    private void GiveMoney(int amount) => MoneyWallet.Add(amount);

    private static void Signal(string signalName) => EventBus.Publish(new DialogueSignal { Name = signalName });

    private void UnlockAbility(string abilityName)
    {
        if (!TryParseAbility(abilityName, out PlayerAbility ability))
            return;
        if (!TryGetPlayer(out CharacterController controller))
            Debug.LogWarning($"{name}: no CharacterController on the object tagged {PlayerTag}.", this);
        else if (!controller.IsUnlocked(ability))
            AbilityPickup.Grant(controller, ability);
    }

    private void GiveLuckyNumber(int number)
    {
        LuckyNumberData data = Array.Find(luckyNumbers, n => n != null && n.Number == number);
        if (data == null)
        {
            Debug.LogWarning($"{name}: <<give_lucky_number {number}>> - add that lucky number's asset to Lucky Numbers.", this);
            return;
        }
        data.Grant();
    }

    [YarnFunction("money")]
    public static int Money() => MoneyWallet.Balance;

    [YarnFunction("has_ability")]
    public static bool HasAbility(string abilityName)
    {
        return TryParseAbility(abilityName, out PlayerAbility ability)
            && TryGetPlayer(out CharacterController controller) && controller.IsUnlocked(ability);
    }

    [YarnFunction("has_lucky_number")]
    public static bool HasLuckyNumber(int number) => LuckyNumbers.Contains(number);

    private static bool TryGetPlayer(out CharacterController controller)
    {
        controller = null;
        GameObject player = GameObject.FindWithTag(PlayerTag);
        return player != null && player.TryGetComponent(out controller);
    }

    private static bool TryParseAbility(string abilityName, out PlayerAbility ability)
    {
        if (Enum.TryParse(abilityName, true, out ability))
            return true;
        Debug.LogWarning($"Yarn: unknown ability \"{abilityName}\". Use one of: {string.Join(", ", Enum.GetNames(typeof(PlayerAbility)))}.");
        return false;
    }
}
