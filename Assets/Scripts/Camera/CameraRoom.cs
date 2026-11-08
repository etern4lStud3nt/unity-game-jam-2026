using Unity.Cinemachine;
using UnityEngine;

// A room the camera respects. The 2D trigger detects the player; the room's own CinemachineCamera decides how it looks:
// - Follow room: camera has a PositionComposer + Confiner2D bound to the room, so it tracks the player but stops at the walls.
// - Locked room (boss arenas): camera has no Position Control and is placed by hand, so it stays still.
// Give room cameras a higher priority than the default follow camera and leave the CinemachineCamera component disabled.
[RequireComponent(typeof(Collider2D))]
public class CameraRoom : MonoBehaviour
{
    [SerializeField] private CameraController CameraController;
    [SerializeField] private CinemachineCamera RoomCamera;
    [SerializeField] private string PlayerTag = "Player";

    [Header("Shake on enter (e.g. boss arena)")]
    // The room's own impulse: shape, duration and strength are tuned per room on that component. Leave empty for no shake.
    [SerializeField] private CinemachineImpulseSource EnterShake;
    // Waiting for the camera blend to finish makes the shake read as a hit on the new view.
    [SerializeField] private float ShakeDelay = 0.75f;

    public CinemachineCamera Camera => RoomCamera;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        // Room prefabs can't reference the scene's controller, so find it when left empty.
        if (CameraController == null)
            CameraController = FindAnyObjectByType<CameraController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        CameraController.EnterRoom(this);
        if (EnterShake != null)
            Invoke(nameof(Shake), ShakeDelay);
    }

    private void Shake()
    {
        EnterShake.GenerateImpulse();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
            CameraController.ExitRoom(this);
    }

    // The player's collider can sit on a child object, so check the object carrying the Rigidbody2D as well.
    private bool IsPlayer(Collider2D other)
    {
        return other.CompareTag(PlayerTag)
            || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(PlayerTag));
    }
}
