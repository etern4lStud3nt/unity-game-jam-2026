using UnityEngine;

// The player's combat and life sounds: a swing for every attack, a hurt sound when damaged and a short sting on
// death. Listens to PlayerAttack and the EventBus, so neither the attack nor the health code knows about audio.
// Put it on the player character; movement sounds stay in CharacterAudio.
public class PlayerSounds : MonoBehaviour
{
    [SerializeField] private SoundData swing;
    [SerializeField] private SoundData hit;
    [SerializeField] private SoundData hurt;
    [SerializeField] private SoundData death;

    private PlayerAttack attack;

    private void Awake()
    {
        attack = GetComponent<PlayerAttack>();
    }

    private void OnEnable()
    {
        if (attack != null)
        {
            attack.Swung += OnSwung;
            attack.HitLanded += OnHitLanded;
        }
        EventBus.Subscribe<PlayerDamaged>(OnDamaged);
        EventBus.Subscribe<PlayerDied>(OnDied);
    }

    private void OnDisable()
    {
        if (attack != null)
        {
            attack.Swung -= OnSwung;
            attack.HitLanded -= OnHitLanded;
        }
        EventBus.Unsubscribe<PlayerDamaged>(OnDamaged);
        EventBus.Unsubscribe<PlayerDied>(OnDied);
    }

    private void OnSwung(Vector2 direction) => Play(swing);

    private void OnHitLanded(Health target) => Play(hit);

    // The last hit plays the death sting instead of the hurt sound.
    private void OnDamaged(PlayerDamaged evt)
    {
        if (evt.CurrentHealth > 0)
            Play(hurt);
    }

    private void OnDied(PlayerDied evt) => Play(death);

    private void Play(SoundData sound)
    {
        if (sound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(sound, transform.position);
    }
}
