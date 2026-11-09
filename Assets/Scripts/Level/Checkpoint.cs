using UnityEngine;

// A respawn point (bench). Touching it heals the player and makes it the place PlayerRespawn returns to after death.
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool healOnReach = true;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || !(other.CompareTag(playerTag) || body.CompareTag(playerTag)))
            return;

        if (healOnReach && body.TryGetComponent(out Health health) && !health.IsDead)
            health.ResetHealth();

        EventBus.Publish(new CheckpointReached { Position = transform.position });
    }
}
