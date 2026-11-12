using UnityEngine;

namespace Bosses
{
    // Tuning for the Old Self fight. Health is on the Health component; everything else is here.
    // Jumps are set by height and time: the boss works out the gravity it needs for each one.
    [CreateAssetMenu(menuName = "Lucky Number/Old Self Stats", fileName = "OldSelfStats")]
    public class OldSelfStats : ScriptableObject
    {
        [field: Header("Between attacks")]
        [field: Tooltip("Shortest pause (seconds) before it picks the next move.")]
        [field: SerializeField, Min(0f)] public float MinIdleTime { get; private set; } = 0.15f;
        [field: Tooltip("Longest pause (seconds) before it picks the next move.")]
        [field: SerializeField, Min(0f)] public float MaxIdleTime { get; private set; } = 0.4f;
        [field: Tooltip("Chance (0-1) that it hops to a new spot before attacking.")]
        [field: SerializeField, Range(0f, 1f)] public float HopChance { get; private set; } = 0.5f;
        [field: Tooltip("Height of a hop in units.")]
        [field: SerializeField, Min(0.1f)] public float HopHeight { get; private set; } = 2.5f;
        [field: Tooltip("Seconds a hop takes from take-off to landing.")]
        [field: SerializeField, Min(0.1f)] public float HopTime { get; private set; } = 0.5f;
        [field: Tooltip("Shortest and longest sideways distance of a hop.")]
        [field: SerializeField] public Vector2 HopDistance { get; private set; } = new Vector2(3f, 7f);
        [field: Tooltip("Lowest ground (units below where it stands) it may hop down onto. It never hops into a pit.")]
        [field: SerializeField, Min(0f)] public float HopMaxDrop { get; private set; } = 5.5f;
        [field: Tooltip("Units it keeps from the arena's left and right edges when picking where to hop.")]
        [field: SerializeField, Min(0f)] public float EdgeMargin { get; private set; } = 1.5f;
        [field: Tooltip("Seconds it stands open after an attack: the player's moment to hit it.")]
        [field: SerializeField, Min(0f)] public float RecoverTime { get; private set; } = 0.6f;
        [field: Tooltip("Height of the sprite (1 = normal) while it crouches before a lunge or staggers.")]
        [field: SerializeField, Range(0.3f, 1f)] public float CrouchScale { get; private set; } = 0.8f;

