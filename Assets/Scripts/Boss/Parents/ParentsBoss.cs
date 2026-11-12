using Enemies;
using Unity.Cinemachine;
using UnityEngine;

namespace Bosses
{
    // The final boss, in C3: the Mother and the Father fight together and share one health (the Health on this
    // object). This object is the director: it stays where it was placed (on the arena's floor) and tells the two
    // what to do, one move at a time (ParentsFightState). Between their moves the two are away; for every move one
    // appears, warns, strikes, stays open for a moment and leaves again:
    // - Dash: one of them appears at a side of the room and dashes across the floor;
    // - Dive: the Father appears above the player, hangs, dives straight down and stays stuck in the ground;
    // - Volley: the Mother appears at a side of the room and throws waves of slippers flying flat in stacked lanes,
    //   each wave with a gap;
    // - Combo: the Mother's volley with the Father's dive coming in the middle of it;
    // - Cross dive: both hang up left and up right of the player, then dive diagonally through the spot the
    //   player stood on, one after the other, so their paths cross.
    // The two never strike at the very same moment. In the second half dives and dashes chain, volleys grow and
    // the combo comes more often.
    //
    // When the shared health runs out the one that took the last hit falls, the other walks over, they say their
    // last words (a Yarn node) and fade out, leaving the reward (ParentsDeadState).
    //
    // The fight start (Start Signal), the arena, a lost attempt and the reward work as for every boss (BossFight);
    // BossPhaseChanged goes out when the second half begins.
    //
    // Layout: this object holds the Mother and the Father as children (each with a stand-in Health, see ParentActor)
    // and has no body, collider or EnemyFlash of its own. Keep the slippers and the reward outside it (siblings
    // under the prefab root), or player hits on them would hurt the boss.
    public class ParentsBoss : BossFight
    {
        // How far below where it was placed the arena's floor is looked for.
        private const float FloorProbe = 10f;

        [SerializeField] private ParentsStats stats;
        [SerializeField] private FatherActor father;
        [SerializeField] private MotherActor mother;
        [Tooltip("The Mother's slippers: as many as can be in the air at once (lanes without the gap, times the " +
                 "waves in flight).")]
        [SerializeField] private ParentSlipper[] slippers;

        private CinemachineImpulseSource impulse;

        public ParentsStats Stats => stats;
        public FatherActor Father => father;
        public MotherActor Mother => mother;

        public ParentsWaitState Waiting { get; private set; }
        public ParentsFightState Fighting { get; private set; }
        public ParentsDeadState Dead { get; private set; }

        protected override IState WaitingState => Waiting;
        protected override IState DefeatedState => Dead;

        // The one the player hit last: the one that falls in the ending.
        public ParentActor LastHit { get; private set; }

        public bool InPhase2 { get; private set; }
        public bool IsDefeated => Fsm.Current == Dead;

        // Conversations that have ended since the scene started; the ending waits for the last words with it.
        public int DialoguesEnded { get; private set; }

        // Floor and walls: the Mother's motor's Ground layers.
        public LayerMask GroundLayers => mother.Motor.GroundLayers;

        // Height of the arena's floor: the solid ground under where the boss was placed (platforms do not count).
        // Dashes run along it, dives end on it and the slipper lanes are measured from it.
        public float FloorY { get; private set; }

        public float IdleBetween => InPhase2 ? stats.Phase2IdleBetween : stats.IdleBetween;
        public float ComboChance => InPhase2 ? stats.Phase2ComboChance : stats.ComboChance;
        public int WaveCount => InPhase2 ? stats.Phase2WaveCount : stats.WaveCount;
        public int DiveChain =>
            InPhase2 ? Random.Range(stats.Phase2DiveChain.x, stats.Phase2DiveChain.y + 1) : 1;
        public float CrossDiveWeight => InPhase2 ? stats.Phase2CrossDiveWeight : stats.CrossDiveWeight;
        // A chained dash goes there and back.
        public int DashChain => InPhase2 ? 2 : 1;

