using UnityEngine;

// Spikes and other hazards: hurt the player and put them back on the last safe ground (Hollow Knight style).
// Works on a trigger collider such as the Hazards tilemap's TilemapCollider2D.
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other) => Hit(other);

    private void OnTriggerStay2D(Collider2D other) => Hit(other);

    private void Hit(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || !(other.CompareTag(playerTag) || body.CompareTag(playerTag)))
            return;
        if (!body.TryGetComponent(out Health health) || !health.TakeDamage(damage))
            return;

        if (!health.IsDead && body.TryGetComponent(out PlayerRespawn respawn))
            respawn.ReturnToSafeGround();
    }
}
