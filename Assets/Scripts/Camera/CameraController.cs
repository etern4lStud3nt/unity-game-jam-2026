using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// Sits on the default follow CinemachineCamera, which is live whenever the player is outside every CameraRoom.
// Framing, damping, confining and blends live on the Cinemachine components in the inspector;
// this only switches between cameras at runtime.
[RequireComponent(typeof(CinemachineCamera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform PlayerTransform;
    [SerializeField] private CinemachineImpulseSource ShakeSource;

    [Header("Zoom (PositionComposer camera distance)")]
    [SerializeField] private float ZoomStep = 2f;
    [SerializeField] private float MinZoomOffset = -6f;
    [SerializeField] private float MaxZoomOffset = 10f;
    [SerializeField] private float ZoomSpeed = 5f;

    [Header("Shake")]
    [SerializeField] private float ShakeForce = 1f;

    private CinemachineCamera followCamera;
    private CinemachineCamera liveCamera;
    // Rooms the player is inside, most recent last. Doorways can overlap two rooms; the latest one wins.
    private readonly List<CameraRoom> occupiedRooms = new();
    // Each camera's own distance from the inspector; zoom is applied as a shared offset on top of it.
    private readonly Dictionary<CinemachinePositionComposer, float> baseDistances = new();
    private float zoomOffset;

    private void Awake()
    {
        followCamera = GetComponent<CinemachineCamera>();
    }

    private void Start()
    {
        FollowPlayer();
    }

    private void Update()
    {
        if (liveCamera == null || !liveCamera.TryGetComponent(out CinemachinePositionComposer composer))
            return;

        if (!baseDistances.TryGetValue(composer, out float baseDistance))
        {
            baseDistance = composer.CameraDistance;
            baseDistances[composer] = baseDistance;
        }

        float targetDistance = baseDistance + zoomOffset;
        if (Mathf.Abs(composer.CameraDistance - targetDistance) < 0.001f)
            return;

        composer.CameraDistance = Mathf.Lerp(composer.CameraDistance, targetDistance, ZoomSpeed * Time.deltaTime);

        // The confiner caches the view size, so it has to be told when the distance changes.
        if (liveCamera.TryGetComponent(out CinemachineConfiner2D confiner))
            confiner.InvalidateLensCache();
    }

    [ContextMenu("Follow Player")]
    public void FollowPlayer()
    {
        occupiedRooms.Clear();
        followCamera.Target.TrackingTarget = PlayerTransform;
        Activate(followCamera);
    }

    public void EnterRoom(CameraRoom room)
    {
        occupiedRooms.Remove(room);
        occupiedRooms.Add(room);
        Activate(room.Camera);
    }

    public void ExitRoom(CameraRoom room)
    {
        occupiedRooms.Remove(room);

        if (occupiedRooms.Count > 0)
            Activate(occupiedRooms[^1].Camera);
        else
            Activate(followCamera);
    }

    [ContextMenu("Zoom In")]
    public void ZoomIn()
    {
        zoomOffset = Mathf.Clamp(zoomOffset - ZoomStep, MinZoomOffset, MaxZoomOffset);
    }

    [ContextMenu("Zoom Out")]
    public void ZoomOut()
    {
        zoomOffset = Mathf.Clamp(zoomOffset + ZoomStep, MinZoomOffset, MaxZoomOffset);
    }

    [ContextMenu("Shake")]
    public void Shake()
    {
        // Every CinemachineCamera with a CinemachineImpulseListener picks this up, room cameras included.
        ShakeSource.GenerateImpulseWithForce(ShakeForce);
    }

    // Room cameras outrank the follow camera, so enabling one makes it live and disabling it hands control back.
    // The brain's blend settings take care of the transition.
    private void Activate(CinemachineCamera camera)
    {
        if (liveCamera != null && liveCamera != followCamera && liveCamera != camera)
            liveCamera.enabled = false;

        if (camera != followCamera)
        {
            // Follow rooms track the player; locked rooms have no Position Control so this does nothing for them.
            camera.Target.TrackingTarget = PlayerTransform;
            camera.enabled = true;
        }

        liveCamera = camera;
    }
}
