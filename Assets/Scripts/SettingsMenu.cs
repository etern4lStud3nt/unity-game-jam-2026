using UnityEngine;
using UnityEngine.UI;

// The volume sliders: they show the saved volumes and set them through the AudioManager (which keeps them).
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private void Start()
    {
        Bind(sfxSlider, AudioManager.SfxVolume);
        Bind(musicSlider, AudioManager.MusicVolume);
    }

    private static void Bind(Slider slider, string parameter)
    {
        AudioManager audio = AudioManager.Instance;
        if (slider == null || audio == null)
            return;
        slider.SetValueWithoutNotify(audio.GetVolume(parameter));
        slider.onValueChanged.AddListener(value => audio.SetVolume(parameter, value));
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
    }
}
