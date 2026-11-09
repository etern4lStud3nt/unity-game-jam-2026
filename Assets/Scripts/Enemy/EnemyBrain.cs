using System;
using UnityEngine;

namespace Enemies
{
    // Runs a ground enemy's state machine: Idle -> Chase -> Windup -> Lunge -> Recover -> Chase ...
    // Transitions between those live inside the states. The only transitions decided here come from Health:
    // getting hit (knockback, flash, optional stun) and dying.
    //
    // This class is the walker. Other enemy types derive from it and override CreateIdleState to swap the calm
    // behaviour (patrol for the walker, standing guard for GuardBrain); the shared states (Chase, Windup, Lunge,
    // Recover, Hurt, Dead) go back to Brain.Idle, so they work for every type unchanged.
    [RequireComponent(typeof(EnemyMotor), typeof(EnemySenses), typeof(Health))]
    public class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyStats stats;
        [Tooltip("Swing attacks only: the weapon's damage area (trigger + DamageOnContact), switched on during a swing. " +
                 "Put it under the Visual child so it flips with the enemy.")]
        [SerializeField] private GameObject attackHitbox;
        [Tooltip("Read only: the state the enemy is in right now.")]
        [SerializeField] private string currentState;
        [Tooltip("Read only: how close (body centre to player centre) the player must be for an attack to start. " +
                 "Worked out from the attack itself: lunge distance, or the Attack Hitbox's far edge.")]
        [SerializeField] private float attackReach;

        public EnemyStats Stats => stats;
        public EnemyMotor Motor { get; private set; }
        public EnemySenses Senses { get; private set; }
        public EnemyFlash Flash { get; private set; }
        public Health Health { get; private set; }
        public StateMachine Fsm { get; private set; }

        // The calm state: where the enemy starts and what it goes back to after losing the player.
        public IState Idle { get; private set; }
        // The pursuit state: ChaseState for ground enemies, overridable (the flyer approaches from above).
        public IState Chase { get; private set; }
        public WindupState Windup { get; private set; }
        public LungeState Lunge { get; private set; }
        public SwingState Swing { get; private set; }
        public RecoverState Recover { get; private set; }
        public HurtState Hurt { get; private set; }
        public DeadState Dead { get; private set; }

        // Earliest time the next attack may start; set when an attack begins.
        public float NextAttackTime { get; set; }

        // Where the enemy was placed and the way it faced; patrols and guards are anchored here.
        public Vector2 Home { get; private set; }
        public int HomeFacing { get; private set; } = 1;

        // True when the player is close enough to home to be worth chasing (always true without a leash).
        public virtual bool PlayerInLeash =>
            stats.LeashDistance <= 0f || (Senses.HasPlayer && Mathf.Abs(Senses.PlayerPoint.x - Home.x) <= stats.LeashDistance);

        // How close the player must be (horizontally, centre to centre) for the attack to land. Not a setting:
        // move or resize the Attack Hitbox, or change the lunge speed/time, and the reach follows.
        public float AttackReach
        {
            get
            {
                if (stats == null)
                    return 0f;
                var body = GetComponent<Collider2D>();
                float bodyCentre = body != null ? body.bounds.center.x : transform.position.x;
                float bodyHalf = body != null ? body.bounds.extents.x : 0.5f;

                switch (stats.AttackType)
                {
                    case EnemyAttackType.WindupLunge:
                        // The body is the weapon: it travels the lunge distance and hits with its front edge.
                        return stats.LungeSpeed * stats.LungeTime + bodyHalf;
                    case EnemyAttackType.WindupSwing:
                        // Ends exactly at the hitbox tip. The swing step is a bonus that makes it a bit harder to back off.
                        return HitboxReach(bodyCentre);
                    default:
                        return 0f;
                }
            }
        }

        private Vector3 startScale;
        private Collider2D[] ownColliders;
        private Collider2D[] playerColliders;

        // Raised when the death animation starts (drops, kill counters, sounds). EnemyKilled goes out on the EventBus too.
        public event Action<EnemyBrain> Died;

        // The walker patrols. Override to give another enemy type a different calm behaviour.
        protected virtual IState CreateIdleState() => new PatrolState(this);

        // Ground enemies run at the player. Override for enemies that pursue differently.
        protected virtual IState CreateChaseState() => new ChaseState(this);

        private void Awake()
        {
            Motor = GetComponent<EnemyMotor>();
            Senses = GetComponent<EnemySenses>();
            Flash = GetComponent<EnemyFlash>();
            Health = GetComponent<Health>();

            if (stats == null)
            {
                Debug.LogWarning($"{name} has no {nameof(EnemyStats)} asset; the enemy stays idle.", this);
                enabled = false;
                return;
            }

            Senses.Configure(stats, Motor);
            Home = transform.position;
            startScale = transform.localScale;
            ownColliders = GetComponents<Collider2D>();
            EnemyReset.Register(this);

            Fsm = new StateMachine();
            Idle = CreateIdleState();
            Chase = CreateChaseState();
            Windup = new WindupState(this);
            Lunge = new LungeState(this);
            Swing = new SwingState(this);
            SetAttackHitbox(false);
            if (stats.AttackType == EnemyAttackType.WindupSwing && attackHitbox == null)
                Debug.LogWarning($"{name} swings but has no Attack Hitbox; its swings will not hurt.", this);
            Recover = new RecoverState(this);
            Hurt = new HurtState(this);
            Dead = new DeadState(this);
        }

        private void OnEnable()
        {
            if (Fsm == null)
                return;
            Health.Damaged += OnDamaged;
            Health.Death += OnDeath;
            Senses.PlayerFound += IgnorePlayerCollisions;
        }

