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
    // The fight start (Start Signal, the end of her intro conversation), the arena, a lost attempt, her death and
    // her reward work as for every boss (BossFight); BossPhaseChanged goes out when the second half begins.
    //
    // Keep the needles and the reward outside her object (siblings under the prefab root): a player hit finds the
    // Health above the collider it touched, so a needle under her would pass its hits on to her.
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public class GrandmaBoss : BossFight
    {
        [SerializeField] private GrandmaStats stats;
        [Tooltip("Her needles, in the order they join (see Starting Needles). Each attacks on its own; where it sits " +
                 "in the prefab (relative to her) is its waiting spot.")]
        [SerializeField] private Needle[] needles;
        [Tooltip("Needles that join in the second half (see Phase 2 Health), the first time she is awake there.")]
        [SerializeField] private Needle[] phase2Needles;
        [Tooltip("Walls that stop her walk.")]
        [SerializeField] private LayerMask groundLayers;
        [Tooltip("Flipped to face where she walks, squashed while she crouches to jump. Usually the Visual child.")]
        [SerializeField] private Transform visual;
        [Tooltip("Switched on while she sleeps (a Zzz...). Optional.")]
        [SerializeField] private GameObject sleepVisual;

        private readonly List<Needle> allNeedles = new List<Needle>();
        private Collider2D body;
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
        private bool phase2Started;

        public GrandmaStats Stats => stats;
        public Rigidbody2D Body { get; private set; }
        public IReadOnlyList<Needle> AllNeedles => allNeedles;

        public GrandmaWaitState Waiting { get; private set; }
        public GrandmaAttackState Attacking { get; private set; }
        public GrandmaSleepState Sleeping { get; private set; }
        public GrandmaDeadState Dead { get; private set; }

        protected override IState WaitingState => Waiting;
        protected override IState DefeatedState => Dead;

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

        protected override void Awake()
        {
            base.Awake();
            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Kinematic;
            body = GetComponent<Collider2D>();
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

        // The fight starts: the first needles rise and she wakes up.
        protected override void OnFightStarted()
        {
            needlesOut = 0;
            attacksSinceNeedle = 0;
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
            PublishPhase(2);
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
            Bounds bounds = Arena.bounds;
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
            if (!walkTarget.HasValue && Arena != null && PlayerWithin(stats.KeepAwayDistance))
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
        public bool ShouldJump => !IsJumping && Arena != null && Time.time >= nextJumpTime
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

        // The nearest free needle (waiting, or flying back) attacks straight away (it still shakes first), outside
        // the attack count. With no Counter Cooldown every hit sends one, so a string of hits brings several at once.
        private void Counter()
        {
            if (!IsAwake || Time.time < nextCounterTime || !HasPlayer)
                return;
            Needle nearest = null;
            float best = float.PositiveInfinity;
            foreach (Needle needle in allNeedles)
            {
                if (!needle.IsFree)
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

        protected override void OnHit(int amount)
        {
            CheckPhase2();
            Counter();
        }

        // A lost attempt: back to the start with the needles put away.
        protected override void ResetForRetry()
        {
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
            Body.position = Home;
            transform.position = Home;
        }
    }
}
