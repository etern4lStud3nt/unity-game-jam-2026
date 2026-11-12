using Enemies;
using Unity.Cinemachine;
using UnityEngine;

namespace Bosses
{
    // The Old Self, the B3 boss: a fast duelist that never stands still. It pauses briefly, maybe hops to a new
    // spot, then picks an attack at random (weighted, never the same twice in a row): Lunge along the ground, Air Dive
    // (jump, hang, dive at the player), Throw (flies out and comes back) or, up close, Slash. Every attack shakes
    // first as a warning and is followed by a short opening (Recover). Too many hits stagger it for a bigger opening.
    // In the second half the pauses shrink, attacks can chain and it lays traps on the floor.
    //
    // The fight starts on the Yarn signal Start Signal or StartFight() (empty signal = when the player is in the
    // arena) and publishes BossFightStarted / BossPhaseChanged / BossDefeated like Grandma. If the player dies, the
    // fight goes back to the start.
    //
    // Physics: a dynamic Rigidbody2D with a solid collider (it lands on any floor) that the player passes through,
    // plus a Hurtbox child (trigger + DamageOnContact) for contact damage. Keep the thrown object, the traps and
    // the reward outside its object (siblings under the prefab root), or player hits on them would hurt the boss.
    [RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(EnemyMotor))]
    public class OldSelfBoss : MonoBehaviour
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
        [Tooltip("The arena. Empty = the LevelRoom trigger it was placed in.")]
        [SerializeField] private Collider2D arena;
        [Tooltip("Floor and walls (the tilemap's Ground layer); used to place traps and stop thrown objects.")]
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Name of the Yarn <<signal ...>> that starts the fight. Empty = it starts when the player is in the arena.")]
        [SerializeField] private string startSignal;
        [Tooltip("Squashed while crouching before a lunge. Usually the Visual child.")]
        [SerializeField] private Transform visual;
        [Tooltip("Switched on after it dies: its lucky number pickup, placed where it should appear. Optional.")]
        [SerializeField] private GameObject reward;
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Read only: the state it is in right now.")]
        [SerializeField] private string currentState;

        private Collider2D[] ownColliders;
        private Collider2D[] solidColliders;
        private Collider2D[] playerColliders;
        private Collider2D playerCollider;
        private Rigidbody2D playerBody;
        private CinemachineImpulseSource impulse;
        private AfterimageTrail trail;
        private Vector2 home;
        private Vector3 visualScale;
        private float baseGravityScale;
        private int hitsSinceStagger;
        private int attacksSinceTraps;
        private bool introDone;
        private bool phase2Started;
        private IState lastAttack;

        public OldSelfStats Stats => stats;
        public Health Health { get; private set; }
        public EnemyFlash Flash { get; private set; }
        public EnemyMotor Motor { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public StateMachine Fsm { get; private set; }
        public OldSelfProjectile Projectile => projectile;
        // Bends the sprite (take-offs, landings, leans, hits); optional.
        public SpriteJuice Juice { get; private set; }
        public LayerMask GroundLayers => groundLayers;
        public string RoomId { get; private set; }

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

        // True when the attack that just ended was chained onto another (so chains stay two long).
        public bool Chained { get; set; }

        public bool InPhase2 => phase2Started;

        // Pauses, warnings and openings shrink in the second half.
        public float TimeScale => InPhase2 ? stats.Phase2TimeScale : 1f;

        public bool HasPlayer => playerCollider != null;
        public Vector2 PlayerPoint => HasPlayer ? (Vector2)playerCollider.bounds.center : Body.position;
        public Vector2 PlayerVelocity => playerBody != null ? playerBody.linearVelocity : Vector2.zero;
        public int PlayerSide => PlayerPoint.x >= Body.position.x ? 1 : -1;
        public float PlayerDistanceX => Mathf.Abs(PlayerPoint.x - Body.position.x);
        public bool PlayerInArena => HasPlayer && arena != null && arena.OverlapPoint(PlayerPoint);
        public Vector2 HandPoint => hand != null ? (Vector2)hand.position : Body.worldCenterOfMass;
        public bool ShouldStartOnSight => introDone || string.IsNullOrEmpty(startSignal);

        private void Awake()
        {
            Health = GetComponent<Health>();
            Flash = GetComponent<EnemyFlash>();
            Motor = GetComponent<EnemyMotor>();
            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.freezeRotation = true;
            baseGravityScale = Body.gravityScale;
            impulse = GetComponent<CinemachineImpulseSource>();
            Juice = GetComponent<SpriteJuice>();
            trail = GetComponent<AfterimageTrail>();
            ownColliders = GetComponentsInChildren<Collider2D>(true);
            solidColliders = System.Array.FindAll(ownColliders, own => !own.isTrigger);
            home = transform.position;
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

            Fsm = new StateMachine();
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
        }

        private void OnEnable()
        {
            if (Fsm == null)
                return;
            Health.Damaged += OnDamaged;
            Health.Death += OnDeath;
            EventBus.Subscribe<DialogueSignal>(OnDialogueSignal);
            EventBus.Subscribe<PlayerRespawned>(OnPlayerRespawned);
        }

        private void OnDisable()
        {
            if (Fsm == null)
                return;
            Health.Damaged -= OnDamaged;
            Health.Death -= OnDeath;
            EventBus.Unsubscribe<DialogueSignal>(OnDialogueSignal);
            EventBus.Unsubscribe<PlayerRespawned>(OnPlayerRespawned);
        }

        // In Start, once every collider of the scene is enabled, so the room around it can be found.
        private void Start()
        {
            if (arena == null)
                arena = FindRoomAt(transform.position);
            if (arena == null)
                Debug.LogWarning($"{name} found no LevelRoom trigger around it; assign the Arena.", this);
            else if (arena.TryGetComponent(out LevelRoom room))
                RoomId = room.RoomId;

            GameObject found = GameObject.FindWithTag(playerTag);
            if (found != null)
            {
                playerBody = found.GetComponent<Rigidbody2D>();
                playerCollider = found.GetComponent<Collider2D>();
                playerColliders = found.GetComponentsInChildren<Collider2D>();
                IgnorePlayer();
            }
            if (reward != null)
                reward.SetActive(false);
            Fsm.SetState(Waiting);
        }

        private void Update()
        {
            Fsm.Tick();
            currentState = Fsm.Current?.GetType().Name;
        }

        private void FixedUpdate()
        {
            KeepIgnoringPlayer();
            Fsm.FixedTick();
        }

        // ---------- Fight flow ----------

        public void StartFight()
        {
            if (Fsm == null || Fsm.Current != Waiting)
                return;
            introDone = true;
            EventBus.Publish(new BossFightStarted { RoomId = RoomId });
            Fsm.SetState(Idle);
        }

        // Picks the next attack: weighted at random, never the last one again, Slash only when the player is close.
        public void StartAttack()
        {
            float slash = PlayerDistanceX <= stats.SlashRange ? stats.SlashWeight : 0f;
            IState[] options = { Lunge, AirDive, Throw, Slash };
            float[] weights = { stats.LungeWeight, stats.AirDiveWeight, Projectile != null ? stats.ThrowWeight : 0f, slash };

            float total = 0f;
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == lastAttack)
                    weights[i] = 0f;
                total += weights[i];
            }

            IState next = Lunge;
            if (total > 0f)
            {
                float pick = Random.value * total;
                for (int i = 0; i < options.Length; i++)
                {
                    pick -= weights[i];
                    if (weights[i] > 0f && pick <= 0f)
                    {
                        next = options[i];
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
            if (phase2Started || Health.IsDead || stats.Phase2Health <= 0f
                || Health.CurrentHealth > Mathf.CeilToInt(Health.MaxHealth * stats.Phase2Health))
                return;
            phase2Started = true;
            EventBus.Publish(new BossPhaseChanged { RoomId = RoomId, Phase = 2 });
            LayTraps();
        }

        // Lays Trap Count traps spread across the arena: on the floor, or (Air Trap Chance) hanging in the air
        // Air Trap Height above it, kept below any ceiling.
        private void LayTraps()
        {
            attacksSinceTraps = 0;
            if (traps == null || arena == null)
                return;
            Bounds bounds = arena.bounds;
            float left = bounds.min.x + stats.EdgeMargin;
            float right = bounds.max.x - stats.EdgeMargin;
            int count = Mathf.Min(stats.TrapCount, traps.Length);
            if (count <= 0 || right <= left)
                return;

            // One trap per equal slice of the arena, somewhere inside its slice, so they never bunch up.
            float slice = (right - left) / count;
            for (int i = 0; i < count; i++)
            {
                if (traps[i] == null)
                    continue;
                float x = left + slice * (i + Random.Range(0.2f, 0.8f));
                if (!FloorBelow(new Vector2(x, bounds.max.y), bounds.size.y, out Vector2 floor))
                    continue;
                traps[i].Place(Random.value < stats.AirTrapChance ? InTheAir(floor) : floor);
            }
        }

        private Vector2 InTheAir(Vector2 floor)
        {
            const float headroom = 0.8f;
            float height = Random.Range(stats.AirTrapHeight.x, stats.AirTrapHeight.y);
            RaycastHit2D ceiling = Physics2D.Raycast(floor + Vector2.up * 0.1f, Vector2.up, height + headroom, groundLayers);
            if (ceiling.collider != null)
                height = Mathf.Max(0f, ceiling.distance - headroom);
            return floor + Vector2.up * height;
        }

        // True when there is ground to land on under x, no more than maxDrop below fromY.
        public bool HasFloorAt(float x, float fromY, float maxDrop)
        {
            var from = new Vector2(x, fromY + 0.5f);
            return Physics2D.Raycast(from, Vector2.down, maxDrop + 0.5f, groundLayers).collider != null;
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
            for (int guard = 0; guard < 400 && probe.y > bottom; guard++)
            {
                if (Physics2D.OverlapPoint(probe, groundLayers) != null)
                {
                    probe.y -= step;
                    continue;
                }
                RaycastHit2D hit = Physics2D.Raycast(probe, Vector2.down, probe.y - bottom, groundLayers);
                if (hit.collider == null)
                    break;
                point = hit.point;
                found = true;
                probe = hit.point + Vector2.down * step;
            }
            return found;
        }

        // ---------- Movement helpers (used by the states) ----------

        // Sets the gravity so a jump rises `height` units in `riseTime` seconds, and returns the take-off speed.
        public float JumpGravity(float height, float riseTime)
        {
            float gravity = 2f * height / (riseTime * riseTime);
            float world = Mathf.Abs(Physics2D.gravity.y);
            Body.gravityScale = world > 0f ? gravity / world : baseGravityScale;
            return gravity * riseTime;
        }

        public void SetGravity(bool on) => Body.gravityScale = on ? baseGravityScale : 0f;

        public void ResetGravity() => Body.gravityScale = baseGravityScale;

        public void StopX() => Body.linearVelocityX = 0f;

        public void Face(int direction) => Motor.Face(direction);

        public void FacePlayer() => Motor.Face(PlayerSide);

        public bool IsGrounded => Motor.IsGrounded;

        public bool WallAhead => Motor.WallAhead;

        // An x position inside the arena, Edge Margin from its sides.
        public float ClampToArena(float x)
        {
            if (arena == null)
                return x;
            Bounds bounds = arena.bounds;
            return Mathf.Clamp(x, bounds.min.x + stats.EdgeMargin, bounds.max.x - stats.EdgeMargin);
        }

        public void SetCrouch(bool on)
        {
            if (visual == null)
                return;
            Vector3 scale = visual.localScale;
            scale.y = visualScale.y * (on ? 0.8f : 1f);
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

        // Puts every attack effect back to rest: used when an attack is cut short (stagger, death, a lost attempt).
        public void StopAttackEffects()
        {
            SetWarning(false);
            SetCrouch(false);
            SetSlashHitbox(false);
            SetTrail(false);
            ResetGravity();
        }

        public void SetCollidersEnabled(bool on)
        {
            foreach (Collider2D own in ownColliders)
                if (own != null)
                    own.enabled = on;
        }

        public void ShowReward()
        {
            if (reward != null)
                reward.SetActive(true);
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

        // ---------- Events ----------

        private void OnDialogueSignal(DialogueSignal evt)
        {
            if (!string.IsNullOrEmpty(startSignal) && evt.Name == startSignal)
                StartFight();
        }

        private void OnDamaged(int amount)
        {
            if (Health.IsDead)
                return;
            if (Flash != null)
                Flash.Hit();
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

        private void OnDeath() => Fsm.SetState(Dead);

        // A lost attempt: back to the start, full health. A defeated Old Self stays defeated.
        private void OnPlayerRespawned(PlayerRespawned evt)
        {
            if (Fsm.Current == Dead)
                return;
            Health.ResetHealth();
            PutAwayThrownAndTraps();
            StopAttackEffects();
            phase2Started = false;
            hitsSinceStagger = 0;
            attacksSinceTraps = 0;
            lastAttack = null;
            Chained = false;
            Body.linearVelocity = Vector2.zero;
            Body.position = home;
            transform.position = home;
            Fsm.SetState(Waiting, restart: true);
        }

        // ---------- Player collisions ----------

        // The player walks through the boss (and takes contact damage from the Hurtbox) instead of standing on it.
        // Only the solid colliders: an ignored pair gets no trigger messages either, so ignoring the Hurtbox or the
        // slash hitbox would stop them hurting.
        private void IgnorePlayer()
        {
            if (playerColliders == null)
                return;
            foreach (Collider2D theirs in playerColliders)
                if (theirs != null)
                    foreach (Collider2D own in solidColliders)
                        if (own != null)
                            Physics2D.IgnoreCollision(own, theirs);
        }

        // Unity forgets ignored pairs when colliders are switched off and on, so check every physics step.
        private void KeepIgnoringPlayer()
        {
            if (playerColliders == null || solidColliders.Length == 0 || solidColliders[0] == null)
                return;
            foreach (Collider2D theirs in playerColliders)
                if (theirs != null && !Physics2D.GetIgnoreCollision(solidColliders[0], theirs))
                {
                    IgnorePlayer();
                    return;
                }
        }

        private static Collider2D FindRoomAt(Vector2 point)
        {
            foreach (Collider2D candidate in Physics2D.OverlapPointAll(point))
                if (candidate.isTrigger && candidate.GetComponent<LevelRoom>() != null)
                    return candidate;
            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && !string.IsNullOrEmpty(currentState))
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, currentState);
        }
#endif
    }
}
