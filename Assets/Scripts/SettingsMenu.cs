using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private void Awake()
    {
        sfxSlider.onValueChanged.AddListener(HandleSFXSliderValueChanged);
        musicSlider.onValueChanged.AddListener(HandleMusicSliderValueChanged);

        // Set those values to the values of the audio manager.
        //sfxSlider.value = 1;
        //musicSlider.value = 1;
    }

    private void HandleSFXSliderValueChanged(float sliderValue)
    {
        // Change sfx volume here
    }

    private void HandleMusicSliderValueChanged(float sliderValue)
    {
        // Change music volume here
    }

}
