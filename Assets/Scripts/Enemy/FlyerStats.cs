using UnityEngine;

namespace Enemies
{
    // Settings for a flyer on top of the shared enemy stats. From the shared ones it uses Patrol Speed (hovering
    // and flying home), Chase Speed (getting into position), sight, Windup Time, Recover Time (also how long it
    // stays stuck after diving into the ground), Attack Cooldown, knockback, stun and death.
    [CreateAssetMenu(menuName = "Lucky Number/Flyer Stats", fileName = "FlyerStats")]
    public class FlyerStats : EnemyStats
    {
        [field: Header("Hover")]
        [field: Tooltip("Units it bobs up and down while idle.")]
        [field: SerializeField, Min(0f)] public float HoverBob { get; private set; } = 0.4f;
        [field: Tooltip("Units it drifts left and right of home while idle.")]
        [field: SerializeField, Min(0f)] public float HoverDrift { get; private set; } = 1.5f;

        [field: Header("Dive")]
        [field: Tooltip("Units above the player it lines up before diving.")]
        [field: SerializeField, Min(0f)] public float DiveHeight { get; private set; } = 3.5f;
        [field: Tooltip("Units to the side of the player it lines up before diving (sets the dive angle).")]
        [field: SerializeField, Min(0f)] public float DiveSide { get; private set; } = 3f;
        [field: Tooltip("How close to that spot (units) is close enough to start the wind-up.")]
        [field: SerializeField, Min(0.1f)] public float LineUpTolerance { get; private set; } = 1.2f;
        [field: Tooltip("Dive speed in units per second.")]
        [field: SerializeField, Min(0f)] public float DiveSpeed { get; private set; } = 18f;
        [field: Tooltip("Longest dive in units; it also stops at ground, walls and the room's edge.")]
        [field: SerializeField, Min(0f)] public float DiveDistance { get; private set; } = 9f;
        [field: Tooltip("0 = dives at where the player is, 1 = dives at where they will be if they keep running.")]
        [field: SerializeField, Range(0f, 1f)] public float Prediction { get; private set; } = 0.6f;
        [field: Tooltip("Seconds at the end of the wind-up during which the aim no longer follows the player.")]
        [field: SerializeField, Min(0f)] public float AimLockTime { get; private set; } = 0.15f;
    }
}
