using UnityEngine;

// Tells AudioManager which area a room belongs to. Put it on the room's trigger (it can share the object with CameraRoom).
// Music and ambience only change when the area changes, so walking between rooms of the same area keeps the music going.
[RequireComponent(typeof(Collider2D))]
public class RoomArea : MonoBehaviour
{
    [SerializeField] private AreaData area;
    [Tooltip("Optional. Plays instead of the area's music while in this room (boss arenas).")]
    [SerializeField] private MusicTrack musicOverride;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (AudioManager.Instance != null && IsPlayer(other))
            AudioManager.Instance.EnterArea(area, musicOverride);
    }

    // The player's collider can sit on a child object, so check the object carrying the Rigidbody2D as well.
    private bool IsPlayer(Collider2D other)
    {
        return other.CompareTag(playerTag)
            || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag));
    }
}
