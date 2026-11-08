using System;
using UnityEngine;

// A piece of music: an optional intro that plays once, then one or more layers (stems) that loop together in sync.
// A plain looping track is just one layer. Extra layers (e.g. "Combat") can be faded in and out at runtime
// with AudioManager.SetMusicLayer.
[CreateAssetMenu(menuName = "Audio/Music Track", fileName = "MUS_New")]
public class MusicTrack : ScriptableObject
{
    [Serializable]
    public class Layer
    {
        public string Name = "Base";
        public AudioClip Clip;
        [Range(0f, 1f)] public float StartVolume = 1f;
    }

    [Tooltip("Optional. Plays once before the layers start looping.")]
    [SerializeField] private AudioClip intro;
    [Tooltip("All layers must be the same length so they stay in sync.")]
    [SerializeField] private Layer[] layers = { new() };
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    public AudioClip Intro => intro;
    public Layer[] Layers => layers;
    public float Volume => volume;

    public int IndexOfLayer(string layerName) => Array.FindIndex(layers, layer => layer.Name == layerName);
}
