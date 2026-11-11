using UnityEngine;

// Plays a boss's music when its fight actually starts (BossFightStarted, e.g. after the intro conversation), not
// when the player walks into the arena. When the boss is defeated the music fades out, or Defeat Music plays.
// Walking into another room brings that area's music back (RoomArea), also after the player dies mid-fight.
// Put it on the boss. Leave the arena's RoomArea Music Override empty.
public class BossMusic : MonoBehaviour
{
    [Tooltip("The arena's LevelRoom id (e.g. A3). Empty = react to any boss.")]
    [SerializeField] private string roomId;
    [SerializeField] private MusicTrack fightMusic;
    [Tooltip("Optional. Plays when the boss is defeated; empty = the fight music fades out.")]
    [SerializeField] private MusicTrack defeatMusic;
    [SerializeField, Min(0f)] private float fadeDuration = 1f;

    private void OnEnable()
    {
        EventBus.Subscribe<BossFightStarted>(OnFightStarted);
        EventBus.Subscribe<BossDefeated>(OnDefeated);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BossFightStarted>(OnFightStarted);
        EventBus.Unsubscribe<BossDefeated>(OnDefeated);
    }

    private void OnFightStarted(BossFightStarted evt)
    {
        if (Matches(evt.RoomId) && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(fightMusic, fadeDuration);
    }

    private void OnDefeated(BossDefeated evt)
    {
        if (!Matches(evt.RoomId) || AudioManager.Instance == null)
            return;
        if (defeatMusic != null)
            AudioManager.Instance.PlayMusic(defeatMusic, fadeDuration);
        else
            AudioManager.Instance.StopMusic(fadeDuration);
    }

    private bool Matches(string id) => string.IsNullOrEmpty(roomId) || id == roomId;
}
