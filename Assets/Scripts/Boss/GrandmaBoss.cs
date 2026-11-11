using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Bosses
{
    // Grandma, the A3 boss. While awake she walks away from the player, jumps away (after a crouch) when they get
    // close, and her knitting needles attack, each on its own but never starting together, in quick chains of dives.
    // The fight starts with one needle and more join as the needles attack; in the second half more needles join.
    // After a number of attacks she falls asleep and the needles drop, which is the safe moment to hit her. She can
    // be hit at any time, but a hit while she is awake sends a needle at the player; touching her hurts (a
    // DamageOnContact trigger, like the enemies' Hurtbox).
    //
    // The fight starts on the Yarn signal Start Signal (the end of her intro conversation) or StartFight(), and
    // publishes BossFightStarted (BossPhaseChanged when the second half begins); her death publishes BossDefeated
    // and switches on the reward. If the player dies,
    // the fight goes back to the start, and being in the arena again restarts it without the intro.
    //
    // Keep the needles and the reward outside her object (siblings under the prefab root): a player hit finds the
    // Health above the collider it touched, so a needle under her would pass its hits on to her.
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public class GrandmaBoss : MonoBehaviour
    {
        [SerializeField] private GrandmaStats stats;
        [Tooltip("Her needles, in the order they join (see Starting Needles). Each attacks on its own; where it sits " +
                 "in the prefab (relative to her) is its waiting spot.")]
        [SerializeField] private Needle[] needles;
        [Tooltip("Needles that join in the second half (see Phase 2 Health), the first time she is awake there.")]
        [SerializeField] private Needle[] phase2Needles;
        [Tooltip("The arena; she walks inside it. Empty = the LevelRoom trigger she was placed in.")]
        [SerializeField] private Collider2D arena;
        [Tooltip("Walls that stop her walk.")]
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Name of the Yarn <<signal ...>> that starts the fight. Empty = it starts when the player is in the arena.")]
        [SerializeField] private string startSignal = "grandma_fight";
        [Tooltip("Flipped to face where she walks, squashed while she crouches to jump. Usually the Visual child.")]
        [SerializeField] private Transform visual;
        [Tooltip("Switched on while she sleeps (a Zzz...). Optional.")]
        [SerializeField] private GameObject sleepVisual;
        [Tooltip("Switched on after she dies: her lucky number pickup, placed where it should appear. Optional.")]
        [SerializeField] private GameObject reward;
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Read only: the state she is in right now.")]
        [SerializeField] private string currentState;

        private readonly List<Needle> allNeedles = new List<Needle>();
        private Collider2D[] ownColliders;
        private Collider2D body;
        private Transform player;
        private Rigidbody2D playerBody;
        private Collider2D playerCollider;
        private Vector2 home;
        private Vector3 visualScale;
        private float? walkTarget;
        private JumpPhase jumpPhase;
        private Vector2 jumpStart;
        private float jumpTargetX;
        private float jumpTimer;
        private float nextJumpTime;
        private float nextCounterTime;
        private float nextAttackTime;
        private int needlesOut;
        private int attacksSinceNeedle;
        private bool introDone;
        private bool phase2Started;

        public GrandmaStats Stats => stats;
        public Health Health { get; private set; }
        public EnemyFlash Flash { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public StateMachine Fsm { get; private set; }
        public IReadOnlyList<Needle> AllNeedles => allNeedles;
        public string RoomId { get; private set; }

        public GrandmaWaitState Waiting { get; private set; }
        public GrandmaAttackState Attacking { get; private set; }
        public GrandmaSleepState Sleeping { get; private set; }
        public GrandmaDeadState Dead { get; private set; }

        // Needle attacks left before she falls asleep (set when she wakes up).
        public int AttacksLeft { get; set; }

        // For animation: whether she is walking and which way she faces (1 = right, -1 = left).
        public bool IsWalking => walkTarget.HasValue;
        public int Facing { get; private set; } = 1;

        // The needles only fly while she is awake and fighting.
        public bool IsAwake => Fsm != null && Fsm.Current == Attacking;

        public bool InPhase2 => Health.CurrentHealth <= Mathf.CeilToInt(Health.MaxHealth * stats.Phase2Health);

        // Waiting times shrink in the second half of the fight.
        public float WaitScale => InPhase2 ? stats.Phase2WaitScale : 1f;

        public bool HasPlayer => player != null;
        public Vector2 PlayerPoint => playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)player.position;
        public Vector2 PlayerVelocity => playerBody != null ? playerBody.linearVelocity : Vector2.zero;

        public bool PlayerInArena => HasPlayer && arena != null && arena.OverlapPoint(PlayerPoint);

        // The fight starts by itself once the player is in the arena, unless the intro has not run yet.
        public bool ShouldStartOnSight => introDone || string.IsNullOrEmpty(startSignal);

        private void Awake()
        {
            Health = GetComponent<Health>();
            Flash = GetComponent<EnemyFlash>();
            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Kinematic;
            body = GetComponent<Collider2D>();
            ownColliders = GetComponentsInChildren<Collider2D>(true);
            home = transform.position;
            if (visual != null)
                visualScale = visual.localScale;

            if (stats == null)
            {
                Debug.LogWarning($"{name} has no {nameof(GrandmaStats)} asset; the fight cannot start.", this);
                enabled = false;
                return;
            }

            AddNeedles(needles);
            AddNeedles(phase2Needles);
            foreach (Needle needle in allNeedles)
                needle.Init(this);

            Fsm = new StateMachine();
            Waiting = new GrandmaWaitState(this);
            Attacking = new GrandmaAttackState(this);
            Sleeping = new GrandmaSleepState(this);
            Dead = new GrandmaDeadState(this);
        }

        private void AddNeedles(Needle[] list)
        {
            if (list == null)
                return;
            foreach (Needle needle in list)
                if (needle != null && !allNeedles.Contains(needle))
                    allNeedles.Add(needle);
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

        // In Start, once every collider of the scene is enabled, so the room around her can be found.
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
                player = found.transform;
                playerBody = found.GetComponent<Rigidbody2D>();
                playerCollider = found.GetComponent<Collider2D>();
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

        private void FixedUpdate() => Fsm.FixedTick();

        // Starts the fight (BossFightStarted goes out, the first needles rise). Does nothing if it is already on or
        // she is defeated.
        public void StartFight()
        {
            if (Fsm == null || Fsm.Current != Waiting)
                return;
            introDone = true;
            needlesOut = 0;
            attacksSinceNeedle = 0;
            EventBus.Publish(new BossFightStarted { RoomId = RoomId });
            while (needlesOut < Mathf.Min(stats.StartingNeedles, needles.Length))
                BringOutNextNeedle();
            Fsm.SetState(Attacking);
        }

        // Called by a needle that is ready to attack: true (and one attack used up) if she is awake, has attacks
        // left before her nap, and no other needle started one in the last Attack Gap seconds. Enough attacks bring
        // out the next needle (Attacks Before Next Needle).
        public bool TryStartAttack()
        {
            if (!IsAwake || AttacksLeft <= 0 || Time.time < nextAttackTime)
                return false;
            AttacksLeft--;
            nextAttackTime = Time.time + stats.AttackGap * WaitScale;

            attacksSinceNeedle++;
            int step = needlesOut - stats.StartingNeedles;
            int[] steps = stats.AttacksBeforeNextNeedle;
            if (needlesOut < needles.Length && steps != null && step >= 0 && step < steps.Length
                && attacksSinceNeedle >= steps[step])
                BringOutNextNeedle();
            return true;
        }

        private void BringOutNextNeedle()
        {
            Needle needle = needles[needlesOut];
            needlesOut++;
            attacksSinceNeedle = 0;
            if (needle != null)
                needle.Rise();
            CheckPhase2();
        }

        public bool AnyNeedleAttacking()
        {
            foreach (Needle needle in allNeedles)
                if (needle.IsAttacking)
                    return true;
            return false;
        }

        // The second-half needles rise the first time she is awake in the second half, but never before all her
        // starting needles are out, so the needles always come 1, 2, 3... even if she loses health quickly.
        public void CheckPhase2()
        {
            if (phase2Started || !IsAwake || !InPhase2 || !AllStartingNeedlesOut())
                return;
            phase2Started = true;
            EventBus.Publish(new BossPhaseChanged { RoomId = RoomId, Phase = 2 });
            if (phase2Needles == null)
                return;
            foreach (Needle needle in phase2Needles)
                if (needle != null)
                    needle.Rise();
        }

        private bool AllStartingNeedlesOut()
        {
            foreach (Needle needle in needles)
                if (needle != null && !needle.IsOut)
                    return false;
            return true;
        }

        // The end of the arena (Edge Margin inside it) farthest from the player.
        private float FarEndFromPlayer()
        {
            Bounds bounds = arena.bounds;
            float left = bounds.min.x + stats.EdgeMargin;
            float right = bounds.max.x - stats.EdgeMargin;
            return Mathf.Abs(PlayerPoint.x - left) > Mathf.Abs(PlayerPoint.x - right) ? left : right;
        }

        private bool PlayerWithin(float distance) =>
            HasPlayer && Mathf.Abs(PlayerPoint.x - Body.position.x) < distance;

        // Walks to the end of the arena farthest from the player once they come within Keep Away Distance, and
        // keeps going until she gets there (or meets a wall).
        public void WalkAwayFromPlayer()
        {
            Vector2 position = Body.position;
            if (!walkTarget.HasValue && arena != null && PlayerWithin(stats.KeepAwayDistance))
            {
                float target = FarEndFromPlayer();
                if (Mathf.Abs(target - position.x) > 0.1f)
                    walkTarget = target;
            }
            if (!walkTarget.HasValue)
                return;

            float offset = walkTarget.Value - position.x;
            int direction = offset > 0f ? 1 : -1;
            float step = Mathf.Min(Mathf.Abs(offset), stats.WalkSpeed * Time.fixedDeltaTime);
            Face(direction);

            float halfWidth = body != null ? body.bounds.extents.x : 0.5f;
            bool wall = Physics2D.Raycast(position, Vector2.right * direction, halfWidth + step, groundLayers).collider != null;
            if (wall || Mathf.Abs(offset) <= step)
            {
                if (!wall)
                    Body.MovePosition(new Vector2(walkTarget.Value, position.y));
                walkTarget = null;
                return;
            }
            Body.MovePosition(position + Vector2.right * (direction * step));
        }

        public void StopWalking() => walkTarget = null;

        // ---------- Escape jump ----------

        private enum JumpPhase { None, Crouch, Air }

        public bool IsJumping => jumpPhase != JumpPhase.None;

        // The player got within Jump Trigger Distance and the last jump is long enough ago.
        public bool ShouldJump => !IsJumping && arena != null && Time.time >= nextJumpTime
                                  && PlayerWithin(stats.JumpTriggerDistance);

        // Crouches and shakes for Jump Warning Time, then jumps to the far end of the arena (over the player if they
        // are in the way).
        public void StartJump()
        {
            StopWalking();
            jumpTargetX = FarEndFromPlayer();
            jumpPhase = JumpPhase.Crouch;
            jumpTimer = 0f;
            Face(jumpTargetX >= Body.position.x ? 1 : -1);
            if (Flash != null)
                Flash.SetWarning(true);
            SetSquash(0.8f);
        }

        public void UpdateJump()
        {
            jumpTimer += Time.fixedDeltaTime;
            if (jumpPhase == JumpPhase.Crouch)
            {
                if (jumpTimer < stats.JumpWarningTime)
                    return;
                if (Flash != null)
                    Flash.SetWarning(false);
                SetSquash(1f);
                jumpStart = Body.position;
                jumpTimer = 0f;
                jumpPhase = JumpPhase.Air;
                return;
            }

            float k = Mathf.Clamp01(jumpTimer / stats.JumpTime);
            float x = Mathf.Lerp(jumpStart.x, jumpTargetX, k);
            float y = jumpStart.y + 4f * stats.JumpHeight * k * (1f - k);
            Body.MovePosition(new Vector2(x, y));
            if (k >= 1f)
            {
                jumpPhase = JumpPhase.None;
                nextJumpTime = Time.time + stats.JumpCooldown;
            }
        }

        // Stops a jump at once (she falls asleep, dies or the fight resets); a jump in the air lands where it is.
        public void CancelJump()
        {
            if (jumpPhase == JumpPhase.Air)
            {
                var landed = new Vector2(Body.position.x, jumpStart.y);
                Body.position = landed;
                transform.position = landed;
            }
            if (jumpPhase != JumpPhase.None && Flash != null)
                Flash.SetWarning(false);
            SetSquash(1f);
            jumpPhase = JumpPhase.None;
        }

        private void SetSquash(float height)
        {
            if (visual == null)
                return;
            Vector3 scale = visual.localScale;
            scale.y = visualScale.y * height;
            visual.localScale = scale;
        }

        private void Face(int direction)
        {
            Facing = direction;
            if (visual == null)
                return;
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            visual.localScale = scale;
        }

        // ---------- Hit while awake ----------

        // The nearest waiting needle attacks straight away (it still shakes first), outside the attack count.
        private void Counter()
        {
            if (!IsAwake || Time.time < nextCounterTime || !HasPlayer)
                return;
            Needle nearest = null;
            float best = float.PositiveInfinity;
            foreach (Needle needle in allNeedles)
            {
                if (!needle.IsWaiting)
                    continue;
                float distance = (needle.Body.position - PlayerPoint).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = needle;
                }
            }
            if (nearest == null)
                return;
            nearest.Attack();
            nextCounterTime = Time.time + stats.CounterCooldown;
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + stats.AttackGap * WaitScale);
        }

        public void ShowSleep(bool on)
        {
            if (sleepVisual != null)
                sleepVisual.SetActive(on);
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
            Counter();
        }

        private void OnDeath() => Fsm.SetState(Dead);

        // A lost attempt: back to the start, full health, needles put away. A defeated Grandma stays defeated.
        private void OnPlayerRespawned(PlayerRespawned evt)
        {
            if (Fsm.Current == Dead)
                return;
            Health.ResetHealth();
            foreach (Needle needle in allNeedles)
                needle.PutAway();
            phase2Started = false;
            needlesOut = 0;
            attacksSinceNeedle = 0;
            nextAttackTime = 0f;
            nextJumpTime = 0f;
            nextCounterTime = 0f;
            CancelJump();
            StopWalking();
            Body.position = home;
            transform.position = home;
            Fsm.SetState(Waiting, restart: true);
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
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, currentState);
        }
#endif
    }
}
