using UnityEngine;

namespace Bosses
{
    // Tuning for the Grandma fight. Her health is on her Health component; everything else is here.
    [CreateAssetMenu(menuName = "Lucky Number/Grandma Stats", fileName = "GrandmaStats")]
    public class GrandmaStats : ScriptableObject
    {
        [field: Header("Sleep")]
        [field: Tooltip("Fewest needle attacks (all needles together) before she falls asleep.")]
        [field: SerializeField, Min(1)] public int MinAttacksBeforeSleep { get; private set; } = 6;
        [field: Tooltip("Most needle attacks (all needles together) before she falls asleep.")]
        [field: SerializeField, Min(1)] public int MaxAttacksBeforeSleep { get; private set; } = 10;
        [field: Tooltip("Seconds she sleeps. The needles lie still meanwhile.")]
        [field: SerializeField, Min(0f)] public float SleepTime { get; private set; } = 3f;

        [field: Header("Needles: how many")]
        [field: Tooltip("Needles out when the fight starts.")]
        [field: SerializeField, Min(1)] public int StartingNeedles { get; private set; } = 1;
        [field: Tooltip("Needle attacks before each further needle joins, counted from the previous one joining: " +
                        "2, 1 = the second needle after 2 attacks, the third after 1 more. The second-half needles " +
                        "do not wait for this; they all come at once.")]
        [field: SerializeField] public int[] AttacksBeforeNextNeedle { get; private set; } = { 2, 1 };

        [field: Header("Needles: waiting")]
        [field: Tooltip("Shortest time (seconds) between two needles starting an attack, so they do not all come at once.")]
        [field: SerializeField, Min(0f)] public float AttackGap { get; private set; } = 0.8f;
        [field: Tooltip("Shortest time (seconds) a needle waits at its spot before attacking again.")]
        [field: SerializeField, Min(0f)] public float MinWait { get; private set; } = 0.4f;
        [field: Tooltip("Longest time (seconds) a needle waits at its spot before attacking again.")]
        [field: SerializeField, Min(0f)] public float MaxWait { get; private set; } = 1.2f;
        [field: Tooltip("Units a waiting needle drifts left and right of its spot.")]
        [field: SerializeField, Min(0f)] public float HoverDrift { get; private set; } = 1f;
        [field: Tooltip("Units a waiting needle bobs up and down.")]
        [field: SerializeField, Min(0f)] public float HoverBob { get; private set; } = 0.5f;

        [field: Header("Needles: attack")]
        [field: Tooltip("Dives in a repeated attack: after sticking, the needle pulls back, aims again and dives again.")]
        [field: SerializeField, Min(1)] public int ChainDives { get; private set; } = 2;
        [field: Tooltip("Chance (0-1) that an attack repeats (Chain Dives dives). Otherwise it dives once and flies back.")]
        [field: SerializeField, Range(0f, 1f)] public float ChainChance { get; private set; } = 0.3f;
        [field: Tooltip("Seconds a needle shakes before each dive (the warning).")]
        [field: SerializeField, Min(0f)] public float WindupTime { get; private set; } = 0.35f;
        [field: Tooltip("Seconds at the end of the shake during which the aim no longer follows the player.")]
        [field: SerializeField, Min(0f)] public float AimLockTime { get; private set; } = 0.1f;
        [field: Tooltip("0 = dives at where the player is, 1 = at where they will be if they keep running.")]
        [field: SerializeField, Range(0f, 1f)] public float Prediction { get; private set; } = 0.5f;
        [field: Tooltip("Dive speed in units per second.")]
        [field: SerializeField, Min(0.1f)] public float DiveSpeed { get; private set; } = 30f;
        [field: Tooltip("Longest dive in units; it normally ends earlier, stuck in the ground or a wall.")]
        [field: SerializeField, Min(0f)] public float MaxDiveDistance { get; private set; } = 30f;
        [field: Tooltip("Units the point sinks into the ground.")]
        [field: SerializeField, Min(0f)] public float StickDepth { get; private set; } = 0.3f;
        [field: Tooltip("Seconds a needle stays stuck between the dives of one attack.")]
        [field: SerializeField, Min(0f)] public float ChainStuckTime { get; private set; } = 0.15f;
        [field: Tooltip("Units a needle pulls back out of the ground before its next dive.")]
        [field: SerializeField, Min(0f)] public float PullBackDistance { get; private set; } = 1.5f;
        [field: Tooltip("Speed (units per second) of that pull back.")]
        [field: SerializeField, Min(0.1f)] public float PullBackSpeed { get; private set; } = 20f;
        [field: Tooltip("Seconds a needle stays stuck after the last dive of an attack, before flying back.")]
        [field: SerializeField, Min(0f)] public float StuckTime { get; private set; } = 0.5f;
        [field: Tooltip("Speed (units per second) of a needle flying back to its spot.")]
        [field: SerializeField, Min(0.1f)] public float ReturnSpeed { get; private set; } = 12f;
        [field: Tooltip("Speed (units per second) of a needle dropping to the ground when she falls asleep.")]
        [field: SerializeField, Min(0.1f)] public float FallSpeed { get; private set; } = 14f;