        [field: Header("Choosing an attack (weights; 0 = never)")]
        [field: SerializeField, Min(0f)] public float LungeWeight { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float AirDiveWeight { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float ThrowWeight { get; private set; } = 1f;
        [field: Tooltip("Only picked when the player is within Slash Range.")]
        [field: SerializeField, Min(0f)] public float SlashWeight { get; private set; } = 2f;
        [field: Tooltip("Units (sideways) within which it may slash.")]
        [field: SerializeField, Min(0f)] public float SlashRange { get; private set; } = 3f;

        [field: Header("Lunge (dash along the ground)")]
        [field: Tooltip("Seconds it crouches and shakes before dashing (the warning).")]
        [field: SerializeField, Min(0f)] public float LungeWindup { get; private set; } = 0.45f;
        [field: SerializeField, Min(0.1f)] public float LungeSpeed { get; private set; } = 22f;
        [field: Tooltip("Longest dash in units; a wall ends it sooner.")]
        [field: SerializeField, Min(0f)] public float LungeDistance { get; private set; } = 12f;
        [field: Tooltip("Camera shake when the dash ends. 0 = none.")]
        [field: SerializeField, Min(0f)] public float LungeShake { get; private set; } = 0.2f;

        [field: Header("Air dive (jump, hang, dive at the player)")]
        [field: SerializeField, Min(0.1f)] public float DiveJumpHeight { get; private set; } = 5f;
        [field: Tooltip("Seconds from take-off to the top of the jump.")]
        [field: SerializeField, Min(0.05f)] public float DiveRiseTime { get; private set; } = 0.35f;
        [field: Tooltip("Seconds it hangs at the top, shaking and aiming (the warning).")]
        [field: SerializeField, Min(0f)] public float DiveHangTime { get; private set; } = 0.4f;
        [field: Tooltip("Seconds at the end of the hang during which the aim no longer follows the player.")]
        [field: SerializeField, Min(0f)] public float DiveAimLockTime { get; private set; } = 0.1f;
        [field: Tooltip("0 = dives at where the player is, 1 = at where they will be if they keep running.")]
        [field: SerializeField, Range(0f, 1f)] public float DivePrediction { get; private set; } = 0.5f;
        [field: SerializeField, Min(0.1f)] public float DiveSpeed { get; private set; } = 28f;
        [field: Tooltip("How steeply it must dive at least: 0 = it may dive flat sideways, 1 = only straight down.")]
        [field: SerializeField, Range(0f, 1f)] public float DiveMinDownward { get; private set; } = 0.3f;
        [field: Tooltip("Camera shake when it hits the ground. 0 = none.")]
        [field: SerializeField, Min(0f)] public float DiveShake { get; private set; } = 0.6f;

        [field: Header("Throw (flies out and comes back)")]
        [field: Tooltip("Seconds it shakes before throwing (the warning).")]
        [field: SerializeField, Min(0f)] public float ThrowWindup { get; private set; } = 0.4f;
        [field: Tooltip("Units the thrown object flies out before turning back.")]
        [field: SerializeField, Min(0.5f)] public float ThrowDistance { get; private set; } = 16f;
        [field: Tooltip("Seconds to fly out (it slows down at the far end).")]
        [field: SerializeField, Min(0.05f)] public float ThrowOutTime { get; private set; } = 0.7f;
        [field: Tooltip("Speed (units per second) when it flies back to the boss.")]
        [field: SerializeField, Min(0.1f)] public float ThrowReturnSpeed { get; private set; } = 18f;
        [field: Tooltip("Degrees per second it spins.")]
        [field: SerializeField] public float ThrowSpin { get; private set; } = 900f;
        [field: Tooltip("Size of the thrown object (multiplies its scale in the prefab).")]
        [field: SerializeField, Min(0.1f)] public float ThrowScale { get; private set; } = 1f;
        [field: Tooltip("Camera shake when it catches it. 0 = none.")]
        [field: SerializeField, Min(0f)] public float CatchShake { get; private set; } = 0.1f;
        [field: Tooltip("Sprite squash (width, height) as it throws, and as it catches.")]
        [field: SerializeField] public Vector2 ThrowPunch { get; private set; } = new Vector2(1.2f, 0.85f);
        [field: SerializeField] public Vector2 CatchPunch { get; private set; } = new Vector2(0.85f, 1.15f);

        [field: Header("Slash (close)")]
        [field: Tooltip("Seconds it shakes before slashing (the warning).")]
        [field: SerializeField, Min(0f)] public float SlashWindup { get; private set; } = 0.3f;
        [field: Tooltip("Seconds the slash hitbox stays on.")]
        [field: SerializeField, Min(0.01f)] public float SlashTime { get; private set; } = 0.15f;
        [field: Tooltip("Speed of the small step forward during the slash.")]
        [field: SerializeField, Min(0f)] public float SlashStep { get; private set; } = 6f;
        [field: Tooltip("Camera shake on the slash. 0 = none.")]
        [field: SerializeField, Min(0f)] public float SlashShake { get; private set; } = 0.1f;
        [field: Tooltip("Sprite squash (width, height) as it slashes.")]
        [field: SerializeField] public Vector2 SlashPunch { get; private set; } = new Vector2(1.3f, 0.8f);

        [field: Header("Stagger (too many hits)")]
        [field: Tooltip("Hits that stagger it. Counted from the last stagger. 0 = never.")]
        [field: SerializeField, Min(0)] public int HitsToStagger { get; private set; } = 7;
        [field: Tooltip("Seconds it stays staggered (a big opening).")]
        [field: SerializeField, Min(0f)] public float StaggerTime { get; private set; } = 1.8f;

        [field: Header("Second half")]
        [field: Tooltip("At or below this share of its health the second half begins. 0 = never.")]
        [field: SerializeField, Range(0f, 1f)] public float Phase2Health { get; private set; } = 0.5f;
        [field: Tooltip("Pauses, warnings and the opening after attacks are multiplied by this in the second half.")]
        [field: SerializeField, Range(0.1f, 1f)] public float Phase2TimeScale { get; private set; } = 0.7f;
        [field: Tooltip("Chance (0-1) in the second half that an attack is followed straight away by another.")]
        [field: SerializeField, Range(0f, 1f)] public float Phase2ChainChance { get; private set; } = 0.5f;
        [field: Tooltip("In the second half it lays traps every this many attacks. 0 = never.")]
        [field: SerializeField, Min(0)] public int TrapEveryAttacks { get; private set; } = 4;
        [field: Tooltip("Traps laid at once (at most as many as the prefab has).")]
        [field: SerializeField, Min(0)] public int TrapCount { get; private set; } = 3;
        [field: Tooltip("Chance (0-1) that a trap hangs in the air instead of lying on the floor.")]
        [field: SerializeField, Range(0f, 1f)] public float AirTrapChance { get; private set; } = 0.4f;
        [field: Tooltip("Lowest and highest height (units above the floor) of a trap in the air.")]
        [field: SerializeField] public Vector2 AirTrapHeight { get; private set; } = new Vector2(1.5f, 3.5f);
        [field: Tooltip("Seconds a trap blinks before it starts to hurt (the warning).")]
        [field: SerializeField, Min(0f)] public float TrapArmTime { get; private set; } = 0.6f;
        [field: Tooltip("Blinks per second while a trap is not armed yet.")]
        [field: SerializeField, Min(0.1f)] public float TrapBlinkRate { get; private set; } = 6f;
        [field: Tooltip("Seconds a trap stays. The player can also break it with a hit.")]
        [field: SerializeField, Min(0f)] public float TrapLifetime { get; private set; } = 6f;

        [field: Header("Death")]
        [field: SerializeField, Min(0f)] public float DeathFadeTime { get; private set; } = 0.8f;

        private void OnValidate()
        {
            MaxIdleTime = Mathf.Max(MaxIdleTime, MinIdleTime);
            HopDistance = new Vector2(Mathf.Max(0f, HopDistance.x), Mathf.Max(HopDistance.x, HopDistance.y));
            AirTrapHeight = new Vector2(Mathf.Max(0f, AirTrapHeight.x), Mathf.Max(AirTrapHeight.x, AirTrapHeight.y));
        }
    }
}
