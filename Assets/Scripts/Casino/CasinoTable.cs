using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterController = Assets.Scripts.CharacterController;

// The roulette table. There is no real game: Interact plays the oldest lucky number the player has not played yet,
// the big wheel comes up on screen, spins and slows down, and the number's reward (LuckyNumberData > Reward) unlocks.
// A lucky number always wins. A number with Reward None spins too if it has a Table Message (a dud: the message shows
// instead of an ability); without one it is skipped. The small wheel on the table is only art.
// The spin itself is the wheel's own animation (its Animator); this only speeds it up and slows it to a stop.
public class CasinoTable : CasinoInteractable
{
    [Header("Spin screen")]
    [Tooltip("The on-screen spin (a canvas with the big wheel). Hidden until the player plays a number.")]
    [SerializeField] private GameObject spinScreen;
    [Tooltip("The big wheel on the spin screen. Its default state is the looping spin animation.")]
    [SerializeField] private Animator spinWheel;

    [Header("Spin")]
    [Tooltip("How long the wheel spins before the reward comes, in seconds.")]
    [SerializeField, Min(0.1f)] private float spinTime = 2.5f;
    [Tooltip("Animation speed when the spin starts: 1 = as the clip is made, 2 = twice as fast.")]
    [SerializeField, Min(0.01f)] private float startSpeed = 1.25f;
    [Tooltip("Animation speed just before it stops.")]
    [SerializeField, Min(0f)] private float endSpeed = 0.2f;
    [Tooltip("Pause on the stopped wheel before it goes away and the reward shows, in seconds.")]
    [SerializeField, Min(0f)] private float stopPause = 0.6f;
    [Tooltip("How long the wheel takes to pop in and out, in seconds.")]
    [SerializeField, Min(0.01f)] private float popTime = 0.15f;

    [Header("Nothing to play")]
    [SerializeField] private string noNumberTitle = "No lucky number";
    [SerializeField] private string noNumberHint = "Beat a boss to win one";

    [Header("Come-here arrow")]
    [Tooltip("Arrow above the table. Shown until the player first walks up to the table, and whenever they have a " +
             "lucky number to play; hidden while they stand at the table or the wheel spins.")]
    [SerializeField] private GameObject marker;

    // Lucky numbers already played at the table, for the whole run.
    private static readonly HashSet<LuckyNumberData> Played = new();
    // True once the player has walked up to the table this run.
    private static bool visited;

    // Domain reload is off, so the statics must be cleared by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Played.Clear();
        visited = false;
    }

    protected override void Start()
    {
        base.Start();
        if (spinScreen != null)
            spinScreen.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();
        if (Player != null)
            visited = true;
        if (marker != null)
            marker.SetActive(Player == null && !Busy && (!visited || NextToPlay() != null));
    }

    protected override void Use(Rigidbody2D player)
    {
        if (!player.TryGetComponent(out CharacterController controller))
            return;
        LuckyNumberData number = NextToPlay();
        if (number == null)
        {
            ScreenMessage.Show(noNumberTitle, noNumberHint);
            return;
        }
        StartCoroutine(Play(number, controller));
    }

    // The oldest earned lucky number the table can play (an ability or a dud message) that has not been played yet.
    private static LuckyNumberData NextToPlay()
    {
        foreach (LuckyNumberData data in LuckyNumbers.All)
            if (data.PlayableAtTable && !Played.Contains(data))
                return data;
        return null;
    }

    private IEnumerator Play(LuckyNumberData number, CharacterController controller)
    {
        Busy = true;
        Played.Add(number);
        InputManager input = InputManager.Instance;
        if (input != null)
            input.BlockGameplay();
        try
        {
            if (spinScreen != null)
                spinScreen.SetActive(true);
            if (spinWheel != null)
                spinWheel.speed = startSpeed;
            yield return ScalePop.Run(spinWheel != null ? spinWheel.transform : null, 0f, 1f, popTime);
            yield return Spin();
            yield return new WaitForSeconds(stopPause);
            yield return ScalePop.Run(spinWheel != null ? spinWheel.transform : null, 1f, 0f, popTime);
            if (number.Reward == LuckyNumberReward.None)
                ScreenMessage.Show(number.TableMessage, number.TableMessageHint);
            else if (controller != null)
                AbilityPickup.Grant(controller, ToAbility(number.Reward));
        }
        finally
        {
            if (spinScreen != null)
                spinScreen.SetActive(false);
            if (input != null)
                input.UnblockGameplay();
            Busy = false;
        }
    }

    // Plays the wheel's animation from the start, fast at first and slowing to a stop (ease out), then holds it.
    private IEnumerator Spin()
    {
        if (spinWheel == null)
        {
            yield return new WaitForSeconds(spinTime);
            yield break;
        }

        spinWheel.Rebind();
        for (float t = 0f; t < spinTime; t += Time.deltaTime)
        {
            float k = t / spinTime;
            spinWheel.speed = Mathf.Lerp(startSpeed, endSpeed, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        spinWheel.speed = 0f;
    }

    private static PlayerAbility ToAbility(LuckyNumberReward reward)
    {
        switch (reward)
        {
            case LuckyNumberReward.WallJump: return PlayerAbility.WallJump;
            case LuckyNumberReward.DoubleJump: return PlayerAbility.DoubleJump;
            default: return PlayerAbility.Dash;
        }
    }
}