        [field: Header("Hit while awake")]
        [field: Tooltip("Hitting her while she is awake sends the nearest free needle (waiting or flying back) at the " +
                        "player straight away (it still shakes first). At most once per this many seconds; 0 = every hit.")]
        [field: SerializeField, Min(0f)] public float CounterCooldown { get; private set; } = 0f;

        [field: Header("Walking")]
        [field: Tooltip("Units per second she walks.")]
        [field: SerializeField, Min(0f)] public float WalkSpeed { get; private set; } = 3.5f;
        [field: Tooltip("When the player comes this close (units, sideways) she walks to the far end of the arena.")]
        [field: SerializeField, Min(0f)] public float KeepAwayDistance { get; private set; } = 6f;
        [field: Tooltip("Units she keeps from the arena's left and right edges. Keep it wider than her needles' spots " +
                        "reach to the side, or a needle can end up waiting inside a wall.")]
        [field: SerializeField, Min(0f)] public float EdgeMargin { get; private set; } = 4f;

        [field: Header("Escape jump")]
        [field: Tooltip("When the player comes this close (units, sideways) she jumps to the far end of the arena, " +
                        "over the player if she has to.")]
        [field: SerializeField, Min(0f)] public float JumpTriggerDistance { get; private set; } = 3.5f;
        [field: Tooltip("Seconds she crouches and shakes before jumping (the warning).")]
        [field: SerializeField, Min(0f)] public float JumpWarningTime { get; private set; } = 0.4f;
        [field: Tooltip("Seconds in the air.")]
        [field: SerializeField, Min(0.1f)] public float JumpTime { get; private set; } = 0.7f;
        [field: Tooltip("Height of the jump in units.")]
        [field: SerializeField, Min(0f)] public float JumpHeight { get; private set; } = 4f;
        [field: Tooltip("Seconds after landing before she can jump again.")]
        [field: SerializeField, Min(0f)] public float JumpCooldown { get; private set; } = 1f;

        [field: Header("Second half")]
        [field: Tooltip("At or below this share of her health the second-half needles join and all needles wait less. 0 = never.")]
        [field: SerializeField, Range(0f, 1f)] public float Phase2Health { get; private set; } = 0.5f;
        [field: Tooltip("Waiting times and the attack gap are multiplied by this in the second half.")]
        [field: SerializeField, Range(0.1f, 1f)] public float Phase2WaitScale { get; private set; } = 0.6f;
        [field: Tooltip("Fewest needle attacks before she falls asleep in the second half (from her first nap there).")]
        [field: SerializeField, Min(1)] public int Phase2MinAttacksBeforeSleep { get; private set; } = 5;
        [field: Tooltip("Most needle attacks before she falls asleep in the second half (from her first nap there).")]
        [field: SerializeField, Min(1)] public int Phase2MaxAttacksBeforeSleep { get; private set; } = 8;

        [field: Header("Death")]
        [field: Tooltip("Seconds she takes to fade out after the last hit.")]
        [field: SerializeField, Min(0f)] public float DeathFadeTime { get; private set; } = 0.8f;

        private void OnValidate()
        {
            MaxAttacksBeforeSleep = Mathf.Max(MaxAttacksBeforeSleep, MinAttacksBeforeSleep);
            MaxWait = Mathf.Max(MaxWait, MinWait);
            Phase2MaxAttacksBeforeSleep = Mathf.Max(Phase2MaxAttacksBeforeSleep, Phase2MinAttacksBeforeSleep);
        }
    }
}
