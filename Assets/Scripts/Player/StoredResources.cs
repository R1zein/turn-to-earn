using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoredResources : MonoBehaviour
{
    [SerializeField] int playerOreStore;
    [SerializeField] Text playerOreStoreText;
    [SerializeField] int playerTreeStore;
    [SerializeField] TMP_Text playerTreeStoreText;
    [SerializeField] int playerIronStore;
    [SerializeField] TMP_Text playerIronStoreText;
    public int PlayerOreStore => playerOreStore;
    public int PlayerTreeStore => playerTreeStore;
    public int PlayerIronStore => playerIronStore;

    public static StoredResources instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
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

    public AllResources GetResources()
    {
        AllResources res = new AllResources();
        res.iron = playerIronStore;
        res.tree = playerTreeStore;
        res.ore = playerOreStore;
        return res;
    }

        // убирает игроку руду
        public void DecreasePlayerOre()
    {

    }
}
