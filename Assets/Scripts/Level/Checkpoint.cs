using UnityEngine;

// A respawn point (bench). Standing at it and pressing Interact heals the player and makes it the place
// PlayerRespawn returns to after death (and saves the game). The prompt ("E Save") shows while the player is in range.
public class Checkpoint : CasinoInteractable
{
    [SerializeField] private bool healOnReach = true;
    [SerializeField] private SoundData sitSound;

    protected override void Use(Rigidbody2D player)
    {
        if (sitSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(sitSound, transform.position);
        if (healOnReach && player.TryGetComponent(out Health health) && !health.IsDead)
            health.ResetHealth();

        EventBus.Publish(new CheckpointReached { Position = transform.position });
    }
}
