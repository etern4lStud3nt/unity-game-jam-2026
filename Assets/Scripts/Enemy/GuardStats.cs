using UnityEngine;

namespace Enemies
{
    // Settings for a guard: everything a walker has, plus how often it glances behind itself.
    // Use the base Leash Distance to keep it near the spot it protects, and Patrol Speed for walking back there.
    [CreateAssetMenu(menuName = "Lucky Number/Guard Stats", fileName = "GuardStats")]
    public class GuardStats : EnemyStats
    {
        [field: Header("Guard")]
        [field: Tooltip("Seconds facing its post before glancing behind. 0 = never looks back.")]
        [field: SerializeField, Min(0f)] public float LookBackInterval { get; private set; } = 3f;
        [field: Tooltip("Seconds it keeps looking behind before turning back.")]
        [field: SerializeField, Min(0f)] public float LookBackTime { get; private set; } = 1f;
    }
}
