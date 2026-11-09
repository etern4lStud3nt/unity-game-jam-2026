using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CharacterController = Assets.Scripts.CharacterController;

// Level testing helpers. Runs only in the editor and development builds.
//   F1            fly mode (no collision, WASD / arrows to move, Shift = faster)
//   PageDown/Up   teleport to the next / previous room's Spawn point
//   F2            toggle double jump unlock
//   F3            toggle wall jump unlock
//   F4            hide / show this panel
public class LevelDebugTools : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private CharacterController character;
    [SerializeField] private Transform roomsRoot;
    [SerializeField] private float flySpeed = 15f;

    private readonly List<Transform> rooms = new();
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private bool flying;
    private bool showPanel = true;
    private int roomIndex = -1;

    private void Start()
    {
        if (character == null) character = FindAnyObjectByType<CharacterController>();
        if (roomsRoot == null)
        {
            GameObject root = GameObject.Find("Level Rooms");
            if (root != null) roomsRoot = root.transform;
        }

        body = character.GetComponent<Rigidbody2D>();
        bodyCollider = character.GetComponent<Collider2D>();

        if (roomsRoot != null)
        {
            foreach (Transform room in roomsRoot)
                if (room.Find("Spawn") != null) rooms.Add(room);
            rooms.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f1Key.wasPressedThisFrame) SetFlying(!flying);
        if (keyboard.f2Key.wasPressedThisFrame) character.DoubleJumpUnlocked = !character.DoubleJumpUnlocked;
        if (keyboard.f3Key.wasPressedThisFrame) character.WallJumpUnlocked = !character.WallJumpUnlocked;
        if (keyboard.f4Key.wasPressedThisFrame) showPanel = !showPanel;
        if (keyboard.pageDownKey.wasPressedThisFrame) Teleport(roomIndex + 1);
        if (keyboard.pageUpKey.wasPressedThisFrame) Teleport(roomIndex - 1);

        if (flying)
        {
            Vector2 move = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1;
            float speed = keyboard.shiftKey.isPressed ? flySpeed * 3f : flySpeed;
            character.transform.position += (Vector3)(move.normalized * speed * Time.deltaTime);
        }
    }

    private void SetFlying(bool on)
    {
        flying = on;
        body.simulated = !on;
        bodyCollider.enabled = !on;
        body.linearVelocity = Vector2.zero;
    }

    private void Teleport(int index)
    {
        if (rooms.Count == 0) return;
        roomIndex = (index % rooms.Count + rooms.Count) % rooms.Count;
        Transform spawn = rooms[roomIndex].Find("Spawn");
        body.linearVelocity = Vector2.zero;
        body.position = spawn.position;
        character.transform.position = spawn.position;
    }

    private void OnGUI()
    {
        if (!showPanel) return;
        string room = roomIndex >= 0 && roomIndex < rooms.Count ? rooms[roomIndex].name : "-";
        string text =
            "LEVEL DEBUG  (F4 hide)\n" +
            $"F1  Fly: {(flying ? "ON" : "off")}\n" +
            $"F2  Double jump: {(character.DoubleJumpUnlocked ? "ON" : "off")}\n" +
            $"F3  Wall jump: {(character.WallJumpUnlocked ? "ON" : "off")}\n" +
            $"PgDn / PgUp  Room: {room}";
        GUI.Box(new Rect(10, 10, 300, 100), GUIContent.none);
        GUI.Label(new Rect(18, 14, 290, 95), text);
    }
#endif
}
