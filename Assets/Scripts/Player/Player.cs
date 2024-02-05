using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [SerializeField] int playerOreStore;
    [SerializeField] Text playerOreStoreText;
    [SerializeField] int playerTreeStore;
    [SerializeField] TMP_Text playerTreeStoreText;
    [SerializeField] int playerIronStore;
    [SerializeField] TMP_Text playerIronStoreText;

    public static Player playerInstance;

    private void Awake()
    {
        playerInstance = this;
    }

    //добавляет игроку руду
    public void AddPlayerOre(int newOre)
    {
        playerOreStore += newOre;
        playerOreStoreText.text = $"Добыто камня: {playerOreStore}";
    }
    public void AddPlayerTree(int newTree)
    {
        playerTreeStore += newTree;
        playerTreeStoreText.text = $"Добыто дерева: {playerTreeStore}";
    }
    public void AddPlayerIron(int newIron)
    {
        playerIronStore += newIron;
        playerIronStoreText.text = $"Добыто железа: {playerIronStore}";
    }

        // убирает игроку руду
        public void DecreasePlayerOre()
    {

    }
}
