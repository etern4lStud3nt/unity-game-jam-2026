using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    // Brings every enemy back (alive, at its start, full health) when the player respawns at a checkpoint,
    // like resting at a bench. Enemies register themselves; dead ones are only switched off, so they can return.
    public static class EnemyReset
    {
        private static readonly List<EnemyBrain> Enemies = new List<EnemyBrain>();

        // Domain reload is off, so the list must be emptied by hand between Play sessions.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearList() => Enemies.Clear();

        // After EventBus clears its handlers (SubsystemRegistration), before the scene loads.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Listen() => EventBus.Subscribe<PlayerRespawned>(OnPlayerRespawned);

        internal static void Register(EnemyBrain enemy)
        {
            if (!Enemies.Contains(enemy))
                Enemies.Add(enemy);
        }

        internal static void Unregister(EnemyBrain enemy) => Enemies.Remove(enemy);

        private static void OnPlayerRespawned(PlayerRespawned evt)
        {
            foreach (EnemyBrain enemy in Enemies.ToArray())
                if (enemy != null)
                    enemy.ResetToStart();
        }
    }
}
