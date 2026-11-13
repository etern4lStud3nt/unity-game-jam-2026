using Enemies;
using UnityEngine;

// An enemy's (or a boss's) sounds, all optional: hurt and death from its Health; and, when it has an EnemyBrain,
// alert (it notices the player and leaves its idle state), windup (it gets ready to strike) and impact (a flyer's
// dive ends). Idle loops on its own positional source while it is alive (a flyer's buzz).
public class EnemySounds : MonoBehaviour
{
    [SerializeField] private SoundData hurt;
    [SerializeField] private SoundData death;
    [SerializeField] private SoundData alert;
    [SerializeField] private SoundData windup;
    [SerializeField] private SoundData impact;
    [Tooltip("Loops while alive, heard from nearby (its first clip).")]
    [SerializeField] private SoundData idle;
    [Tooltip("Distance where the idle loop starts to fade, and where it is gone.")]
    [SerializeField] private Vector2 idleRange = new Vector2(3f, 12f);

    private Health health;
    private EnemyBrain brain;
    private AudioSource idleSource;

    private void Awake()
    {
        health = GetComponentInParent<Health>();
        brain = GetComponentInParent<EnemyBrain>();
        if (idle != null && idle.TryGetClip(out AudioClip clip))
        {
            idleSource = gameObject.AddComponent<AudioSource>();
            idleSource.clip = clip;
            idleSource.loop = true;
            idleSource.volume = idle.Volume;
            idleSource.outputAudioMixerGroup = idle.MixerGroup;
            idleSource.spatialBlend = 1f;
            idleSource.rolloffMode = AudioRolloffMode.Linear;
            idleSource.minDistance = idleRange.x;
            idleSource.maxDistance = idleRange.y;
            idleSource.dopplerLevel = 0f;
            idleSource.playOnAwake = false;
        }
    }

    // The brain builds its state machine in Awake: hook it here.
    private void Start()
    {
        if (brain != null && brain.Fsm != null)
            brain.Fsm.StateChanged += OnStateChanged;
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Death += OnDeath;
        }
        if (idleSource != null && (health == null || !health.IsDead))
            idleSource.Play();
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Death -= OnDeath;
        }
    }

    private void OnDestroy()
    {
        if (brain != null && brain.Fsm != null)
            brain.Fsm.StateChanged -= OnStateChanged;
    }

    private void OnDamaged(int amount)
    {
        if (!health.IsDead)
            Play(hurt);
    }

    private void OnDeath()
    {
        Play(death);
        if (idleSource != null)
            idleSource.Stop();
    }

    private void OnStateChanged(IState from, IState to)
    {
        if (to == brain.Hurt || to == brain.Dead)
            return;
        if (from == brain.Idle)
            Play(alert);
        if (to is WindupState || to is AimState)
            Play(windup);
        if (from is DiveState)
            Play(impact);
    }

    private void Play(SoundData sound)
    {
        if (sound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(sound, transform.position);
    }
}
