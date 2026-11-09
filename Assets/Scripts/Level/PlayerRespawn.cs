using System.Collections;
using UnityEngine;
using CharacterController = Assets.Scripts.CharacterController;

// Brings the player back:
// - after death, at the last checkpoint (bench), with full health;
// - after a hazard hit, on the last safe ground it stood on (Hollow Knight style spikes).
[RequireComponent(typeof(Health), typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField, Min(0f)] private float respawnDelay = 1f;
    [Tooltip("How often the last safe standing position is remembered.")]
    [SerializeField, Min(0.05f)] private float safeGroundInterval = 0.25f;
    [SerializeField] private LayerMask hazardLayers = ~0;

    private Health health;
    private Rigidbody2D body;
    private CharacterController controller;
    private Vector3 checkpoint;
    private Vector3 safeGround;
    private float safeTimer;
    private bool respawning;

    private void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Rigidbody2D>();
        controller = GetComponent<CharacterController>();
        checkpoint = transform.position;
        safeGround = transform.position;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CheckpointReached>(OnCheckpoint);
        EventBus.Subscribe<PlayerDied>(OnDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CheckpointReached>(OnCheckpoint);
        EventBus.Unsubscribe<PlayerDied>(OnDied);
    }

    private void FixedUpdate()
    {
        safeTimer -= Time.fixedDeltaTime;
        if (safeTimer > 0f || respawning || controller == null || !controller.IsGrounded())
            return;

        safeTimer = safeGroundInterval;
        if (!TouchingHazard())
            safeGround = transform.position;
    }

    // Called by hazards that hurt without killing.
    public void ReturnToSafeGround()
    {
        if (!health.IsDead)
            MoveTo(safeGround);
    }

    private void OnCheckpoint(CheckpointReached evt)
    {
        checkpoint = evt.Position;
    }

    private void OnDied(PlayerDied evt)
    {
        if (!respawning)
            StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        respawning = true;
        body.simulated = false;
        yield return new WaitForSeconds(respawnDelay);

        MoveTo(checkpoint);
        safeGround = checkpoint;
        health.ResetHealth();
        body.simulated = true;
        respawning = false;
        EventBus.Publish(new PlayerRespawned { Position = checkpoint });
    }

    private void MoveTo(Vector3 position)
    {
        body.linearVelocity = Vector2.zero;
        body.position = position;
        transform.position = position;
    }

    private bool TouchingHazard()
    {
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = hazardLayers };
        var hits = new Collider2D[4];
        int count = Physics2D.OverlapBox(transform.position, Vector2.one * 1.5f, 0f, filter, hits);
        for (int i = 0; i < count; i++)
            if (hits[i].GetComponentInParent<Hazard>() != null)
                return true;
        return false;
    }
}
