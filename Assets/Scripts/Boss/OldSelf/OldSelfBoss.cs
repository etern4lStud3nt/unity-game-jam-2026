using Enemies;
using Unity.Cinemachine;
using UnityEngine;

namespace Bosses
{
    // The Old Self, the B3 boss: a fast duelist that never stands still. It pauses briefly, maybe hops to a new
    // spot, then picks an attack at random (weighted, never the same twice in a row): Lunge along the ground, Air Dive
    // (jump, hang, dive at the player), Throw (flies out and comes back) or, up close, Slash. Every attack shakes
    // first as a warning and is followed by a short opening (Recover). Too many hits stagger it for a bigger opening.
    // In the second half the pauses shrink, attacks can chain and it lays traps on the floor and in the air.
    //
    // The fight start (Start Signal), the arena, a lost attempt, its death and its reward work as for every boss
    // (BossFight); BossPhaseChanged goes out when the second half begins.
    //
    // Physics: a dynamic Rigidbody2D with a solid collider (it lands on any floor) that the player passes through,
    // plus a Hurtbox child (trigger + DamageOnContact) for contact damage. The motor moves it, owns its gravity and
    // counts its landings. Keep the thrown object, the traps and the reward outside its object (siblings under the
    // prefab root), or player hits on them would hurt the boss.
    [RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(EnemyMotor))]
    public class OldSelfBoss : BossFight
    {
        [SerializeField] private OldSelfStats stats;
        [Tooltip("The slash's damage area (trigger + DamageOnContact), switched on during a slash. Put it under the " +
                 "Visual child so it flips with the boss.")]
        [SerializeField] private GameObject slashHitbox;
        [Tooltip("Where thrown objects leave from and are caught. Empty = the body centre.")]
        [SerializeField] private Transform hand;
        [SerializeField] private OldSelfProjectile projectile;
        [Tooltip("Traps laid in the second half (see Trap Every Attacks).")]
        [SerializeField] private OldSelfTrap[] traps;
        [Tooltip("Squashed while crouching before a lunge. Usually the Visual child.")]
        [SerializeField] private Transform visual;

        private Collider2D[] ownColliders;
        private Collider2D[] playerColliders;
        private CinemachineImpulseSource impulse;
        private AfterimageTrail trail;
        private Vector3 visualScale;
        private int hitsSinceStagger;
        private int attacksSinceTraps;
        private IState lastAttack;
        private IState[] attacks;
        private readonly float[] attackWeights = new float[4];

        public OldSelfStats Stats => stats;
        public EnemyMotor Motor { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public OldSelfProjectile Projectile => projectile;
        // Bends the sprite (take-offs, landings, leans, hits); optional.
        public SpriteJuice Juice { get; private set; }
        // Floor and walls: the motor's Ground layers.
        public LayerMask GroundLayers => Motor.GroundLayers;

        public OldSelfWaitState Waiting { get; private set; }
        public OldSelfIdleState Idle { get; private set; }
        public OldSelfHopState Hop { get; private set; }
        public OldSelfLungeState Lunge { get; private set; }
        public OldSelfAirDiveState AirDive { get; private set; }
        public OldSelfThrowState Throw { get; private set; }
        public OldSelfSlashState Slash { get; private set; }
        public OldSelfRecoverState Recover { get; private set; }
        public OldSelfStaggerState Stagger { get; private set; }
        public OldSelfDeadState Dead { get; private set; }

        protected override IState WaitingState => Waiting;
        protected override IState DefeatedState => Dead;

        // True when the attack that just ended was chained onto another (so chains stay two long).
        public bool Chained { get; set; }

        public bool InPhase2 { get; private set; }

        // Pauses, warnings and openings shrink in the second half.
        public float TimeScale => InPhase2 ? stats.Phase2TimeScale : 1f;

        public int PlayerSide => PlayerPoint.x >= Body.position.x ? 1 : -1;
        public float PlayerDistanceX => Mathf.Abs(PlayerPoint.x - Body.position.x);
        public Vector2 HandPoint => hand != null ? (Vector2)hand.position : Body.worldCenterOfMass;
        public bool IsGrounded => Motor.IsGrounded;
        public bool WallAhead => Motor.WallAhead;

        protected override void Awake()
        {
            base.Awake();
            Motor = GetComponent<EnemyMotor>();
            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.freezeRotation = true;
            impulse = GetComponent<CinemachineImpulseSource>();
            Juice = GetComponent<SpriteJuice>();
            trail = GetComponent<AfterimageTrail>();
            ownColliders = GetComponentsInChildren<Collider2D>(true);
            if (visual != null)
                visualScale = visual.localScale;

            if (stats == null)
            {
                Debug.LogWarning($"{name} has no {nameof(OldSelfStats)} asset; the fight cannot start.", this);
                enabled = false;
                return;
            }

            if (projectile != null)
                projectile.Init(this);
            if (traps != null)
                foreach (OldSelfTrap trap in traps)
                    if (trap != null)
                        trap.Init(this);
            SetSlashHitbox(false);

            Waiting = new OldSelfWaitState(this);
            Idle = new OldSelfIdleState(this);
            Hop = new OldSelfHopState(this);
            Lunge = new OldSelfLungeState(this);
            AirDive = new OldSelfAirDiveState(this);
            Throw = new OldSelfThrowState(this);
            Slash = new OldSelfSlashState(this);
            Recover = new OldSelfRecoverState(this);
            Stagger = new OldSelfStaggerState(this);
            Dead = new OldSelfDeadState(this);
            attacks = new IState[] { Lunge, AirDive, Throw, Slash };
        }

        protected override void Start()
        {
            base.Start();
            if (HasPlayer)
            {
                playerColliders = Player.GetComponentsInChildren<Collider2D>();
                PlayerPassThrough.Ignore(ownColliders, playerColliders);
            }
        }

        protected override void FixedUpdate()
        {
            PlayerPassThrough.Keep(ownColliders, playerColliders);
            Motor.TrackLanding();
            base.FixedUpdate();
        }

        // ---------- Fight flow ----------

        protected override void OnFightStarted() => Fsm.SetState(Idle);

        // Picks the next attack: weighted at random, never the last one again, Slash only when the player is close.
        public void StartAttack()
        {
            float[] weights = attackWeights;
            weights[0] = stats.LungeWeight;
            weights[1] = stats.AirDiveWeight;
            weights[2] = projectile != null ? stats.ThrowWeight : 0f;
            weights[3] = PlayerDistanceX <= stats.SlashRange ? stats.SlashWeight : 0f;

            float total = 0f;
            for (int i = 0; i < attacks.Length; i++)
            {
                if (attacks[i] == lastAttack)
                    weights[i] = 0f;
                total += weights[i];
            }

            IState next = Lunge;
            if (total > 0f)
            {
                float pick = Random.value * total;
                for (int i = 0; i < attacks.Length; i++)
                {
                    pick -= weights[i];
                    if (weights[i] > 0f && pick <= 0f)
                    {
                        next = attacks[i];
                        break;
                    }
                }
            }

            lastAttack = next;
            CountAttack();
            Fsm.SetState(next, restart: true);
        }

        // In the second half traps go down every Trap Every Attacks attacks.
        private void CountAttack()
        {
            if (!InPhase2 || stats.TrapEveryAttacks <= 0)
                return;
            attacksSinceTraps++;
            if (attacksSinceTraps >= stats.TrapEveryAttacks)
                LayTraps();
        }

        private void CheckPhase2()
        {
            if (InPhase2 || stats.Phase2Health <= 0f
                || Health.CurrentHealth > Mathf.CeilToInt(Health.MaxHealth * stats.Phase2Health))
                return;
            InPhase2 = true;
            PublishPhase(2);
            LayTraps();
        }

        // Lays Trap Count traps spread across the arena: on the floor, or (Air Trap Chance) hanging in the air
        // Air Trap Height above it, kept below any ceiling.
        private void LayTraps()
        {
            attacksSinceTraps = 0;
            if (traps == null || Arena == null)
                return;
            Bounds bounds = Arena.bounds;
            float left = bounds.min.x + stats.EdgeMargin;
            float right = bounds.max.x - stats.EdgeMargin;
            int count = Mathf.Min(stats.TrapCount, traps.Length);
            if (count <= 0 || right <= left)
                return;

            // One trap per equal slice of the arena, somewhere inside its slice, so they never bunch up.
            const float sliceJitter = 0.3f;
            float slice = (right - left) / count;
            for (int i = 0; i < count; i++)
            {
                if (traps[i] == null)
                    continue;
                float x = left + slice * (i + 0.5f + Random.Range(-sliceJitter, sliceJitter));
                if (!FloorBelow(new Vector2(x, bounds.max.y), bounds.size.y, out Vector2 floor))
                    continue;
                traps[i].Place(Random.value < stats.AirTrapChance ? InTheAir(floor, traps[i].Height) : floor);
            }
        }

        // A point Air Trap Height above floor, lowered so a trap trapHeight tall stays clear of any ceiling.
        private Vector2 InTheAir(Vector2 floor, float trapHeight)
        {
            float headroom = trapHeight + 0.1f;
            float height = Random.Range(stats.AirTrapHeight.x, stats.AirTrapHeight.y);
            RaycastHit2D ceiling = Physics2D.Raycast(floor + Vector2.up * 0.1f, Vector2.up, height + headroom, GroundLayers);
            if (ceiling.collider != null)
                height = Mathf.Max(0f, ceiling.distance - headroom);
            return floor + Vector2.up * height;
        }

        // True when there is ground to land on under x, no more than maxDrop below fromY.
        public bool HasFloorAt(float x, float fromY, float maxDrop)
        {
            var from = new Vector2(x, fromY + 0.5f);
            return Physics2D.Raycast(from, Vector2.down, maxDrop + 0.5f, GroundLayers).collider != null;
        }

        // The lowest ground surface under from (the arena floor, not the ceiling or a ledge above it). Walks down the
        // column: steps out of any ground it starts in (the ceiling), casts down to the next surface, and carries on
        // below it. The ground is often one composite collider, so a single RaycastAll would stop at the ceiling.
        private bool FloorBelow(Vector2 from, float depth, out Vector2 point)
        {
            const float step = 0.25f;
            point = default;
            bool found = false;
            float bottom = from.y - depth;
            Vector2 probe = from;
            LayerMask ground = GroundLayers;
            for (int guard = 0; guard < 400 && probe.y > bottom; guard++)
            {
                if (Physics2D.OverlapPoint(probe, ground) != null)
                {
                    probe.y -= step;
                    continue;
                }
                RaycastHit2D hit = Physics2D.Raycast(probe, Vector2.down, probe.y - bottom, ground);
                if (hit.collider == null)
                    break;
                point = hit.point;
                found = true;
                probe = hit.point + Vector2.down * step;
            }
            return found;
        }

        // ---------- Helpers for the states ----------

        public void Face(int direction) => Motor.Face(direction);

        public void FacePlayer() => Motor.Face(PlayerSide);

        // An x position inside the arena, Edge Margin from its sides.
        public float ClampToArena(float x)
        {
            if (Arena == null)
                return x;
            Bounds bounds = Arena.bounds;
            return Mathf.Clamp(x, bounds.min.x + stats.EdgeMargin, bounds.max.x - stats.EdgeMargin);
        }

        public void SetCrouch(bool on)
        {
            if (visual == null)
                return;
            Vector3 scale = visual.localScale;
            scale.y = visualScale.y * (on ? stats.CrouchScale : 1f);
            visual.localScale = scale;
        }

        public void SetWarning(bool on)
        {
            if (Flash != null)
                Flash.SetWarning(on);
        }

        public void SetSlashHitbox(bool on)
        {
            if (slashHitbox != null)
                slashHitbox.SetActive(on);
        }

        public void Punch(Vector2 scale)
        {
            if (Juice != null)
                Juice.Punch(scale);
        }

        // Afterimages behind it, for the dash and the dive only.
        public void SetTrail(bool on)
        {
            if (trail != null)
                trail.Emitting = on;
        }

        public void Shake(float force)
        {
            if (impulse != null && force > 0f)
                impulse.GenerateImpulseWithForce(force);
        }

        // Puts every move's effects back to rest. Runs after every state's exit (OldSelfState), so nothing a state
        // switched on (warning, crouch, slash hitbox, trail, changed gravity, sideways speed) outlives it.
        public void StopAttackEffects()
        {
            SetWarning(false);
            SetCrouch(false);
            SetSlashHitbox(false);
            SetTrail(false);
            Motor.RestoreGravity();
            Motor.Stop();
        }

        public void PutAwayThrownAndTraps()
        {
            if (projectile != null)
                projectile.PutAway();
            if (traps != null)
                foreach (OldSelfTrap trap in traps)
                    if (trap != null)
                        trap.PutAway();
        }

        // ---------- Hits and a lost attempt ----------

        protected override void OnHit(int amount)
        {
            CheckPhase2();
            if (stats.HitsToStagger <= 0 || Fsm.Current == Waiting || Fsm.Current == Stagger)
                return;
            hitsSinceStagger++;
            if (hitsSinceStagger >= stats.HitsToStagger)
            {
                hitsSinceStagger = 0;
                Fsm.SetState(Stagger);
            }
        }

        // Back to the start: thrown object and traps put away, first half, standing where it was placed.
        protected override void ResetForRetry()
        {
            PutAwayThrownAndTraps();
            InPhase2 = false;
            hitsSinceStagger = 0;
            attacksSinceTraps = 0;
            lastAttack = null;
            Chained = false;
            Motor.SetVelocity(Vector2.zero);
            Body.position = Home;
            transform.position = Home;
        }
    }
}
