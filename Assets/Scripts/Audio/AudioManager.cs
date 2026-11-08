using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

// Plays everything audio through the MainMixer:
// - SFX from SoundData assets, on pooled sources so each play can have its own pitch and stereo pan.
// - Music from MusicTrack assets, on two decks so a new track crossfades in while the old one fades out.
//   Layers of the playing track can be faded in and out (e.g. a combat layer).
// - Ambience loops per area, also crossfaded.
// Fades run on unscaled time so they keep going while the game is paused.
public class AudioManager : Singleton<AudioManager>
{
    public const string MasterVolume = "MasterVolume";
    public const string MusicVolume = "MusicVolume";
    public const string AmbienceVolume = "AmbienceVolume";
    public const string SfxVolume = "SFXVolume";
    public const string UIVolume = "UIVolume";

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [Tooltip("Used by SoundData assets that don't pick their own group.")]
    [SerializeField] private AudioMixerGroup defaultSfxGroup;
    [SerializeField] private AudioMixerSnapshot normalSnapshot;
    [SerializeField] private AudioMixerSnapshot pausedSnapshot;

    [Header("Music")]
    [SerializeField, Min(0f)] private float defaultFadeDuration = 1.5f;

    [Header("SFX")]
    [Tooltip("Most sound effects that can play at once. Extra plays are dropped.")]
    [SerializeField, Min(1)] private int maxSfxVoices = 24;
    [Tooltip("World distance from the camera centre at which a positioned sound is fully in one ear.")]
    [SerializeField, Min(0.01f)] private float stereoPanWidth = 12f;

    private ObjectPool<AudioSource> sfxPool;
    private readonly List<AudioSource> playingSfx = new();
    private readonly Dictionary<SoundData, AudioSource> loopedSfx = new();
    private readonly Dictionary<SoundData, float> lastPlayTimes = new();

    private readonly MusicDeck[] musicDecks = new MusicDeck[2];
    private int activeMusicDeck;