        protected override void Awake()
        {
            base.Awake();
            impulse = GetComponent<CinemachineImpulseSource>();

            if (stats == null || father == null || mother == null)
            {
                Debug.LogWarning($"{name} needs its {nameof(ParentsStats)} asset, the Father and the Mother; " +
                                 "the fight cannot start.", this);
                enabled = false;
                return;
            }

            father.Init(this);
            mother.Init(this);
            if (slippers != null)
                foreach (ParentSlipper slipper in slippers)
                    if (slipper != null)
                        slipper.Init(this);

            Waiting = new ParentsWaitState(this);
            Fighting = new ParentsFightState(this);
            Dead = new ParentsDeadState(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus.Subscribe<DialogueEnded>(OnDialogueEnded);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Unsubscribe<DialogueEnded>(OnDialogueEnded);
        }

        protected override void Start()
        {
            FloorY = FindFloor();
            base.Start();
        }

        private float FindFloor()
        {
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(Home, Vector2.down, FloorProbe, GroundLayers))
                if (hit.collider != null && !hit.collider.usedByEffector)
                    return hit.point.y;
            Debug.LogWarning($"{name} found no floor under it; place it on the arena's floor.", this);
            return Home.y;
        }

        private void OnDialogueEnded(DialogueEnded evt) => DialoguesEnded++;

        // ---------- Fight flow ----------

        protected override void OnFightStarted() => Fsm.SetState(Fighting);

        // A player hit on one of the two (from its stand-in Health): comes off the shared health.
        public void TakeHit(ParentActor who, int amount)
        {
            if (Fsm.Current != Fighting)
                return;
            LastHit = who;
            Health.TakeDamage(amount);
        }

        protected override void OnHit(int amount)
        {
            if (InPhase2 || stats.Phase2Health <= 0f
                || Health.CurrentHealth > Mathf.CeilToInt(Health.MaxHealth * stats.Phase2Health))
                return;
            InPhase2 = true;
            PublishPhase(2);
            Shake(stats.PhaseShake);
        }

        // Back to the start: slippers put away, first half, both where they were placed.
        protected override void ResetForRetry()
        {
            PutAwaySlippers();
            InPhase2 = false;
            LastHit = null;
            father.ResetForRetry();
            mother.ResetForRetry();
        }

        // ---------- Helpers for the states ----------

        // A slipper that is not in use, or null when all are.
        public ParentSlipper TakeSlipper()
        {
            if (slippers != null)
                foreach (ParentSlipper slipper in slippers)
                    if (slipper != null && !slipper.InUse)
                        return slipper;
            return null;
        }

        public void PutAwaySlippers()
        {
            if (slippers == null)
                return;
            foreach (ParentSlipper slipper in slippers)
                if (slipper != null)
                    slipper.PutAway();
        }

        // An x position inside the arena, Edge Margin from its sides.
        public float ClampToArena(float x)
        {
            if (Arena == null)
                return x;
            Bounds bounds = Arena.bounds;
            return Mathf.Clamp(x, bounds.min.x + stats.EdgeMargin, bounds.max.x - stats.EdgeMargin);
        }

        // The x of a side of the room (-1 left, 1 right), Edge Margin in from the arena's edge.
        public float SideX(int side)
        {
            if (Arena == null)
                return Home.x;
            Bounds bounds = Arena.bounds;
            return side >= 0 ? bounds.max.x - stats.EdgeMargin : bounds.min.x + stats.EdgeMargin;
        }

        // The side of the room (-1 left, 1 right) that is farther from the player.
        public int FarSide => Arena != null && PlayerPoint.x > Arena.bounds.center.x ? -1 : 1;

        public void Shake(float force)
        {
            if (impulse != null && force > 0f)
                impulse.GenerateImpulseWithForce(force);
        }
    }
}
