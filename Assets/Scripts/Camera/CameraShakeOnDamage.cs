using UnityEngine;

// Shakes the camera whenever the player takes damage. Put it on the object with CameraController.
[RequireComponent(typeof(CameraController))]
public class CameraShakeOnDamage : MonoBehaviour
{
    private CameraController cameraController;

    private void Awake()
    {
        cameraController = GetComponent<CameraController>();
    }

    private void OnEnable() => EventBus.Subscribe<PlayerDamaged>(OnPlayerDamaged);

    private void OnDisable() => EventBus.Unsubscribe<PlayerDamaged>(OnPlayerDamaged);

    private void OnPlayerDamaged(PlayerDamaged evt) => cameraController.Shake();
}