    private readonly AudioSource[] ambienceSources = new AudioSource[2];
    private readonly Fade[] ambienceFades = new Fade[2];
    private readonly SoundData[] ambienceSounds = new SoundData[2];
    private int activeAmbience;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        // Keep music playing across scene loads.
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);

        sfxPool = new ObjectPool<AudioSource>(
            () => CreateSource("SFX", defaultSfxGroup),
            actionOnRelease: source =>
            {
                source.Stop();
                source.clip = null;
                source.loop = false;
            },
            defaultCapacity: 8,
            maxSize: maxSfxVoices);

        musicDecks[0] = new MusicDeck(CreateChild("Music Deck A"));
        musicDecks[1] = new MusicDeck(CreateChild("Music Deck B"));

        ambienceSources[0] = CreateSource("Ambience A", ambienceGroup);
        ambienceSources[1] = CreateSource("Ambience B", ambienceGroup);
        ambienceSources[0].loop = ambienceSources[1].loop = true;
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        // Hand finished one-shots back to the pool.
        for (int i = playingSfx.Count - 1; i >= 0; i--)
        {
            if (playingSfx[i].isPlaying)
                continue;

            sfxPool.Release(playingSfx[i]);
            playingSfx.RemoveAt(i);
        }

        foreach (MusicDeck deck in musicDecks)
            deck.Tick(deltaTime);

        for (int i = 0; i < ambienceSources.Length; i++)
        {
            ambienceFades[i].Tick(deltaTime);
            AudioSource source = ambienceSources[i];
            float baseVolume = ambienceSounds[i] != null ? ambienceSounds[i].Volume : 0f;
            source.volume = baseVolume * ambienceFades[i].Value;
            if (source.isPlaying && ambienceFades[i].IsSilent)
                source.Stop();
        }
    }

    // ---------- SFX ----------

    public void PlaySFX(SoundData sound) => PlaySfxInternal(sound, null);

    // Pans the sound left/right by where it happens relative to the camera.
    public void PlaySFX(SoundData sound, Vector3 position) => PlaySfxInternal(sound, position);

    public void StartLoopedSFX(SoundData sound) => StartLoopedSfxInternal(sound, null);

    public void StartLoopedSFX(SoundData sound, Vector3 position) => StartLoopedSfxInternal(sound, position);

    public void StopLoopedSFX(SoundData sound)
    {
        if (sound == null || !loopedSfx.Remove(sound, out AudioSource source))
            return;

        sfxPool.Release(source);
    }

    private void PlaySfxInternal(SoundData sound, Vector3? position)
    {
        if (sound == null || playingSfx.Count + loopedSfx.Count >= maxSfxVoices || IsCoolingDown(sound))
            return;
        if (!sound.TryGetClip(out AudioClip clip))
            return;

        AudioSource source = sfxPool.Get();
        Configure(source, sound, clip, position);
        source.Play();
        playingSfx.Add(source);
    }

    private void StartLoopedSfxInternal(SoundData sound, Vector3? position)
    {
        if (sound == null || loopedSfx.ContainsKey(sound) || !sound.TryGetClip(out AudioClip clip))
            return;

        AudioSource source = sfxPool.Get();
        Configure(source, sound, clip, position);
        source.loop = true;
        source.Play();
        loopedSfx[sound] = source;
    }

    private bool IsCoolingDown(SoundData sound)
    {
        float now = Time.unscaledTime;
        if (sound.Cooldown > 0f && lastPlayTimes.TryGetValue(sound, out float lastTime) && now - lastTime < sound.Cooldown)
            return true;

        lastPlayTimes[sound] = now;
        return false;
    }

    private void Configure(AudioSource source, SoundData sound, AudioClip clip, Vector3? position)
    {
        source.clip = clip;
        source.outputAudioMixerGroup = sound.MixerGroup != null ? sound.MixerGroup : defaultSfxGroup;
        source.volume = sound.Volume;
        source.pitch = sound.RandomPitch;
        source.panStereo = 0f;

        Camera listenerCamera = Camera.main;
        if (position.HasValue && listenerCamera != null)
            source.panStereo = Mathf.Clamp((position.Value.x - listenerCamera.transform.position.x) / stereoPanWidth, -1f, 1f);
    }

    // ---------- Music ----------

    public void PlayMusic(MusicTrack track) => PlayMusic(track, defaultFadeDuration);

    // Crossfades to the track. Asking for the track that is already playing does nothing,
    // so rooms can call this every time the player walks in.
    public void PlayMusic(MusicTrack track, float fadeDuration)
    {
        if (track == null)
        {
            StopMusic(fadeDuration);
            return;
        }

        MusicDeck current = musicDecks[activeMusicDeck];
        if (current.Track == track && current.IsFadingIn)
            return;

        MusicDeck incoming = musicDecks[1 - activeMusicDeck];
        incoming.Play(track, fadeDuration, musicGroup);
        current.FadeOut(fadeDuration);
        activeMusicDeck = 1 - activeMusicDeck;
    }

    public void StopMusic() => StopMusic(defaultFadeDuration);

    public void StopMusic(float fadeDuration)
    {
        foreach (MusicDeck deck in musicDecks)
            deck.FadeOut(fadeDuration);
    }

    // Fades one layer of the playing track, e.g. SetMusicLayer("Combat", 1f, 2f) when a fight starts.
    public void SetMusicLayer(string layerName, float volume, float fadeDuration)
    {
        musicDecks[activeMusicDeck].SetLayer(layerName, volume, fadeDuration);
    }

    // ---------- Areas & ambience ----------

    // Called by RoomArea. musicOverride lets a room (a boss arena) play something other than its area's music.
    public void EnterArea(AreaData area, MusicTrack musicOverride = null)
    {
        if (area == null)
            return;

        PlayMusic(musicOverride != null ? musicOverride : area.Music, area.FadeDuration);
        PlayAmbience(area.Ambience, area.FadeDuration);
    }

    public void PlayAmbience(SoundData ambience, float fadeDuration)
    {
        if (ambience != null && ambience == ambienceSounds[activeAmbience] && !ambienceFades[activeAmbience].IsFadingOut)
            return;

        ambienceFades[activeAmbience].To(0f, fadeDuration);
        if (ambience == null || !ambience.TryGetClip(out AudioClip clip))
            return;

        activeAmbience = 1 - activeAmbience;
        AudioSource source = ambienceSources[activeAmbience];
        source.clip = clip;
        source.outputAudioMixerGroup = ambience.MixerGroup != null ? ambience.MixerGroup : ambienceGroup;
        source.Play();
        ambienceSounds[activeAmbience] = ambience;
        ambienceFades[activeAmbience] = new Fade();
        ambienceFades[activeAmbience].To(1f, fadeDuration);
    }

    // ---------- Mixer ----------

    // volume is 0..1 (a settings slider); the mixer works in decibels.
    public void SetVolume(string exposedParameter, float volume)
    {
        mixer.SetFloat(exposedParameter, volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f);
    }

    // Muffles music and ambience (lowpass) while a pause menu is open. The Paused snapshot only changes the filters:
    // group volumes are exposed for SetVolume, and an exposed parameter always overrides snapshot values.
    public void SetPaused(bool paused, float transitionDuration = 0.3f)
    {
        AudioMixerSnapshot snapshot = paused ? pausedSnapshot : normalSnapshot;
        if (snapshot != null)
            snapshot.TransitionTo(transitionDuration);
    }

    // ---------- Helpers ----------

    private Transform CreateChild(string childName)
    {
        var child = new GameObject(childName).transform;
        child.SetParent(transform, false);
        return child;
    }

    private AudioSource CreateSource(string sourceName, AudioMixerGroup group)
    {
        var source = CreateChild(sourceName).gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = group;
        source.playOnAwake = false;
        return source;
    }

    // A volume that moves toward a target over a set time.
    private struct Fade
    {
        public float Value;
        private float target;
        private float speed;

        public bool IsSilent => Value <= 0f && target <= 0f;
        public bool IsFadingIn => target > 0f;
        public bool IsFadingOut => target <= 0f;

        public void To(float newTarget, float duration)
        {
            target = newTarget;
            if (duration > 0f)
                speed = Mathf.Abs(newTarget - Value) / duration;
            else
                Value = newTarget;
        }

        public void Snap(float value)
        {
            Value = target = value;
        }

        public void Tick(float deltaTime)
        {
            Value = Mathf.MoveTowards(Value, target, speed * deltaTime);
        }
    }

    // One playing music track: an intro source plus one looping source per layer, all scheduled on the DSP clock
    // so the layers start on the same sample and the loop starts exactly when the intro ends.
    private class MusicDeck
    {
        private readonly Transform root;
        private readonly AudioSource intro;
        private readonly List<AudioSource> layerSources = new();
        private Fade[] layerFades = new Fade[0];
        private Fade deckFade;

        public MusicTrack Track { get; private set; }
        public bool IsFadingIn => deckFade.IsFadingIn;

        public MusicDeck(Transform root)
        {
            this.root = root;
            intro = NewSource("Intro");
        }

        public void Play(MusicTrack track, float fadeDuration, AudioMixerGroup group)
        {
            StopAll();
            Track = track;

            MusicTrack.Layer[] layers = track.Layers;
            while (layerSources.Count < layers.Length)
                layerSources.Add(NewSource($"Layer {layerSources.Count}"));

            layerFades = new Fade[layers.Length];
            double startTime = AudioSettings.dspTime + 0.1;
            double loopStartTime = startTime;

            intro.outputAudioMixerGroup = group;
            if (track.Intro != null)
            {
                intro.clip = track.Intro;
                intro.PlayScheduled(startTime);
                loopStartTime += (double)track.Intro.samples / track.Intro.frequency;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                AudioSource source = layerSources[i];
                source.outputAudioMixerGroup = group;
                source.clip = layers[i].Clip;
                source.loop = true;
                layerFades[i].Snap(layers[i].StartVolume);
                if (source.clip != null)
                    source.PlayScheduled(loopStartTime);
            }

            deckFade = new Fade();
            deckFade.To(1f, fadeDuration);
            ApplyVolumes();
        }

        public void FadeOut(float fadeDuration)
        {
            deckFade.To(0f, fadeDuration);
        }

        public void SetLayer(string layerName, float volume, float fadeDuration)
        {
            int index = Track != null ? Track.IndexOfLayer(layerName) : -1;
            if (index < 0)
            {
                Debug.LogWarning($"AudioManager: the playing music has no layer named '{layerName}'");
                return;
            }

            layerFades[index].To(volume, fadeDuration);
        }

        public void Tick(float deltaTime)
        {
            if (Track == null)
                return;

            deckFade.Tick(deltaTime);
            for (int i = 0; i < layerFades.Length; i++)
                layerFades[i].Tick(deltaTime);

            ApplyVolumes();
            if (deckFade.IsSilent)
                StopAll();
        }

        private void ApplyVolumes()
        {
            if (Track == null)
                return;

            float deckVolume = Track.Volume * deckFade.Value;
            intro.volume = deckVolume;
            for (int i = 0; i < layerFades.Length; i++)
                layerSources[i].volume = deckVolume * layerFades[i].Value;
        }

        private void StopAll()
        {
            intro.Stop();
            intro.clip = null;
            foreach (AudioSource source in layerSources)
            {
                source.Stop();
                source.clip = null;
            }

            Track = null;
        }

        private AudioSource NewSource(string sourceName)
        {
            var child = new GameObject(sourceName).transform;
            child.SetParent(root, false);
            var source = child.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }
    }
}
