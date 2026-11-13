using System;
using Unity.Tutorials.Editor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject creditsMenu;

    public void StartGame()
    {
        SceneManager.LoadScene("Game 1");
    }
}
