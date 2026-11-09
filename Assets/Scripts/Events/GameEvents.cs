using UnityEngine;

// Game-wide events sent through EventBus. Publishers don't know who listens (audio, camera, UI, save...).

// The player walked into a room (published by LevelRoom).
public struct RoomEntered : IEvent
{
    public string RoomId;
    public string RoomName;
    public bool IsBossRoom;
}

// The player entered a boss arena for the first time this session.
public struct BossFightStarted : IEvent
{
    public string RoomId;
}

// The player lost health (published by PlayerHealthEvents).
public struct PlayerDamaged : IEvent
{
    public int Damage;
    public int CurrentHealth;
    public int MaxHealth;
}

public struct PlayerDied : IEvent { }

// The player came back at the last checkpoint after dying (published by PlayerRespawn).
public struct PlayerRespawned : IEvent
{
    public Vector3 Position;
}

// The player touched a checkpoint such as a bench (published by Checkpoint).
public struct CheckpointReached : IEvent
{
    public Vector3 Position;
}

// An enemy died (published by EnemyBrain when its death starts).
public struct EnemyKilled : IEvent
{
    public GameObject Enemy;
    public Vector3 Position;
}
