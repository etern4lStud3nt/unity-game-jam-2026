using UnityEngine;

// Plays a boss's music when its fight actually starts (BossFightStarted, e.g. after the intro conversation), not
// when the player walks into the arena, and switches to Phase 2 Music when the boss reaches its second half.
// During the fight it holds the music, so rooms (RoomArea) cannot swap their own music in, also when the player
// dies and comes back inside the arena. Coming back outside the arena hands the music back to the area.
// When the boss is defeated the music fades out, or Defeat Music plays; the next room entered brings its area's
// music back. Put it on the boss. Leave the arena's RoomArea Music Override empty.
public class BossMusic : MonoBehaviour
{
    [Tooltip("The arena's LevelRoom id (e.g. A3). Empty = the LevelRoom the boss is placed in (any boss if none).")]
    [SerializeField] private string roomId;
    [SerializeField] private MusicTrack fightMusic;
    [Tooltip("Optional. Plays from the boss's second half on; empty = the fight music keeps playing.")]
    [SerializeField] private MusicTrack phase2Music;
    [Tooltip("Optional. Plays when the boss is defeated; empty = the fight music fades out.")]
    [SerializeField] private MusicTrack defeatMusic;
    [SerializeField, Min(0f)] private float fadeDuration = 1f;

    private void OnEnable()
    {
        EventBus.Subscribe<BossFightStarted>(OnFightStarted);
        EventBus.Subscribe<BossPhaseChanged>(OnPhaseChanged);
        EventBus.Subscribe<BossDefeated>(OnDefeated);
        EventBus.Subscribe<PlayerRespawned>(OnPlayerRespawned);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BossFightStarted>(OnFightStarted);
        EventBus.Unsubscribe<BossPhaseChanged>(OnPhaseChanged);
        EventBus.Unsubscribe<BossDefeated>(OnDefeated);
        EventBus.Unsubscribe<PlayerRespawned>(OnPlayerRespawned);
        if (AudioManager.Instance != null)
            AudioManager.Instance.ReleaseMusic(this, resumeArea: false);
    }

    // In Start, once every collider of the scene is enabled, so the room around the boss can be found.
    private void Start()
    {
        if (!string.IsNullOrEmpty(roomId))
            return;
        foreach (Collider2D candidate in Physics2D.OverlapPointAll(transform.position))
            if (candidate.isTrigger && candidate.TryGetComponent(out LevelRoom room))
            {
                roomId = room.RoomId;
                return;
            }
    }

    private void OnFightStarted(BossFightStarted evt)
    {
        if (!Matches(evt.RoomId) || AudioManager.Instance == null)
            return;
        AudioManager.Instance.HoldMusic(this);
        AudioManager.Instance.PlayMusic(fightMusic, fadeDuration);
    }

    private void OnPhaseChanged(BossPhaseChanged evt)
    {
        if (Matches(evt.RoomId) && evt.Phase == 2 && phase2Music != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(phase2Music, fadeDuration);
    }

    private void OnDefeated(BossDefeated evt)
    {
        if (!Matches(evt.RoomId) || AudioManager.Instance == null)
            return;
        if (defeatMusic != null)
            AudioManager.Instance.PlayMusic(defeatMusic, fadeDuration);
        else
            AudioManager.Instance.StopMusic(fadeDuration);
        AudioManager.Instance.ReleaseMusic(this, resumeArea: false);
    }

    // Back inside the arena the boss restarts its fight (and its music); anywhere else the area's music returns.
    private void OnPlayerRespawned(PlayerRespawned evt)
    {
        if (AudioManager.Instance != null && !InArena(evt.Position))
            AudioManager.Instance.ReleaseMusic(this, resumeArea: true);
    }

    private bool InArena(Vector2 point)
    {
        foreach (Collider2D candidate in Physics2D.OverlapPointAll(point))
        {
            if (!candidate.isTrigger || !candidate.TryGetComponent(out LevelRoom room))
                continue;
            if (string.IsNullOrEmpty(roomId) ? room.IsBossRoom : room.RoomId == roomId)
                return true;
        }
        return false;
    }

    private bool Matches(string id) => string.IsNullOrEmpty(roomId) || id == roomId;
}