        private void OnDisable()
        {
            if (Fsm == null)
                return;
            Health.Damaged -= OnDamaged;
            Health.Death -= OnDeath;
            Senses.PlayerFound -= IgnorePlayerCollisions;
        }

        private void Start()
        {
            // After every Awake, so the motor has applied its starting direction.
            HomeFacing = Motor.Facing;
            Fsm.SetState(Idle);
        }

        private void Update()
        {
            Senses.Refresh();
            Fsm.Tick();
            currentState = Fsm.Current?.GetType().Name;
            attackReach = AttackReach;
        }

        private void OnValidate()
        {
            attackReach = AttackReach;
        }

        private void FixedUpdate()
        {
            KeepIgnoringPlayer();
            Fsm.FixedTick();
        }

        private void OnDamaged(int amount)
        {
            if (Health.IsDead)
                return;

            if (Flash != null)
                Flash.Hit();

            // Pushed away from the player; a hit from behind also makes it notice them.
            int away = Senses.HasPlayer ? -Senses.PlayerDirection : -Motor.Facing;
            Motor.Knockback(away, stats.KnockbackForce, stats.KnockbackTime);
            Senses.Alert();

            if (stats.StunDuration > 0f)
                Fsm.SetState(Hurt, restart: true);
            else if (Fsm.Current == Idle)
                Fsm.SetState(Chase);
        }

        private void OnDeath()
        {
            Fsm.SetState(Dead);
        }

        internal void RaiseDied()
        {
            Died?.Invoke(this);
            EventBus.Publish(new EnemyKilled { Enemy = gameObject, Position = transform.position });
        }

        // Back to where and how it was placed: alive, full health, calm. Used when the player respawns.
        public void ResetToStart()
        {
            if (Fsm == null)
                return;

            gameObject.SetActive(true);
            transform.localScale = startScale;
            Motor.Body.simulated = true;
            Motor.Body.position = Home;
            transform.position = Home;
            Motor.Body.linearVelocity = Vector2.zero;
            if (Flash != null)
                Flash.Fade = 1f;
            Health.ResetHealth();
            NextAttackTime = 0f;
            SetAttackHitbox(false);
            Motor.Face(HomeFacing);
            Fsm.SetState(Idle, restart: true);
        }

        private void OnDestroy() => EnemyReset.Unregister(this);

        public void SetAttackHitbox(bool on)
        {
            if (attackHitbox != null)
                attackHitbox.SetActive(on);
        }

        // Distance from the body centre to the far edge of the hitbox. Worked out from the transform rather than
        // collider bounds, because the hitbox is switched off between swings and then has no bounds.
        private float HitboxReach(float bodyCentre)
        {
            if (attackHitbox == null || !attackHitbox.TryGetComponent(out BoxCollider2D box))
                return 0f;
            Transform t = attackHitbox.transform;
            float centre = t.TransformPoint(box.offset).x;
            float half = Mathf.Abs(box.size.x * t.lossyScale.x) * 0.5f;
            return Mathf.Abs(centre - bodyCentre) + half;
        }

        // The player walks through enemies (and takes contact damage from the Hurtbox trigger) instead of
        // standing on them or pushing them, without needing a dedicated physics layer.
        private void IgnorePlayerCollisions(Transform player)
        {
            playerColliders = player.GetComponentsInChildren<Collider2D>();
            ApplyIgnore();
        }

        // Unity forgets ignored pairs when the enemy object is switched off and on (death, then reset), so check
        // every physics step and ignore them again if needed. Without this a revived enemy bumps into the player
        // and its Hurtbox can never touch them.
        private void KeepIgnoringPlayer()
        {
            if (playerColliders == null || ownColliders.Length == 0)
                return;
            foreach (Collider2D theirs in playerColliders)
            {
                if (theirs != null && !Physics2D.GetIgnoreCollision(ownColliders[0], theirs))
                {
                    ApplyIgnore();
                    return;
                }
            }
        }

        private void ApplyIgnore()
        {
            foreach (Collider2D theirs in playerColliders)
            {
                if (theirs == null)
                    continue;
                foreach (Collider2D own in ownColliders)
                    Physics2D.IgnoreCollision(own, theirs);
            }
        }

#if UNITY_EDITOR
        // Patrol limits as green posts, leash as taller red posts (around the current position until Play starts),
        // attack reach as a magenta line in front.
        private void OnDrawGizmosSelected()
        {
            if (stats == null)
                return;

            float reach = AttackReach;
            if (reach > 0f)
            {
                var body = GetComponent<Collider2D>();
                Vector3 centre = body != null ? body.bounds.center : transform.position;
                int facing = Motor != null ? Motor.Facing : 1;
                Vector3 tip = centre + Vector3.right * facing * reach;
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(centre, tip);
                Gizmos.DrawLine(tip + Vector3.up * 0.3f, tip + Vector3.down * 0.3f);
            }

            Vector3 home = Application.isPlaying ? (Vector3)Home : transform.position;
            if (Idle is PatrolState || (!Application.isPlaying && GetType() == typeof(EnemyBrain)))
                DrawPosts(home, stats.PatrolDistance, 1f, Color.green);
            DrawPosts(home, stats.LeashDistance, 2f, Color.red);
            DrawExtraGizmos();
        }

        private static void DrawPosts(Vector3 home, float distance, float height, Color color)
        {
            if (distance <= 0f)
                return;
            Gizmos.color = color;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 post = home + Vector3.right * side * distance;
                Gizmos.DrawLine(post + Vector3.down * height, post + Vector3.up * height);
            }
        }

        // Extra gizmos for enemy types, drawn while the enemy is selected.
        protected virtual void DrawExtraGizmos() { }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying && !string.IsNullOrEmpty(currentState))
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, currentState);
        }
#endif
    }
}
