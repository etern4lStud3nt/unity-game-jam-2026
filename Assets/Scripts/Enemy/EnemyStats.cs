using UnityEngine;

namespace Enemies
{
    // How an enemy attacks. Body contact damage (the Hurtbox) works with every type.
    public enum EnemyAttackType
    {
        ContactOnly,
        WindupLunge,
        // Warns, then swings: the brain's Attack Hitbox is switched on for SwingActiveTime.
        WindupSwing,
    }

    // All the numbers of one enemy type. Several prefabs can share one asset, and a new variant
    // ("fast but fragile walker") is just a new asset plus a prefab variant, no code.
    // Max health lives on the Health component, like for every other object with hit points.
    // Enemy types with extra settings derive from this (see GuardStats).
    [CreateAssetMenu(menuName = "Lucky Number/Enemy Stats", fileName = "EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [field: Header("Movement")]
        [field: SerializeField, Min(0f)] public float PatrolSpeed { get; private set; } = 2f;
        [field: SerializeField, Min(0f)] public float ChaseSpeed { get; private set; } = 4.5f;
        [field: Tooltip("Seconds the enemy waits at a wall or ledge before turning around.")]
        [field: SerializeField, Min(0f)] public float TurnPause { get; private set; } = 0.4f;
        [field: Tooltip("How far it patrols left and right of where it was placed. 0 = no limit, only walls and ledges turn it.")]
        [field: SerializeField, Min(0f)] public float PatrolDistance { get; private set; } = 4f;

        [field: Header("Sight")]
        [field: Tooltip("How far it sees in the direction it faces.")]
        [field: SerializeField, Min(0f)] public float SightRange { get; private set; } = 7f;
        [field: Tooltip("How far it notices the player behind its back.")]
        [field: SerializeField, Min(0f)] public float BackSightRange { get; private set; } = 2f;
        [field: Tooltip("Max height difference at which it still sees the player.")]
        [field: SerializeField, Min(0f)] public float VerticalSight { get; private set; } = 2.5f;
        [field: Tooltip("Seconds without seeing the player before it gives up the chase.")]
        [field: SerializeField, Min(0f)] public float LoseSightTime { get; private set; } = 1.5f;
        [field: Tooltip("Gives up the chase when the player gets this far from where the enemy was placed. 0 = no leash.")]
        [field: SerializeField, Min(0f)] public float LeashDistance { get; private set; }

        [field: Header("Attack (wind-up, lunge, recover)")]
        [field: Tooltip("ContactOnly: no attack, only the body hurts on touch. WindupLunge: warns, then lunges at the player. WindupSwing: warns, then swings its Attack Hitbox.")]
        [field: SerializeField] public EnemyAttackType AttackType { get; private set; } = EnemyAttackType.WindupLunge;
        [field: Tooltip("Max height difference at which it starts the attack.")]
        [field: SerializeField, Min(0f)] public float AttackHeight { get; private set; } = 1.5f;
        [field: Tooltip("Seconds between the end of one attack and the start of the next.")]
        [field: SerializeField, Min(0f)] public float AttackCooldown { get; private set; } = 1f;
        [field: Tooltip("The warning before the lunge: the player's window to react.")]
        [field: SerializeField, Min(0f)] public float WindupTime { get; private set; } = 0.35f;
        [field: Tooltip("Lunge only: average speed in units per second. Distance = Lunge Speed x Lunge Time.")]
        [field: SerializeField, Min(0f)] public float LungeSpeed { get; private set; } = 14f;
        [field: Tooltip("Lunge only: seconds the lunge lasts.")]
        [field: SerializeField, Min(0.01f)] public float LungeTime { get; private set; } = 0.25f;
        [field: Tooltip("Swing only: seconds the Attack Hitbox stays on.")]
        [field: SerializeField, Min(0.01f)] public float SwingActiveTime { get; private set; } = 0.15f;
        [field: Tooltip("Swing only: units it surges forward while swinging (1 = one tile). 0 = swings on the spot.")]
        [field: SerializeField, Min(0f)] public float SwingStep { get; private set; } = 0.5f;
        [field: Tooltip("Speed over the lunge and the swing step, like the player's dash: a burst that dies off. " +
                        "Only the shape matters; it is rescaled so the distance stays the same.")]
        [field: SerializeField] public AnimationCurve BurstCurve { get; private set; } =
            new AnimationCurve(new Keyframe(0f, 1.9f), new Keyframe(0.55f, 0.95f), new Keyframe(1f, 0.3f));
        [field: Tooltip("Pause after the lunge or swing: the player's window to hit back.")]
        [field: SerializeField, Min(0f)] public float RecoverTime { get; private set; } = 0.4f;

        // How long the active part of the chosen attack lasts.
        public float AttackActiveTime => AttackType == EnemyAttackType.WindupSwing ? SwingActiveTime : LungeTime;

        // Speed multiplier at progress t (0..1) of a lunge or swing step. Divided by the curve's average, so
        // speed x time still covers the set distance whatever shape the curve has.
        public float Burst(float t)
        {
            if (BurstCurve == null || BurstCurve.length == 0)
                return 1f;
            const int samples = 16;
            float sum = 0f;
            for (int i = 0; i < samples; i++)
                sum += BurstCurve.Evaluate((i + 0.5f) / samples);
            float average = sum / samples;
            return average > 0.0001f ? Mathf.Max(0f, BurstCurve.Evaluate(Mathf.Clamp01(t))) / average : 1f;
        }

        [field: Header("Getting hit")]
        [field: SerializeField, Min(0f)] public float KnockbackForce { get; private set; } = 6f;
        [field: SerializeField, Min(0f)] public float KnockbackTime { get; private set; } = 0.15f;
        [field: Tooltip("Seconds it stays stunned after a hit (cancels its attack). 0 = no stun, it keeps going.")]
        [field: SerializeField, Min(0f)] public float StunDuration { get; private set; }

        [field: Header("Death")]
        [field: SerializeField, Min(0.01f)] public float DeathTime { get; private set; } = 0.3f;
    }
}
