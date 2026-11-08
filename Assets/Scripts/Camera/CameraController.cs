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

    // Zoom scales the live follow camera's view: orthographic size for an orthographic camera,
    // PositionComposer distance for a perspective one. 1 = the size set in the inspector, 0.5 = twice as close.
    [Header("Zoom")]
    [SerializeField] private float ZoomStepFactor = 0.2f;
    [SerializeField] private float MinZoom = 0.5f;
    [SerializeField] private float MaxZoom = 2f;
    [SerializeField] private float ZoomSpeed = 5f;

    [Header("Shake")]
    [SerializeField] private float ShakeForce = 1f;

    private CinemachineCamera followCamera;
    private CinemachineCamera liveCamera;
    // Rooms the player is inside, most recent last. Doorways can overlap two rooms; the latest one wins.
    private readonly List<CameraRoom> occupiedRooms = new();
    // Each camera's own view size from the inspector; zoom is a shared multiplier on top of it.
    private readonly Dictionary<CinemachineCamera, float> baseViewSizes = new();
    private float zoom = 1f;

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
        // Only follow cameras zoom; locked rooms keep the framing they were placed with.
        if (liveCamera == null || !liveCamera.TryGetComponent(out CinemachinePositionComposer composer))
            return;

        bool orthographic = IsOrthographic(liveCamera);
        float current = orthographic ? liveCamera.Lens.OrthographicSize : composer.CameraDistance;
        if (!baseViewSizes.TryGetValue(liveCamera, out float baseSize))
        {
            baseSize = current;
            baseViewSizes[liveCamera] = baseSize;
        }

        float target = baseSize * zoom;
        if (Mathf.Abs(current - target) < 0.001f)
            return;

        float next = Mathf.Lerp(current, target, ZoomSpeed * Time.deltaTime);
        if (orthographic)
        {
            LensSettings lens = liveCamera.Lens;
            lens.OrthographicSize = next;
            liveCamera.Lens = lens;
        }
        else
        {
            composer.CameraDistance = next;
        }

        // The confiner caches the view size, so it has to be told when it changes.
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
        ActivateRoom(room);
    }

    public void ExitRoom(CameraRoom room)
    {
        occupiedRooms.Remove(room);

        if (occupiedRooms.Count > 0)
            ActivateRoom(occupiedRooms[^1]);
        else
            Activate(followCamera);
    }

    [ContextMenu("Zoom In")]
    public void ZoomIn()
    {
        zoom = Mathf.Clamp(zoom - ZoomStepFactor, MinZoom, MaxZoom);
    }

    [ContextMenu("Zoom Out")]
    public void ZoomOut()
    {
        zoom = Mathf.Clamp(zoom + ZoomStepFactor, MinZoom, MaxZoom);
    }

    [ContextMenu("Shake")]
    public void Shake()
    {
        // Every CinemachineCamera with a CinemachineImpulseListener picks this up, room cameras included.
        ShakeSource.GenerateImpulseWithForce(ShakeForce);
    }

    private void ActivateRoom(CameraRoom room)
    {
        CinemachineCamera roomCamera = room.Camera;
        if (!room.UsesOwnViewSize && roomCamera.TryGetComponent(out CinemachinePositionComposer roomComposer))
            CopyFollowViewSize(roomCamera, roomComposer);

        Activate(roomCamera);
    }

    // Gives a follow room the default follow camera's view size (before zoom), so it only has to be set in one place.
    private void CopyFollowViewSize(CinemachineCamera roomCamera, CinemachinePositionComposer roomComposer)
    {
        if (!followCamera.TryGetComponent(out CinemachinePositionComposer followComposer))
            return;

        bool orthographic = IsOrthographic(followCamera);
        if (!baseViewSizes.TryGetValue(followCamera, out float followSize))
            followSize = orthographic ? followCamera.Lens.OrthographicSize : followComposer.CameraDistance;

        LensSettings lens = roomCamera.Lens;
        lens.FieldOfView = followCamera.Lens.FieldOfView;
        if (orthographic)
            lens.OrthographicSize = followSize;
        roomCamera.Lens = lens;
        roomComposer.CameraDistance = orthographic ? followComposer.CameraDistance : followSize;

        baseViewSizes[roomCamera] = followSize;
        if (roomCamera.TryGetComponent(out CinemachineConfiner2D confiner))
            confiner.InvalidateLensCache();
    }

    // A CinemachineCamera's lens inherits orthographic/perspective from the Unity Camera it renders to,
    // and Lens.Orthographic doesn't report that inherited mode, so ask the output camera.
    private static bool IsOrthographic(CinemachineCamera camera)
    {
        if (camera.Lens.ModeOverride != LensSettings.OverrideModes.None)
            return camera.Lens.Orthographic;

        CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(camera);
        return brain != null && brain.OutputCamera != null && brain.OutputCamera.orthographic;
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
