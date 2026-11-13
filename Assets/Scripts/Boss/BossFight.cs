using Enemies;
using UnityEngine;

namespace Bosses
{
    // What every boss fight shares, so each boss only adds its own moves:
    // - the arena (a LevelRoom trigger; found around the boss if not assigned) and its RoomId;
    // - the player (by tag): where they are, how fast they move, whether they are in the arena;
    // - the start: the Yarn signal Start Signal (the end of the intro conversation) or StartFight(); without a
    //   signal, or once the intro has run, the player being in the arena starts it (the waiting state calls
    //   StartOnSight). BossFightStarted goes out; phases publish BossPhaseChanged (PublishPhase);
    // - hits (a flash, then OnHit), death (the defeated state) and a lost attempt (the player respawns: full
    //   health, ResetForRetry, back to waiting). A defeated boss stays defeated;
    // - the end: BossDefeated (PublishDefeated), a fade out and the reward switched on (FadeOut).
    // Keep anything the player can hit but should not hurt the boss (needles, thrown objects, traps, the reward)
    // outside the boss's object: a player hit finds the Health above the collider it touched.
    [RequireComponent(typeof(Health))]
    public abstract class BossFight : MonoBehaviour
    {
        [Tooltip("The arena. Empty = the LevelRoom trigger the boss was placed in.")]
        [SerializeField] private Collider2D arena;
        [Tooltip("Name of the Yarn <<signal ...>> that starts the fight. Empty = it starts when the player is in the arena.")]
        [SerializeField] private string startSignal;
        [Tooltip("Switched on after it dies: its lucky number pickup, placed where it should appear. Optional.")]
        [SerializeField] private GameObject reward;
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Read only: the state it is in right now.")]
        [SerializeField] private string currentState;

        private Collider2D[] ownColliders;
        private Collider2D playerCollider;
        private Rigidbody2D playerBody;
        private IState shownState;
        private bool introDone;
        private bool rewarded;

        public Health Health { get; private set; }
        public EnemyFlash Flash { get; private set; }
        public StateMachine Fsm { get; private set; }
        public Collider2D Arena => arena;
        public string RoomId { get; private set; }

        // Where it was placed; a lost attempt puts it back here.
        protected Vector2 Home { get; private set; }

        // The player's object, once found in Start (null without one).
        protected GameObject Player { get; private set; }

        // The state it waits in before the fight (and after a lost attempt), and the one it ends in.
        protected abstract IState WaitingState { get; }
        protected abstract IState DefeatedState { get; }

        public bool HasPlayer => Player != null;
        public Vector2 PlayerPoint => playerCollider != null ? (Vector2)playerCollider.bounds.center
                                    : HasPlayer ? (Vector2)Player.transform.position : (Vector2)transform.position;
        public Vector2 PlayerVelocity => playerBody != null ? playerBody.linearVelocity : Vector2.zero;
        public bool PlayerInArena => HasPlayer && arena != null && arena.OverlapPoint(PlayerPoint);

        // The fight starts by itself once the player is in the arena, unless the intro has not run yet.
        public bool ShouldStartOnSight => introDone || string.IsNullOrEmpty(startSignal);

        // Derived bosses call this first, then build their states (and switch themselves off if they can't).
        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Flash = GetComponent<EnemyFlash>();
            ownColliders = GetComponentsInChildren<Collider2D>(true);
            Home = transform.position;
            Fsm = new StateMachine();
        }

        protected virtual void OnEnable()
        {
            if (WaitingState == null)
                return;
            Health.Damaged += OnDamaged;
            Health.Death += OnDeath;
            EventBus.Subscribe<DialogueSignal>(OnDialogueSignal);
            EventBus.Subscribe<PlayerRespawned>(OnPlayerRespawned);
        }

        protected virtual void OnDisable()
        {
            if (WaitingState == null)
                return;
            Health.Damaged -= OnDamaged;
            Health.Death -= OnDeath;
            EventBus.Unsubscribe<DialogueSignal>(OnDialogueSignal);
            EventBus.Unsubscribe<PlayerRespawned>(OnPlayerRespawned);
        }

        // In Start, once every collider of the scene is enabled, so the room around the boss can be found.
        protected virtual void Start()
        {
            if (arena == null && LevelRoom.FindAt(transform.position) is LevelRoom around)
                arena = around.GetComponent<Collider2D>();
            if (arena == null)
                Debug.LogWarning($"{name} found no LevelRoom trigger around it; assign the Arena.", this);
            else if (arena.TryGetComponent(out LevelRoom room))
                RoomId = room.RoomId;

            Player = GameObject.FindWithTag(playerTag);
            if (Player != null)
            {
                playerBody = Player.GetComponent<Rigidbody2D>();
                playerCollider = Player.GetComponent<Collider2D>();
            }
            if (reward != null)
                reward.SetActive(false);
            Fsm.SetState(WaitingState);
        }

        protected virtual void Update()
        {
            Fsm.Tick();
            if (Fsm.Current != shownState)
            {
                shownState = Fsm.Current;
                currentState = shownState?.GetType().Name;
            }
        }

        protected virtual void FixedUpdate() => Fsm.FixedTick();

        // Starts the fight (BossFightStarted goes out, then OnFightStarted). Does nothing unless it is waiting.
        public void StartFight()
        {
            if (WaitingState == null || Fsm.Current != WaitingState)
                return;
            introDone = true;
            EventBus.Publish(new BossFightStarted { RoomId = RoomId });
            OnFightStarted();
        }

        // Called by the waiting state every frame.
        public void StartOnSight()
        {
            if (ShouldStartOnSight && PlayerInArena)
                StartFight();
        }

        // The fight just started: move on from the waiting state.
        protected abstract void OnFightStarted();

        // A hit that did not kill it (it has already flashed).
        protected virtual void OnHit(int amount) { }

        // A lost attempt: put everything back as it was at the start (health is already full). The boss then goes
        // back to its waiting state.
        protected abstract void ResetForRetry();

        protected void PublishPhase(int phase) => EventBus.Publish(new BossPhaseChanged { RoomId = RoomId, Phase = phase });

        // Switches off the intro conversations standing in the arena, so a beaten boss doesn't talk again.
        public void DisableIntros()
        {
            if (arena == null)
                return;
            foreach (DialogueTrigger trigger in FindObjectsByType<DialogueTrigger>())
                if (arena.OverlapPoint(trigger.transform.position))
                    trigger.gameObject.SetActive(false);
        }

        public void PublishDefeated()
        {
            DisableIntros();
            EventBus.Publish(new BossDefeated { RoomId = RoomId, Position = transform.position });
        }

        // For the defeated state, every frame: fades out over duration, then switches the reward on (once).
        public void FadeOut(float elapsed, float duration)
        {
            float k = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            if (Flash != null)
                Flash.Fade = 1f - k;
            if (k >= 1f && !rewarded)
            {
                rewarded = true;
                if (reward != null)
                    reward.SetActive(true);
            }
        }

        public void ResetFade()
        {
            if (Flash != null)
                Flash.Fade = 1f;
        }

        public void SetCollidersEnabled(bool on)
        {
            foreach (Collider2D own in ownColliders)
                if (own != null)
                    own.enabled = on;
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
            OnHit(amount);
        }

        private void OnDeath() => Fsm.SetState(DefeatedState);

        private void OnPlayerRespawned(PlayerRespawned evt)
        {
            if (Fsm.Current == DefeatedState)
                return;
            Health.ResetHealth();
            ResetForRetry();
            Fsm.SetState(WaitingState, restart: true);
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
