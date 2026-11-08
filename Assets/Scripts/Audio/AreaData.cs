using UnityEngine;

// A region of the world (forest, caves...). Rooms point at their area; music and ambience only change
// when the player crosses into a different area, not on every room change.
[CreateAssetMenu(menuName = "Audio/Area", fileName = "AREA_New")]
public class AreaData : ScriptableObject
{
    [SerializeField] private MusicTrack music;
    [Tooltip("A looping sound (wind, dripping water...). Leave empty for none.")]
    [SerializeField] private SoundData ambience;
    [SerializeField, Min(0f)] private float fadeDuration = 2f;

    public MusicTrack Music => music;
    public SoundData Ambience => ambience;
    public float FadeDuration => fadeDuration;
}
