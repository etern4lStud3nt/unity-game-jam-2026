using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CharacterController = Assets.Scripts.CharacterController;

// The roulette table. There is no real game: Interact plays the oldest lucky number the player has not played yet,
// the big wheel comes up on screen, spins and slows down, and the number's reward (LuckyNumberData > Reward) unlocks.
// A lucky number always wins. Numbers whose reward is None are skipped. The small wheel on the table is only art.
public class CasinoTable : CasinoInteractable
{
    [Header("Spin screen")]
    [Tooltip("The on-screen spin (a canvas with the big wheel). Hidden until the player plays a number.")]
    [SerializeField] private GameObject spinScreen;
    [Tooltip("The big wheel on the spin screen; its sprite is stepped through the frames.")]
    [SerializeField] private Image spinWheel;
    [Tooltip("The spin, one full turn, in order. Played in a loop while spinning.")]
    [SerializeField] private Sprite[] spinFrames;

    [Header("Spin")]
    [Tooltip("How long the wheel spins before the reward comes, in seconds.")]
    [SerializeField, Min(0.1f)] private float spinTime = 2.5f;
    [Tooltip("Frames per second when the spin starts.")]
    [SerializeField, Min(1f)] private float startSpeed = 30f;
    [Tooltip("Frames per second just before it stops.")]
    [SerializeField, Min(0.1f)] private float endSpeed = 3f;
    [Tooltip("Pause on the stopped wheel before it goes away and the reward shows, in seconds.")]
    [SerializeField, Min(0f)] private float stopPause = 0.6f;
    [Tooltip("How long the wheel takes to pop in and out, in seconds.")]
    [SerializeField, Min(0.01f)] private float popTime = 0.15f;

    [Header("Nothing to play")]
    [SerializeField] private string noNumberTitle = "No lucky number";
    [SerializeField] private string noNumberHint = "Beat a boss to win one";

    // Lucky numbers already played at the table, for the whole run.
    private static readonly HashSet<LuckyNumberData> Played = new();

    // Domain reload is off, so the static must be cleared by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Played.Clear();

    protected override void Start()
    {
        base.Start();
        if (spinScreen != null)
            spinScreen.SetActive(false);
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

    // The oldest earned lucky number with a reward that has not been played yet.
    private static LuckyNumberData NextToPlay()
    {
        foreach (LuckyNumberData data in LuckyNumbers.All)
            if (data.Reward != LuckyNumberReward.None && !Played.Contains(data))
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
            ShowFrame(0);
            if (spinScreen != null)
                spinScreen.SetActive(true);
            yield return Pop(0f, 1f);
            yield return Spin();
            yield return new WaitForSeconds(stopPause);
            yield return Pop(1f, 0f);
            if (controller != null)
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

    // Grows (or shrinks) the big wheel with a small overshoot, so it pops onto the screen.
    private IEnumerator Pop(float from, float to)
    {
        if (spinWheel == null)
            yield break;
        Transform wheel = spinWheel.transform;
        for (float t = 0f; t < popTime; t += Time.deltaTime)
        {
            float k = t / popTime;
            float eased = to > from ? 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f) : 1f - k * k;
            wheel.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, to > from ? eased : 1f - eased);
            yield return null;
        }
        wheel.localScale = Vector3.one * to;
    }

    // Steps through the frames, starting fast and slowing to a stop (ease out).
    private IEnumerator Spin()
    {
        if (spinFrames == null || spinFrames.Length == 0)
        {
            yield return new WaitForSeconds(spinTime);
            yield break;
        }

        float frame = 0f;
        for (float t = 0f; t < spinTime; t += Time.deltaTime)
        {
            float k = t / spinTime;
            float speed = Mathf.Lerp(startSpeed, endSpeed, 1f - (1f - k) * (1f - k));
            frame += speed * Time.deltaTime;
            ShowFrame(Mathf.FloorToInt(frame));
            yield return null;
        }
    }

    private void ShowFrame(int index)
    {
        if (spinWheel != null && spinFrames != null && spinFrames.Length > 0)
            spinWheel.sprite = spinFrames[index % spinFrames.Length];
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
