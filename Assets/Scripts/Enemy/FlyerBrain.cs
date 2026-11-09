using UnityEngine;

namespace Enemies
{
    // A flyer: hovers around home, gets into position above and to the side of the player, winds up while aiming
    // ahead of them, then dives in one straight line. It never leaves its room: the room is the trigger collider
    // it was placed in (a LevelRoom, RoomArea or CameraRoom volume), or the Territory you assign.
    // Needs a FlyingMotor and a FlyerStats asset.
    [RequireComponent(typeof(FlyingMotor))]
    public class FlyerBrain : EnemyBrain
    {
        private static readonly string[] RoomComponents = { "LevelRoom", "RoomArea", "CameraRoom" };

        [Tooltip("The area it stays in. Empty = the room trigger it was placed in, found when the level starts.")]
        [SerializeField] private Collider2D territory;

        private bool searched;

        public FlyingMotor Flyer { get; private set; }
        public FlyerStats FlyerStats => Stats as FlyerStats;
        public Collider2D Territory => territory;

        public AimState Aim { get; private set; }
        public DiveState Dive { get; private set; }

        // Set by the aim state, used by the dive.
        public Vector2 DiveDirection { get; set; } = Vector2.down;

        // The player counts as "in reach" only while inside the room.
        public override bool PlayerInLeash => Senses.HasPlayer && InTerritory(Senses.PlayerPoint);

        protected override IState CreateIdleState()
        {
            Flyer = GetComponent<FlyingMotor>();
            if (FlyerStats == null)
            {
                Debug.LogWarning($"{name} needs a {nameof(Enemies.FlyerStats)} asset to hover and dive; it stays idle.", this);
                enabled = false;
            }
            Aim = new AimState(this);
            Dive = new DiveState(this);
            return new HoverState(this);
        }

        protected override IState CreateChaseState() => new ApproachState(this);

        public bool InTerritory(Vector2 point)
        {
            if (territory == null && !searched)
            {
                searched = true;
                territory = FindRoomAt(Home);
                if (territory == null)
                    Debug.LogWarning($"{name} found no room trigger around it; it is not confined to a room.", this);
            }
            return territory == null || territory.OverlapPoint(point);
        }

        // The point itself when inside the room; otherwise the nearest point on the room's edge, pulled a little
        // towards home so the flyer does not scrape along the wall.
        public Vector2 ClampToTerritory(Vector2 point)
        {
            if (InTerritory(point))
                return point;
            Vector2 edge = territory.ClosestPoint(point);
            return Vector2.MoveTowards(edge, Home, 0.75f);
        }

        // The room trigger (LevelRoom, RoomArea or CameraRoom) containing a point, matched by component name so
        // this works on branches where some of those scripts do not exist yet.
        private static Collider2D FindRoomAt(Vector2 point)
        {
            foreach (Collider2D candidate in Physics2D.OverlapPointAll(point))
            {
                if (!candidate.isTrigger)
                    continue;
                foreach (MonoBehaviour behaviour in candidate.GetComponents<MonoBehaviour>())
                    if (behaviour != null && System.Array.IndexOf(RoomComponents, behaviour.GetType().Name) >= 0)
                        return candidate;
            }
            return null;
        }

#if UNITY_EDITOR
        // The room it is confined to, as a cyan box (the collider's bounds).
        protected override void DrawExtraGizmos()
        {
            Collider2D area = Application.isPlaying ? territory : (territory != null ? territory : FindRoomAt(transform.position));
            if (area == null)
                return;
            Bounds b = area.bounds;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
#endif
    }
}
