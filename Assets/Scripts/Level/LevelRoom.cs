using UnityEngine;

// A room of the level. Its 2D trigger covers the room; when the player enters, RoomEntered is published
// (and BossFightStarted the first time a boss arena is entered). It can share the object with CameraRoom and RoomArea.
[RequireComponent(typeof(Collider2D))]
public class LevelRoom : MonoBehaviour
{
    [SerializeField] private string roomId;
    [SerializeField] private string roomName;
    [SerializeField] private bool isBossRoom;
    [SerializeField] private string playerTag = "Player";

    private bool bossStarted;

    public string RoomId => roomId;
    public string RoomName => roomName;
    public bool IsBossRoom => isBossRoom;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        EventBus.Publish(new RoomEntered { RoomId = roomId, RoomName = roomName, IsBossRoom = isBossRoom });
        if (isBossRoom && !bossStarted)
        {
            bossStarted = true;
            EventBus.Publish(new BossFightStarted { RoomId = roomId });
        }
    }

    // The player's collider can sit on a child object, so check the object carrying the Rigidbody2D as well.
    private bool IsPlayer(Collider2D other)
    {
        return other.CompareTag(playerTag)
            || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag));
    }
}
