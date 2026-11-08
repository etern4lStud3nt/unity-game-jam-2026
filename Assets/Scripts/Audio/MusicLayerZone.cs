using UnityEngine;

// Fades a layer of the playing music in while the player is inside this trigger and back out when they leave,
// e.g. the "Combat" layer in an arena. Gameplay code (an enemy noticing the player) can do the same by calling
// AudioManager.SetMusicLayer directly.
[RequireComponent(typeof(Collider2D))]
public class MusicLayerZone : MonoBehaviour
{
    [Tooltip("Must match a layer name in the MusicTrack that is playing here.")]
    [SerializeField] private string layerName = "Combat";
    [SerializeField, Range(0f, 1f)] private float volumeInside = 1f;
    [SerializeField, Range(0f, 1f)] private float volumeOutside;
    [SerializeField, Min(0f)] private float fadeDuration = 2f;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (AudioManager.Instance != null && IsPlayer(other))
            AudioManager.Instance.SetMusicLayer(layerName, volumeInside, fadeDuration);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (AudioManager.Instance != null && IsPlayer(other))
            AudioManager.Instance.SetMusicLayer(layerName, volumeOutside, fadeDuration);
    }

    // The player's collider can sit on a child object, so check the object carrying the Rigidbody2D as well.
    private bool IsPlayer(Collider2D other)
    {
        return other.CompareTag(playerTag)
            || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag));
    }
}
