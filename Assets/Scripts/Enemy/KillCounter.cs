using System.Collections.Generic;
using Enemies;
using UnityEngine;

// Counts the enemies the player has defeated this session, in total and per kind (the enemy's stats asset name,
// e.g. "Walker_Basic" or "Guard_Basic"). Listens to EnemyKilled and publishes KillCountChanged, so a HUD, an
// achievement or a casino reward can use the numbers without touching the enemies.
public static class KillCounter
{
    private static readonly Dictionary<string, int> ByType = new Dictionary<string, int>();

    public static int Total { get; private set; }

    public static int Count(string enemyType) => ByType.TryGetValue(enemyType, out int count) ? count : 0;

    // Domain reload is off, so statics must be reset by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Total = 0;
        ByType.Clear();
    }

    // After EventBus clears its handlers (SubsystemRegistration), before the scene loads.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Listen() => EventBus.Subscribe<EnemyKilled>(OnEnemyKilled);

    private static void OnEnemyKilled(EnemyKilled evt)
    {
        string type = TypeOf(evt.Enemy);
        Total++;
        ByType[type] = Count(type) + 1;
        EventBus.Publish(new KillCountChanged { Total = Total, EnemyType = type, OfType = ByType[type] });
    }

    private static string TypeOf(GameObject enemy)
    {
        if (enemy != null && enemy.TryGetComponent(out EnemyBrain brain) && brain.Stats != null)
            return brain.Stats.name;
        return enemy != null ? enemy.name : "Unknown";
    }
}
