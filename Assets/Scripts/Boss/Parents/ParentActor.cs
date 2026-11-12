using Enemies;
using UnityEngine;

namespace Bosses
{
    // What the Mother and the Father share. Each is a body of its own under the boss (ParentsBoss, the director that
    // tells them what to do) with its own small state machine. During the fight they are away (hidden, untouchable)
    // between their moves: for every move one appears where the move starts, warns, strikes, stays open for a
    // moment and leaves again.
    //
    // The Health on this object is a stand-in: the two share one health, on the boss. A player hit lands on this
    // Health, is passed on to the boss (which remembers who was hit last) and this Health is filled up again, so
    // give it more health than any single hit takes. Only the parent that was hit flashes.
    //
    // Physics: the body is made kinematic and its collider a trigger, so it goes exactly where its states move it:
    // through the player, the other parent, walls and platforms. A Hurtbox child (trigger + DamageOnContact) gives
    // the contact damage.
    [RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(EnemyMotor))]
    public abstract class ParentActor : MonoBehaviour
    {
        [Tooltip("Squashed while crouching and laid down in the ending. Usually the Visual child.")]
        [SerializeField] private Transform visual;
        [Tooltip("Read only: the state it is in right now.")]
        [SerializeField] private string currentState;

        private Health proxy;
        private AfterimageTrail trail;
        private DamageOnContact[] hurters;
        private Collider2D[] colliders;
        private Collider2D bodyCollider;
        private Vector2 home;
        private Vector3 visualScale;
        private Vector3 visualPosition;
        private Quaternion visualRotation;
        private IState shownState;
        private bool ready;
        // When the fade-in of an appearance began; negative = not fading in.
        private float fadeInFrom = -1f;
        // The dive line: a stretched copy of its own sprite, outside its object so it stays where it was drawn.
        private SpriteRenderer diveLine;
        private Color lineColor = Color.white;

        public ParentsBoss Boss { get; private set; }
        public ParentsStats Stats => Boss.Stats;
        public EnemyMotor Motor { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public EnemyFlash Flash { get; private set; }
        public SpriteJuice Juice { get; private set; }
        public StateMachine Fsm { get; private set; }

        // From the moment a move is ordered until its last strike: the director starts nothing new meanwhile.
        public bool IsAttacking { get; set; }
        // The opening after a move and leaving: this parent gets no new move until it is away again.
        public bool IsRecovering { get; set; }
        public bool IsFree => !IsAttacking && !IsRecovering;

        // Visible and touchable (false while away).
        public bool IsPresent { get; private set; } = true;

        // The ending: lying on the ground / standing next to the fallen one.
        public bool HasFallen { get; set; }
        public bool HasArrived { get; set; }

        public ParentHoldState Holding { get; private set; }
        public ParentAwayState Away { get; private set; }
        public ParentVanishState Vanishing { get; private set; }
        public ParentDashState Dashing { get; private set; }
        public ParentCrossDiveState CrossDiving { get; private set; }
        public ParentFallState Falling { get; private set; }
        public ParentWalkToState WalkingTo { get; private set; }

        public int PlayerSide => Boss.PlayerPoint.x >= Body.position.x ? 1 : -1;
        public float HalfHeight { get; private set; }
        public float HalfWidth { get; private set; }

        // The height of its middle when it stands on the arena's floor.
        public float StandY => Boss.FloorY + HalfHeight;

        private void Awake() => Setup();

        // Called once by the boss in its Awake (which may run before this object's own).
        public void Init(ParentsBoss boss)
        {
            Setup();
            Boss = boss;
            Holding = new ParentHoldState(this);
            Away = new ParentAwayState(this);
            Vanishing = new ParentVanishState(this);
            Dashing = new ParentDashState(this);
            CrossDiving = new ParentCrossDiveState(this);
            Falling = new ParentFallState(this);
            WalkingTo = new ParentWalkToState(this);
            BuildStates();
            BuildDiveLine();
        }

        private void BuildDiveLine()
        {
            SpriteRenderer source = visual != null
                ? visual.GetComponentInChildren<SpriteRenderer>(true)
                : GetComponentInChildren<SpriteRenderer>(true);
            if (source == null)
                return;
            var line = new GameObject(name + " Dive Line");
            line.transform.SetParent(Boss.transform, false);
            diveLine = line.AddComponent<SpriteRenderer>();
            diveLine.sprite = source.sprite;
            diveLine.sharedMaterial = source.sharedMaterial;
            diveLine.sortingLayerID = source.sortingLayerID;
            diveLine.sortingOrder = source.sortingOrder - 2;
            lineColor = source.color;
            line.SetActive(false);
        }

        protected abstract void BuildStates();

        // Its voice as it appears for a move (none by default).
        protected virtual SoundData Voice => null;

        private void Setup()
        {
            if (ready)
                return;
            ready = true;
            proxy = GetComponent<Health>();
            Motor = GetComponent<EnemyMotor>();
            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Kinematic;
            Body.freezeRotation = true;
            bodyCollider = GetComponent<Collider2D>();
            // From the box itself: a collider's bounds are not ready this early.
            Vector2 size = bodyCollider is BoxCollider2D box
                ? Vector2.Scale(box.size, transform.lossyScale)
                : (Vector2)bodyCollider.bounds.size;
            HalfHeight = Mathf.Abs(size.y) * 0.5f;
            HalfWidth = Mathf.Abs(size.x) * 0.5f;
            bodyCollider.isTrigger = true;
            Flash = GetComponent<EnemyFlash>();
            Juice = GetComponent<SpriteJuice>();
            trail = GetComponent<AfterimageTrail>();
            hurters = GetComponentsInChildren<DamageOnContact>(true);
            colliders = GetComponentsInChildren<Collider2D>(true);
            Fsm = new StateMachine();
            home = transform.position;
            if (visual != null)
            {
                visualScale = visual.localScale;
                visualPosition = visual.localPosition;
                visualRotation = visual.localRotation;
            }
        }

        private void OnEnable() => proxy.Damaged += OnDamaged;

        private void OnDisable()
        {
            proxy.Damaged -= OnDamaged;
            HideDiveLine();
        }

        private void Update()
        {
            Fsm.Tick();
            if (Fsm.Current != shownState)
            {
                shownState = Fsm.Current;
                currentState = shownState?.GetType().Name;
            }

            if (fadeInFrom >= 0f)
            {
                float k = Stats.AppearFadeTime > 0f ? Mathf.Clamp01((Time.time - fadeInFrom) / Stats.AppearFadeTime) : 1f;
                SetFade(k);
                if (k >= 1f)
                    fadeInFrom = -1f;
            }
        }

        private void FixedUpdate() => Fsm.FixedTick();

        private void OnDamaged(int amount)
        {
            proxy.ResetHealth();
            if (Boss == null || Boss.IsDefeated)
                return;
            if (Flash != null)
                Flash.Hit();
            Boss.TakeHit(this, amount);
        }

        // ---------- Orders from the director ----------

        // Stands still where it is: before the fight and after a lost attempt.
        public void Hold() => Fsm.SetState(Holding, restart: true);

        // Leaves (fades away); it is free for a move once it is away.
        public void Leave()
        {
            fadeInFrom = -1f;
            Fsm.SetState(Vanishing, restart: true);
        }

        // Appears at the side of the room (side: -1 left, 1 right) after delay seconds and dashes across the floor
        // to the other side, count times (there and back).
        public void Dash(int side, int count, float delay = 0f)
        {
            Dashing.Side = side >= 0 ? 1 : -1;
            Dashing.Count = Mathf.Max(1, count);
            Dashing.Delay = Mathf.Max(0f, delay);
            Fsm.SetState(Dashing, restart: true);
        }

        // One half of the cross dive: appears up and to side (-1 left, 1 right) of the player after delay seconds,
        // hangs aiming at them, then dives diagonally to the floor through where it aimed.
        public void CrossDive(int side, float delay = 0f)
        {
            CrossDiving.Side = side >= 0 ? 1 : -1;
            CrossDiving.Delay = Mathf.Max(0f, delay);
            Fsm.SetState(CrossDiving, restart: true);
        }

        // The ending, for the one that took the last hit: drops to the floor and lies there (HasFallen).
        public void FallDown() => Fsm.SetState(Falling, restart: true);

        // The ending, for the other one: comes to the floor, walks to target and stops next to it (HasArrived).
        public void WalkTo(ParentActor target)
        {
            WalkingTo.Target = target;
            Fsm.SetState(WalkingTo, restart: true);
        }

        // Back where it was placed, as it was placed.
        public void ResetForRetry()
        {
            Hold();
            HasFallen = false;
            HasArrived = false;
            Body.bodyType = RigidbodyType2D.Kinematic;
            Appear(home, Motor.Facing, punch: false);
            if (visual != null)
            {
                visual.localPosition = visualPosition;
                visual.localRotation = visualRotation;
            }
        }

        // ---------- Helpers for the states ----------

        // Shows up at point, facing facing (-1 or 1): visible, touchable and hurting again.
        public void Appear(Vector2 point, int facing, bool punch = true)
        {
            Body.linearVelocity = Vector2.zero;
            Body.position = point;
            transform.position = point;
            Motor.Face(facing);
            SetPresent(true);
            if (!punch)
                return;
            Punch(Stats.AppearPunch);
            PlaySound(Stats.AppearSound);
            PlaySound(Voice);
            fadeInFrom = Time.time;
            SetFade(0f);
        }

        // Hidden and untouchable (away), or back.
        public void SetPresent(bool on)
        {
            IsPresent = on;
            fadeInFrom = -1f;
            foreach (Collider2D own in colliders)
                if (own != null)
                    own.enabled = on;
            SetHurting(on);
            SetFade(on ? 1f : 0f);
        }

        public void SetVelocity(Vector2 velocity) => Body.linearVelocity = velocity;

        public void PlaySound(SoundData sound)
        {
            if (sound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(sound, transform.position);
        }

        // Draws the dive line from from along direction for length units, alpha (0-1) of Dive Line Alpha visible.
        public void ShowDiveLine(Vector2 from, Vector2 direction, float length, float alpha)
        {
            if (diveLine == null || !Stats.DiveLines || length <= 0f || direction.sqrMagnitude < 0.0001f)
            {
                HideDiveLine();
                return;
            }
            direction.Normalize();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Transform line = diveLine.transform;
            line.SetPositionAndRotation(from + direction * (length * 0.5f), Quaternion.Euler(0f, 0f, angle));
            line.localScale = new Vector3(length, Stats.DiveLineWidth, 1f);
            Color color = lineColor;
            color.a *= Mathf.Clamp01(alpha) * Stats.DiveLineAlpha;
            diveLine.color = color;
            if (!diveLine.gameObject.activeSelf)
                diveLine.gameObject.SetActive(true);
        }

        public void HideDiveLine()
        {
            if (diveLine != null && diveLine.gameObject.activeSelf)
                diveLine.gameObject.SetActive(false);
        }

        public void FacePlayer() => Motor.Face(PlayerSide);

        public void SetWarning(bool on)
        {
            if (Flash != null)
                Flash.SetWarning(on);
        }

        public void SetCrouch(bool on)
        {
            if (visual == null)
                return;
            Vector3 scale = visual.localScale;
            scale.y = visualScale.y * (on ? Stats.CrouchScale : 1f);
            visual.localScale = scale;
        }

        public void Punch(Vector2 scale)
        {
            if (Juice != null)
                Juice.Punch(scale);
        }

        // Afterimages behind it, for fast moves only.
        public void SetTrail(bool on)
        {
            if (trail != null)
                trail.Emitting = on;
        }

        // Contact damage on or off (the body can still be hit).
        public void SetHurting(bool on)
        {
            foreach (DamageOnContact hurter in hurters)
                if (hurter != null)
                    hurter.enabled = on;
        }

        public void SetFade(float fade)
        {
            if (Flash != null)
                Flash.Fade = fade;
        }

        // Lies on its side on the ground (the ending).
        public void LieDown()
        {
            if (visual == null)
                return;
            visual.localRotation = visualRotation * Quaternion.Euler(0f, 0f, 90f);
            visual.localPosition = visualPosition + Vector3.down * Mathf.Max(0f, HalfHeight - HalfWidth);
        }

        // Puts every move's effects back to rest. Runs after every state's exit (ParentState), so nothing a state
        // switched on outlives it.
        public void StopEffects()
        {
            IsAttacking = false;
            IsRecovering = false;
            SetWarning(false);
            SetCrouch(false);
            SetTrail(false);
            HideDiveLine();
            Body.linearVelocity = Vector2.zero;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && IsPresent && !string.IsNullOrEmpty(currentState))
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, currentState);
        }
#endif
    }
}
