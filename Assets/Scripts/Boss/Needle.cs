using Enemies;
using UnityEngine;

namespace Bosses
{
    // One of Grandma's knitting needles. Drifts around its spot (where it sits in the prefab, relative to Grandma,
    // so it follows her), then attacks with a chain of dives: shakes while aiming ahead of the player, dives point
    // first in a straight line and sticks in the ground or a wall; pulls back, aims and dives again, and after the
    // last dive flies back. It hurts on touch only while attacking, from the first shake to the last dive sticking
    // (a trigger with DamageOnContact on this object; the component is switched on and off with the attack, the
    // collider stays on so the player can always pogo off the needle with a PogoTarget next to it).
    // Draw the sprite pointing down: the needle's local down is its point. Grandma drives it (GrandmaBoss).
    [RequireComponent(typeof(Rigidbody2D))]
    public class Needle : MonoBehaviour
    {
        [Tooltip("Units from the needle's centre to its point.")]
        [SerializeField, Min(0f)] private float pointLength = 0.8f;
        [Tooltip("What the needle sticks into.")]
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Read only: the state it is in right now.")]
        [SerializeField] private string currentState;

        private Vector2 localSpot;
        private DamageOnContact damage;

        public GrandmaBoss Boss { get; private set; }
        public GrandmaStats Stats => Boss.Stats;
        public Rigidbody2D Body { get; private set; }
        public EnemyFlash Flash { get; private set; }
        public StateMachine Fsm { get; private set; }

        public NeedleHoverState Hover { get; private set; }
        public NeedleAimState Aim { get; private set; }
        public NeedleDiveState Dive { get; private set; }
        public NeedleStuckState Stuck { get; private set; }
        public NeedlePullBackState PullBack { get; private set; }
        public NeedleReturnState Return { get; private set; }
        public NeedleDropState Dropping { get; private set; }
        public NeedleAwayState Away { get; private set; }

        // Set by the aim, used by the dive (and the pull back that follows it).
        public Vector2 DiveDirection { get; set; } = Vector2.down;

        // Dives left in the current attack after the one under way.
        public int ChainLeft { get; set; }

        // Where it waits between attacks.
        public Vector2 Spot => Boss.transform.TransformPoint(localSpot);

        // Where its point is and which way it points.
        public Vector2 Forward => -(Vector2)transform.up;
        public Vector2 PointPosition => Body.position + Forward * pointLength;

        // Taking part in the fight (not put away).
        public bool IsOut => Fsm != null && Fsm.Current != Away;

        // Waiting at its spot, free to attack.
        public bool IsWaiting => Fsm != null && Fsm.Current == Hover;

        // Not busy with an attack or lying on the ground: waiting, or flying back to its spot.
        public bool IsFree => Fsm != null && (Fsm.Current == Hover || Fsm.Current == Return);

        // From the first shake to the last dive sticking (Grandma does not fall asleep in the middle of it).
        public bool IsAttacking => Fsm != null && (Fsm.Current == Aim || Fsm.Current == Dive || Fsm.Current == PullBack
                                                   || (Fsm.Current == Stuck && ChainLeft > 0));

        // Called once by Grandma in her Awake. Puts the needle away until the fight starts.
        public void Init(GrandmaBoss boss)
        {
            Boss = boss;
            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Kinematic;
            Flash = GetComponent<EnemyFlash>();
            damage = GetComponent<DamageOnContact>();
            localSpot = boss.transform.InverseTransformPoint(transform.position);

            Fsm = new StateMachine();
            Hover = new NeedleHoverState(this);
            Aim = new NeedleAimState(this);
            Dive = new NeedleDiveState(this);
            Stuck = new NeedleStuckState(this);
            PullBack = new NeedlePullBackState(this);
            Return = new NeedleReturnState(this);
            Dropping = new NeedleDropState(this);
            Away = new NeedleAwayState(this);
            Fsm.SetState(Away);
        }

        // The fight starts: comes out of Grandma and flies to its spot.
        public void Rise()
        {
            gameObject.SetActive(true);
            Place(Boss.transform.position);
            SetPointing(Vector2.down);
            Fsm.SetState(Return, restart: true);
        }

        // She fell asleep: a waiting or returning needle drops to the ground. Stuck ones stay where they are.
        public void Drop()
        {
            if (Fsm.Current == Hover || Fsm.Current == Return)
                Fsm.SetState(Dropping);
        }

        // Starts an attack: one dive, or Chain Dives dives in a row (Chain Chance).
        public void Attack()
        {
            ChainLeft = Random.value < Stats.ChainChance ? Stats.ChainDives - 1 : 0;
            Fsm.SetState(Aim);
        }

        // Hidden until the fight starts again (lost attempt) or for good (she is defeated).
        public void PutAway()
        {
            ChainLeft = 0;
            Fsm.SetState(Away);
        }

        private void Update()
        {
            if (Fsm == null)
                return;
            Fsm.Tick();
            currentState = Fsm.Current?.GetType().Name;
            if (damage != null)
                damage.enabled = IsAttacking;
        }

        private void FixedUpdate() => Fsm?.FixedTick();

        // Ground or wall in front of the point within distance along direction.
        public RaycastHit2D PointHit(Vector2 direction, float distance) =>
            Physics2D.Raycast(PointPosition, direction, distance, groundLayers);

        // Moves so the point sinks StickDepth past where it hit.
        public void StickAt(RaycastHit2D hit, Vector2 direction)
        {
            Body.MovePosition(hit.point + direction * (Stats.StickDepth - pointLength));
        }

        // Turns straight away so the point faces direction.
        public void SetPointing(Vector2 direction)
        {
            float angle = AngleOf(direction);
            Body.rotation = angle;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // Turns towards direction at most maxDegrees this physics step.
        public void TurnTowards(Vector2 direction, float maxDegrees)
        {
            Body.MoveRotation(Mathf.MoveTowardsAngle(Body.rotation, AngleOf(direction), maxDegrees));
        }

        private void Place(Vector2 position)
        {
            Body.position = position;
            transform.position = position;
        }

        // Rotation (degrees) at which the local down axis points along direction.
        private static float AngleOf(Vector2 direction) => Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 point = transform.position - transform.up * pointLength;
            Gizmos.DrawLine(transform.position, point);
            Gizmos.DrawWireSphere(point, 0.08f);
        }
#endif
    }
}
