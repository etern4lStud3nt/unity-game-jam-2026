using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

// Moves the player between the level and the casino behind a black screen. The casino is its own scene, loaded on
// top of the level (additive) and placed far away, so the player, camera and managers of the level keep working in it.
public static class CasinoTravel
{
    // Where the leave door puts the player back: the spot of the last casino door they came in through.
    public static Vector3? ReturnPosition { get; set; }

    // Domain reload is off, so the static must be cleared by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ReturnPosition = null;

    // Fades out, runs whileBlack (e.g. loading the casino scene; may be null), moves the player to target() (null =
    // stay), then fades back in. Input is frozen the whole time.
    public static IEnumerator Move(Rigidbody2D player, float fadeTime, IEnumerator whileBlack, Func<Vector3?> target)
    {
        InputManager input = InputManager.Instance;
        if (input != null)
            input.BlockGameplay();
        try
        {
            yield return ScreenFade.To(1f, fadeTime);
            if (whileBlack != null)
                yield return whileBlack;
            Vector3? spot = target();
            if (spot.HasValue && player != null)
            {
                Teleport(player, spot.Value);
                // One physics step so the camera rooms at the new spot see the player and switch cameras...
                yield return new WaitForFixedUpdate();
                // ...then put every camera straight there, with no glide and no blend from the old spot.
                SnapCameras();
                yield return null;
                SnapCameras();
            }
            yield return ScreenFade.To(0f, fadeTime);
        }
        finally
        {
            if (input != null)
                input.UnblockGameplay();
        }
    }

    private static void Teleport(Rigidbody2D player, Vector3 target)
    {
        Vector3 delta = target - player.transform.position;
        player.linearVelocity = Vector2.zero;
        player.position = target;
        player.transform.position = target;
        // Snap the cameras that follow the player instead of letting them glide across the map.
        CinemachineCore.OnTargetObjectWarped(player.transform, delta);
    }

    // Drops every camera's damping history (room cameras too, which were off and remember the old spot) and cancels
    // the brain's blend, so the next camera update jumps straight to the new view.
    private static void SnapCameras()
    {
        foreach (CinemachineVirtualCameraBase camera in UnityEngine.Object.FindObjectsByType<CinemachineVirtualCameraBase>(FindObjectsInactive.Include))
            camera.PreviousStateIsValid = false;
        foreach (CinemachineBrain brain in UnityEngine.Object.FindObjectsByType<CinemachineBrain>())
            brain.ResetState();
    }
}
