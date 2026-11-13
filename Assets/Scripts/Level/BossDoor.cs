using System.Collections;
using UnityEngine;

// A door tied to a boss, horizontal or vertical (its size and Open Offset decide). Placed where it stands closed; when
// open it is pulled away by Open Offset (into the wall or floor, where the level tiles hide it). Tweened in code.
// - Arena: open; slams shut when the fight in its room starts, slides open when the boss is beaten or the player
//   loses (respawns at a bench).
// - Opens When Beaten (map progress): closed until that boss is beaten, then opens for good (also from the save).
[RequireComponent(typeof(Collider2D))]
public class BossDoor : MonoBehaviour
{
    public enum Mode
    {
        Arena,
        OpensWhenBeaten,
    }

    [SerializeField] private Mode mode = Mode.Arena;
    [Tooltip("RoomId of the boss's arena (the LevelRoom's Room Id), e.g. A3.")]
    [SerializeField] private string roomId;
    [Tooltip("Where the door sits when open, relative to where it is placed (closed): e.g. up into the wall for a " +
             "vertical door, sideways into the floor for a horizontal one.")]
    [SerializeField] private Vector2 openOffset = new(0f, 3f);

    [Header("Feel")]
    [SerializeField, Min(0.01f)] private float closeTime = 0.18f;
    [SerializeField, Min(0.01f)] private float openTime = 0.6f;
    [Tooltip("How far it shakes when it slams shut, in units.")]
    [SerializeField, Min(0f)] private float slamShake = 0.08f;
    [SerializeField, Min(0f)] private float slamShakeTime = 0.2f;
    [SerializeField] private SoundData closeSound;
    [SerializeField] private SoundData openSound;

    private Vector3 closedPosition;
    private Collider2D solid;
    private Coroutine moving;
    private bool closed;

    private void Awake()
    {
        closedPosition = transform.localPosition;
        solid = GetComponent<Collider2D>();
        if (mode == Mode.Arena)
            SetOpen();
        else
            SetClosed();
    }

    private void Start()
    {
        // A boss beaten in the save: arena doors stay open, progress doors are already open.
        if (SaveGame.IsBossBeaten(roomId))
            SetOpen();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<BossFightStarted>(OnFightStarted);
        EventBus.Subscribe<BossDefeated>(OnBossDefeated);
        EventBus.Subscribe<PlayerRespawned>(OnPlayerRespawned);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BossFightStarted>(OnFightStarted);
        EventBus.Unsubscribe<BossDefeated>(OnBossDefeated);
        EventBus.Unsubscribe<PlayerRespawned>(OnPlayerRespawned);
    }

    private void OnFightStarted(BossFightStarted evt)
    {
        if (mode == Mode.Arena && evt.RoomId == roomId)
            Close();
    }

    private void OnBossDefeated(BossDefeated evt)
    {
        if (evt.RoomId == roomId)
            Open();
    }

    // A lost fight: let the player back in. Progress doors stay as they are.
    private void OnPlayerRespawned(PlayerRespawned evt)
    {
        if (mode == Mode.Arena)
            Open();
    }

    public void Close()
    {
        if (closed)
            return;
        closed = true;
        solid.enabled = true;
        Play(closeSound);
        Move(closedPosition, closeTime, slam: true);
    }

    public void Open()
    {
        if (!closed)
            return;
        closed = false;
        Play(openSound);
        Move(closedPosition + (Vector3)openOffset, openTime, slam: false);
    }

    private void SetClosed()
    {
        closed = true;
        transform.localPosition = closedPosition;
        solid.enabled = true;
    }

    private void SetOpen()
    {
        closed = false;
        transform.localPosition = closedPosition + (Vector3)openOffset;
        solid.enabled = false;
    }

    private void Move(Vector3 target, float time, bool slam)
    {
        if (moving != null)
            StopCoroutine(moving);
        moving = StartCoroutine(Slide(target, time, slam));
    }

    // Shutting accelerates into the slam (ease in); opening eases out.
    private IEnumerator Slide(Vector3 target, float time, bool slam)
    {
        Vector3 from = transform.localPosition;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            float eased = slam ? k * k : 1f - (1f - k) * (1f - k);
            transform.localPosition = Vector3.LerpUnclamped(from, target, eased);
            yield return null;
        }
        transform.localPosition = target;

        if (slam)
        {
            for (float t = 0f; t < slamShakeTime; t += Time.deltaTime)
            {
                transform.localPosition = target + (Vector3)(Random.insideUnitCircle * slamShake * (1f - t / slamShakeTime));
                yield return null;
            }
            transform.localPosition = target;
        }
        else
        {
            solid.enabled = false;
        }
        moving = null;
    }

    private void Play(SoundData sound)
    {
        if (sound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(sound, transform.position);
    }
}
