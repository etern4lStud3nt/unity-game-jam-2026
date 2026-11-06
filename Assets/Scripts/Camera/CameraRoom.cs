using Unity.Cinemachine;
using UnityEngine;

// A room the camera respects. The trigger detects the player; the room's own CinemachineCamera decides how it looks:
// - Follow room: camera has a PositionComposer + Confiner2D bound to the room, so it tracks the player but stops at the walls.
// - Locked room (boss arenas): camera has no Position Control and is placed by hand, so it stays still.
// Give room cameras a higher priority than the default follow camera and leave the CinemachineCamera component disabled.
[RequireComponent(typeof(Collider))]
public class CameraRoom : MonoBehaviour
{
    [SerializeField] private CameraController CameraController;
    [SerializeField] private CinemachineCamera RoomCamera;
    [SerializeField] private string PlayerTag = "Player";

    public CinemachineCamera Camera => RoomCamera;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        // Room prefabs can't reference the scene's controller, so find it when left empty.
        if (CameraController == null)
            CameraController = FindAnyObjectByType<CameraController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
            CameraController.EnterRoom(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            CameraController.ExitRoom(this);
    }

    // The player's collider can sit on a child object, so check the root as well.
    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(PlayerTag) || other.transform.root.CompareTag(PlayerTag);
    }
}
