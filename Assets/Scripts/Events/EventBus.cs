using System;
using System.Collections.Generic;
using UnityEngine;

// Marker for event types. Events are small structs carrying the data listeners need:
//
//     public struct PlayerDied : IEvent { }
//     public struct EnemyKilled : IEvent { public GameObject Enemy; public Vector3 Position; }
public interface IEvent { }

// Global event bus: whoever raises an event doesn't need to know who listens, and listeners don't need a
// reference to the sender. Raise with
//
//     EventBus.Publish(new EnemyKilled { Enemy = gameObject, Position = transform.position });
//
// and listen from any component, always unsubscribing again so destroyed objects aren't called:
//
//     private void OnEnable()  => EventBus.Subscribe<EnemyKilled>(OnEnemyKilled);
//     private void OnDisable() => EventBus.Unsubscribe<EnemyKilled>(OnEnemyKilled);
//     private void OnEnemyKilled(EnemyKilled evt) { ... }
public static class EventBus
{
    // Clear callbacks of every channel used so far, so all of them can be emptied when Play starts.
    private static readonly List<Action> channelClears = new();

    public static void Subscribe<T>(Action<T> handler) where T : struct, IEvent
    {
        Channel<T>.Handlers += handler;
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : struct, IEvent
    {
        Channel<T>.Handlers -= handler;
    }

    // Calls every handler subscribed to T. Handlers may subscribe or unsubscribe while this runs;
    // the change applies from the next Publish.
    public static void Publish<T>(T evt) where T : struct, IEvent
    {
        Channel<T>.Handlers?.Invoke(evt);
    }

    // Removes every handler of T.
    public static void Clear<T>() where T : struct, IEvent
    {
        Channel<T>.Handlers = null;
    }

    public static int SubscriberCount<T>() where T : struct, IEvent
    {
        return Channel<T>.Handlers?.GetInvocationList().Length ?? 0;
    }

    // The project enters Play mode without a domain reload, so static fields keep their values between
    // Play sessions. Empty every channel at the start of each session; otherwise handlers of objects from
    // the previous session would still be called.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearAll()
    {
        foreach (Action clear in channelClears)
            clear();
    }

    // One static class per event type holds that type's handlers, so no lookup or casting is needed.
    private static class Channel<T> where T : struct, IEvent
    {
        public static Action<T> Handlers;

        static Channel()
        {
            channelClears.Add(() => Handlers = null);
        }
    }
}
