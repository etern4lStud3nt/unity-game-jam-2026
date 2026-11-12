using UnityEngine;

// A room of the level. Its 2D trigger covers the room; when the player enters, RoomEntered is published.
// It can share the object with CameraRoom and RoomArea. A boss arena's fight is started by the boss itself.
[RequireComponent(typeof(Collider2D))]
public class LevelRoom : MonoBehaviour
{
    [SerializeField] private string roomId;
    [SerializeField] private string roomName;
    [SerializeField] private bool isBossRoom;
    [SerializeField] private string playerTag = "Player";

    public string RoomId => roomId;
    public string RoomName => roomName;
    public bool IsBossRoom => isBossRoom;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // The room whose trigger covers point (e.g. the arena a boss was placed in), or null.
    public static LevelRoom FindAt(Vector2 point)
    {
        foreach (Collider2D candidate in Physics2D.OverlapPointAll(point))
            if (candidate.isTrigger && candidate.TryGetComponent(out LevelRoom room))
                return room;
        return null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        EventBus.Publish(new RoomEntered { RoomId = roomId, RoomName = roomName, IsBossRoom = isBossRoom });
    }

    // The player's collider can sit on a child object, so check the object carrying the Rigidbody2D as well.
    private bool IsPlayer(Collider2D other)
    {
        return other.CompareTag(playerTag)
            || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag));
    }
}
