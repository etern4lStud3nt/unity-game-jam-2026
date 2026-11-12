using UnityEngine;

namespace Bosses
{
    // Tuning for the final fight (Mother + Father). The shared health is on the boss's Health component;
    // everything else is here.
    [CreateAssetMenu(menuName = "Lucky Number/Parents Stats", fileName = "ParentsStats")]
    public class ParentsStats : ScriptableObject
    {
        [field: Header("Rhythm (the director)")]
        [field: Tooltip("Seconds between the end of one attack and the choice of the next.")]
        [field: SerializeField, Min(0f)] public float IdleBetween { get; private set; } = 0.35f;
        [field: Tooltip("Chance (0-1) that the next move is the combo (slippers + dive). Never twice in a row.")]
        [field: SerializeField, Range(0f, 1f)] public float ComboChance { get; private set; } = 0.3f;
        [field: Tooltip("Weights of the solo moves (0 = never). The same solo never comes twice in a row.")]
        [field: SerializeField, Min(0f)] public float DashWeight { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float DiveWeight { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float VolleyWeight { get; private set; } = 1f;
        [field: Tooltip("Units both keep from the arena's left and right edges.")]
        [field: SerializeField, Min(0f)] public float EdgeMargin { get; private set; } = 1.5f;

        [field: Header("Appearing and leaving (both)")]
        [field: Tooltip("Sprite stretch (width, height) as one appears.")]
        [field: SerializeField] public Vector2 AppearPunch { get; private set; } = new Vector2(0.6f, 1.4f);
        [field: Tooltip("Seconds one takes to fade away after its opening.")]
        [field: SerializeField, Min(0f)] public float VanishTime { get; private set; } = 0.15f;
        [field: Tooltip("Height of the sprite (1 = normal) while crouching (warnings, the Mother's reload).")]
        [field: SerializeField, Range(0.3f, 1f)] public float CrouchScale { get; private set; } = 0.8f;

        [field: Header("Dash (either one: appears at a side of the room, dashes across the floor)")]
        [field: Tooltip("Seconds it crouches and shakes at the side before dashing (the warning).")]
        [field: SerializeField, Min(0f)] public float DashTell { get; private set; } = 0.45f;
        [field: SerializeField, Min(0.1f)] public float DashSpeed { get; private set; } = 30f;
        [field: Tooltip("Camera shake when the dash ends. 0 = none.")]
        [field: SerializeField, Min(0f)] public float DashShake { get; private set; } = 0.2f;
        [field: Tooltip("Seconds it stands open at the far side after a dash: the player's moment to hit it.")]
        [field: SerializeField, Min(0f)] public float DashRecover { get; private set; } = 0.6f;

        [field: Header("Father: dive (appears above the player, hangs, dives straight down)")]
        [field: Tooltip("Units above the floor he appears at.")]
        [field: SerializeField, Min(0.5f)] public float DiveHeight { get; private set; } = 7f;
        [field: Tooltip("Seconds he hangs still and shakes before diving (the warning). His position is locked.")]
        [field: SerializeField, Min(0f)] public float DiveTell { get; private set; } = 0.5f;
        [field: SerializeField, Min(0.1f)] public float DiveSpeed { get; private set; } = 32f;
        [field: Tooltip("Camera shake when he hits the ground. 0 = none.")]
        [field: SerializeField, Min(0f)] public float ImpactShake { get; private set; } = 1f;
        [field: Tooltip("Sprite squash (width, height) when he hits the ground.")]
        [field: SerializeField] public Vector2 ImpactPunch { get; private set; } = new Vector2(1.4f, 0.65f);
        [field: Tooltip("Seconds he stays stuck in the ground after a dive: the player's moment to hit him.")]
        [field: SerializeField, Min(0f)] public float StuckTime { get; private set; } = 0.7f;

        [field: Header("Mother: slipper waves (appears at a side of the room; flat, stacked in lanes)")]
        [field: Tooltip("Heights (units above the floor, to a slipper's middle) of the lanes slippers fly in, lowest " +
                        "first. The lowest must be low enough to hit a standing player.")]
        [field: SerializeField] public float[] LaneHeights { get; private set; } = new[] { 0.5f, 1.8f, 3.1f, 4.4f, 5.7f, 7f };
        [field: Tooltip("Lanes next to each other left empty in every wave: the gap the player escapes through. " +
                        "The gap moves to other lanes every wave.")]
        [field: SerializeField, Min(1)] public int GapLanes { get; private set; } = 2;
        [field: Tooltip("Seconds she crouches and shakes before the first wave (the warning).")]
        [field: SerializeField, Min(0f)] public float ThrowTell { get; private set; } = 0.4f;
        [field: Tooltip("Seconds a wave's slippers hang in front of her before they fly, showing where the gap is.")]
        [field: SerializeField, Min(0f)] public float WaveTell { get; private set; } = 0.55f;
        [field: Tooltip("Seconds between a wave leaving and the next one appearing.")]
        [field: SerializeField, Min(0f)] public float WaveInterval { get; private set; } = 0.45f;
        [field: Tooltip("Waves per volley.")]
        [field: SerializeField, Min(1)] public int WaveCount { get; private set; } = 3;
        [field: Tooltip("Slipper speed in units per second.")]
        [field: SerializeField, Min(0.1f)] public float SlipperSpeed { get; private set; } = 13f;
        [field: Tooltip("Units in front of her the slippers appear.")]
        [field: SerializeField, Min(0f)] public float SlipperSpawnAhead { get; private set; } = 0.8f;
        [field: Tooltip("Degrees per second a flying slipper spins, rolling forwards. 0 = it flies flat. Only the " +
                        "sprite spins: the hitbox stays flat, so a wave's gap stays as wide as it looks at rest.")]
        [field: SerializeField] public float SlipperSpin { get; private set; } = 900f;
        [field: Tooltip("Sprite squash (width, height) as she throws a wave.")]
        [field: SerializeField] public Vector2 ThrowPunch { get; private set; } = new Vector2(1.2f, 0.85f);
        [field: Tooltip("Seconds she bends down for new slippers after a volley: the player's moment to hit her.")]
        [field: SerializeField, Min(0f)] public float ReloadTime { get; private set; } = 0.9f;

        [field: Header("Cross dive (both: they hang up left and up right of the player, then dive diagonally)")]
        [field: Tooltip("Weight of the cross dive among the solo moves in the first half (0 = never).")]
        [field: SerializeField, Min(0f)] public float CrossDiveWeight { get; private set; } = 1f;
        [field: Tooltip("Weight of the cross dive in the second half (0 = never).")]
        [field: SerializeField, Min(0f)] public float Phase2CrossDiveWeight { get; private set; } = 1.5f;
        [field: Tooltip("Units above the floor they hang at.")]
        [field: SerializeField, Min(0.5f)] public float CrossDiveHeight { get; private set; } = 6f;
        [field: Tooltip("Units to the left and right of the player they hang at (with the height, sets the angle).")]
        [field: SerializeField, Min(0.5f)] public float CrossDiveSideOffset { get; private set; } = 6f;
        [field: Tooltip("Seconds each hangs still and shakes before diving (the warning). Nothing moves any more: " +
                        "both dive through the spot the player stood on when the move began.")]
        [field: SerializeField, Min(0f)] public float CrossDiveTell { get; private set; } = 0.6f;
        [field: SerializeField, Min(0.1f)] public float CrossDiveSpeed { get; private set; } = 30f;
        [field: Tooltip("0 = they dive at where the player stood when the move began, 1 = at where the player will be " +
                        "when the first dive arrives if they keep running the same way.")]
        [field: SerializeField, Range(0f, 1f)] public float CrossDivePrediction { get; private set; } = 0.5f;
        [field: Tooltip("How steeply they must dive at least: 0 = may fly flat sideways, 1 = only straight down. " +
                        "Keeps a dive at a player high in the air from crossing the whole room.")]
        [field: SerializeField, Range(0f, 1f)] public float CrossDiveMinDownward { get; private set; } = 0.4f;
        [field: Tooltip("Seconds the second one appears after the first. 0 = both dive at the same moment.")]
        [field: SerializeField, Min(0f)] public float CrossDiveStagger { get; private set; } = 0.35f;

        [field: Header("Combo: slippers + dive")]
        [field: Tooltip("Shortest and longest wait (seconds) after her first wave leaves before he appears above the player.")]
        [field: SerializeField] public Vector2 ComboDiveDelay { get; private set; } = new Vector2(0.2f, 0.9f);

        [field: Header("Second half")]
        [field: Tooltip("At or below this share of the shared health the second half begins. 0 = never.")]
        [field: SerializeField, Range(0f, 1f)] public float Phase2Health { get; private set; } = 0.5f;
        [field: SerializeField, Min(0f)] public float Phase2IdleBetween { get; private set; } = 0.15f;
        [field: SerializeField, Range(0f, 1f)] public float Phase2ComboChance { get; private set; } = 0.45f;
        [field: Tooltip("Fewest and most dives he chains in the second half (dashes: there and back).")]
        [field: SerializeField] public Vector2Int Phase2DiveChain { get; private set; } = new Vector2Int(2, 3);
        [field: Tooltip("Seconds between the dashes or dives of a chain (the last one uses the full opening).")]
        [field: SerializeField, Min(0f)] public float ChainPause { get; private set; } = 0.25f;
        [field: SerializeField, Min(1)] public int Phase2WaveCount { get; private set; } = 4;
        [field: Tooltip("Camera shake when the second half begins. 0 = none.")]
        [field: SerializeField, Min(0f)] public float PhaseShake { get; private set; } = 0.8f;

        [field: Header("Game feel")]
        [field: Tooltip("Seconds one takes to fade in when it appears.")]
        [field: SerializeField, Min(0f)] public float AppearFadeTime { get; private set; } = 0.08f;
        [field: Tooltip("Units a dasher slides back while it winds up (the dash then snaps forward).")]
        [field: SerializeField, Min(0f)] public float DashWindupPull { get; private set; } = 0.5f;
        [field: Tooltip("Sprite stretch (width, height) as the dash launches.")]
        [field: SerializeField] public Vector2 DashLaunchPunch { get; private set; } = new Vector2(1.4f, 0.7f);
        [field: Tooltip("Camera shake as the dash launches. 0 = none.")]
        [field: SerializeField, Min(0f)] public float DashLaunchShake { get; private set; } = 0.1f;
        [field: Tooltip("Units over which the dash brakes before the far side, like a skid. 0 = stops dead.")]
        [field: SerializeField, Min(0f)] public float DashSkid { get; private set; } = 2f;
        [field: Tooltip("Sprite squash (width, height) as the dash comes to a stop.")]
        [field: SerializeField] public Vector2 DashStopPunch { get; private set; } = new Vector2(0.8f, 1.2f);
        [field: Tooltip("Units a diver draws back (up, or away from its dive) while it hangs and shakes.")]
        [field: SerializeField, Min(0f)] public float HangPull { get; private set; } = 0.4f;
        [field: Tooltip("Show a thin line along the dive's path during the dive warnings.")]
        [field: SerializeField] public bool DiveLines { get; private set; } = true;
        [field: Tooltip("Thickness of the dive line in units.")]
        [field: SerializeField, Min(0.01f)] public float DiveLineWidth { get; private set; } = 0.08f;
        [field: Tooltip("How visible (0-1) the dive line is at the end of the warning; it fades in from nothing.")]
        [field: SerializeField, Range(0f, 1f)] public float DiveLineAlpha { get; private set; } = 0.35f;
        [field: Tooltip("Camera shake as each slipper wave flies. 0 = none.")]
        [field: SerializeField, Min(0f)] public float ThrowShake { get; private set; } = 0.08f;
        [field: Tooltip("Seconds a slipper takes to pop to full size when it appears in her hand.")]
        [field: SerializeField, Min(0f)] public float SlipperPopTime { get; private set; } = 0.12f;
        [field: Tooltip("Degrees a hanging slipper wiggles back and forth before it flies.")]
        [field: SerializeField, Min(0f)] public float SlipperWiggle { get; private set; } = 12f;
        [field: Tooltip("Wiggles per second of a hanging slipper.")]
        [field: SerializeField, Min(0f)] public float SlipperWiggleRate { get; private set; } = 4f;
        [field: Tooltip("Sprite stretch (width, height) of a slipper as it is let go.")]
        [field: SerializeField] public Vector2 SlipperLaunchStretch { get; private set; } = new Vector2(1.5f, 0.7f);
        [field: Tooltip("Camera shake on the hit that empties the shared health. 0 = none.")]
        [field: SerializeField, Min(0f)] public float DeathShake { get; private set; } = 1.5f;

        [field: Header("Sounds (optional; empty = silent)")]
        [field: Tooltip("Plays whenever either one appears for a move.")]
        [field: SerializeField] public SoundData AppearSound { get; private set; }
        [field: Tooltip("The Father's voice as he appears for a move (give the sound a cooldown so it does not " +
                        "come every time).")]
        [field: SerializeField] public SoundData FatherVoice { get; private set; }
        [field: Tooltip("The Mother's voice as she appears for a move.")]
        [field: SerializeField] public SoundData MotherVoice { get; private set; }
        [field: SerializeField] public SoundData DashSound { get; private set; }
        [field: SerializeField] public SoundData ImpactSound { get; private set; }
        [field: SerializeField] public SoundData ThrowSound { get; private set; }
        [field: Tooltip("Plays when the shared health runs out.")]
        [field: SerializeField] public SoundData DefeatSound { get; private set; }

        [field: Header("Ending")]
        [field: Tooltip("Yarn node of their last words. Empty = none.")]
        [field: SerializeField] public string LastWordsNode { get; private set; } = "Parents_LastWords";
        [field: Tooltip("Speed the fallen one drops to the floor with.")]
        [field: SerializeField, Min(0.1f)] public float EndFallSpeed { get; private set; } = 14f;
        [field: Tooltip("Speed the one left standing walks to the fallen one with.")]
        [field: SerializeField, Min(0.1f)] public float WalkSpeed { get; private set; } = 6f;
        [field: Tooltip("Units from the fallen one the other appears at, if it was away.")]
        [field: SerializeField, Min(0f)] public float WalkInDistance { get; private set; } = 5f;
        [field: Tooltip("Units from the fallen one the other stops at.")]
        [field: SerializeField, Min(0f)] public float KneelOffset { get; private set; } = 1.2f;
        [field: Tooltip("Longest the fall and the walk may take before the ending moves on anyway (seconds).")]
        [field: SerializeField, Min(0.1f)] public float EndingStepTimeout { get; private set; } = 6f;
        [field: Tooltip("Seconds both take to fade out after their last words.")]
        [field: SerializeField, Min(0f)] public float EndFadeTime { get; private set; } = 1.5f;

        private void OnValidate()
        {
            ComboDiveDelay = new Vector2(Mathf.Max(0f, ComboDiveDelay.x), Mathf.Max(ComboDiveDelay.x, ComboDiveDelay.y));
            int least = Mathf.Max(1, Phase2DiveChain.x);
            Phase2DiveChain = new Vector2Int(least, Mathf.Max(least, Phase2DiveChain.y));
        }
    }
}
