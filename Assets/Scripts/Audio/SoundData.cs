using UnityEngine;
using UnityEngine.Audio;

// One sound effect as an asset: a few clip variations picked at random, with its own volume, pitch spread and cooldown.
// Gameplay code references the asset and hands it to AudioManager.PlaySFX.
[CreateAssetMenu(menuName = "Audio/Sound", fileName = "SFX_New")]
public class SoundData : ScriptableObject
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("A random pitch in this range is used each time, so repeated sounds don't feel copy-pasted.")]
    [SerializeField] private Vector2 pitchRange = new(1f, 1f);
    [Tooltip("Minimum seconds between two plays of this sound. Stops footsteps or hits stacking into one loud burst.")]
    [SerializeField, Min(0f)] private float cooldown;
    [Tooltip("Leave empty to use AudioManager's default group for this kind of sound.")]
    [SerializeField] private AudioMixerGroup mixerGroup;
    [Tooltip("World distance from the camera at which a positioned sound fades to silence. 0 = heard everywhere at full volume " +
             "(player and UI sounds).")]
    [SerializeField, Min(0f)] private float hearingRange;
    [Tooltip("When every voice is busy, a new sound replaces the oldest playing sound with a lower priority. " +
             "Give the player's hurt/death and boss sounds a high value, footsteps a low one.")]
    [SerializeField, Range(0, 100)] private int priority = 50;

    private int lastClipIndex = -1;

    public float Volume => volume;
    public float Cooldown => cooldown;
    public AudioMixerGroup MixerGroup => mixerGroup;
    public float HearingRange => hearingRange;
    public int Priority => priority;
    public float RandomPitch => Random.Range(pitchRange.x, pitchRange.y);

    // Picks a random clip, avoiding the one played last time when there is a choice.
    public bool TryGetClip(out AudioClip clip)
    {
        clip = null;
        if (clips == null || clips.Length == 0)
            return false;

        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && index == lastClipIndex)
            index = (index + 1) % clips.Length;

        lastClipIndex = index;
        clip = clips[index];
        return clip != null;
    }
}
