using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.Tests.CoreCLR.TestJobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Roulette_UI : MonoBehaviour
{
    [SerializeField] private Roulette roulette;
    [SerializeField] private Transform pocketContainer;
    [SerializeField] private GameObject tilePrefab;

    private void Start()
    {
        // Generate pocket tiles
        foreach (int number in roulette.Pockets.Keys)
        {
            Color color = roulette.Pockets[number];
            if (color == Color.green) continue;

            GameObject tile = Instantiate(tilePrefab, pocketContainer);
            tile.GetComponent<Image>().color = color;
            tile.GetComponentInChildren<TMP_Text>().text = number.ToString();
        }
    }
}
