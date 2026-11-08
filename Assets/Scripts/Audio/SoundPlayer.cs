using UnityEngine;

// Plays sounds without writing code: call Play() from a UnityEvent (buttons, triggers) or from an Animation Event.
// For footsteps or swings, add an Animation Event on the right frame that calls PlaySound and drag a SoundData
// asset into the event's Object field.
public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private SoundData sound;

    public void Play()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(sound, transform.position);
    }

    public void PlaySound(Object soundAsset)
    {
        if (AudioManager.Instance != null && soundAsset is SoundData soundData)
            AudioManager.Instance.PlaySFX(soundData, transform.position);
    }

    public void StartLoop()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StartLoopedSFX(sound, transform.position);
    }

    public void StopLoop()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopLoopedSFX(sound);
    }
}
