using UnityEngine;

// Game-wide events sent through EventBus. Publishers don't know who listens (audio, camera, UI, save...).

// The player walked into a room (published by LevelRoom).
public struct RoomEntered : IEvent
{
    public string RoomId;
    public string RoomName;
    public bool IsBossRoom;
}

// A boss fight began (published by the boss, e.g. GrandmaBoss when her intro conversation ends). Also sent again
// when the fight restarts after the player died. RoomId is the arena's LevelRoom id.
public struct BossFightStarted : IEvent
{
    public string RoomId;
}

// A boss was defeated (published by the boss when its death starts).
public struct BossDefeated : IEvent
{
    public string RoomId;
    public Vector3 Position;
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

// The player picked up an ability (published by AbilityPickup).
public struct AbilityUnlocked : IEvent
{
    public PlayerAbility Ability;
}

// The player's money changed (published by MoneyWallet). Delta is positive when money was gained.
public struct MoneyChanged : IEvent
{
    public int Balance;
    public int Delta;
}

// The player's health changed for any reason: damage, healing, respawn or a new max health. Also sent once
// at start (published by PlayerHealthEvents). Delta is negative for damage, positive for healing.
public struct PlayerHealthChanged : IEvent
{
    public int Current;
    public int Max;
    public int Delta;
}

// A Yarn conversation started (published by DialogueBridge). The player stops until DialogueEnded.
public struct DialogueStarted : IEvent
{
    public string Node;
}

public struct DialogueEnded : IEvent { }

// A Yarn script ran <<signal Name>> (published by DialogueCommands). DialogueSignalListener turns it into scene
// changes: open a gate, start a fight, hide an NPC...
public struct DialogueSignal : IEvent
{
    public string Name;
}

// The number of enemies the player has defeated changed (published by KillCounter).
public struct KillCountChanged : IEvent
{
    public int Total;
    // Which kind of enemy was defeated (its stats asset name, e.g. "Walker_Dash") and how many of that kind so far.
    public string EnemyType;
    public int OfType;
}
