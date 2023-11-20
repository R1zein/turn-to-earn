using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [SerializeField] int playerOreStore;
    [SerializeField] Text playerOreStoreText;

    public static Player playerInstance;

    private void Awake()
    {
        playerInstance = this;
    }

    //добавляет игроку руду
    public void AddPlayerOre(int newOre)
    {
        playerOreStore += newOre;
        playerOreStoreText.text = $"Добыто руды: {playerOreStore}";
    }

    // убирает игроку руду
    public void DecreasePlayerOre()
    {

    }
}
