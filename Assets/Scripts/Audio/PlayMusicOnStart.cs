using UnityEngine;

// Starts a music track through the AudioManager when the scene starts (the main menu theme).
public class PlayMusicOnStart : MonoBehaviour
{
    [SerializeField] private MusicTrack track;
    [SerializeField, Min(0f)] private float fadeDuration = 1f;

    private void Start()
    {
        if (track != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(track, fadeDuration);
    }
}
