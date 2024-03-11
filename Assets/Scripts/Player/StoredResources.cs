using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoredResources : MonoBehaviour
{
    [SerializeField] Text playerOreStoreText;
    [SerializeField] TMP_Text playerTreeStoreText;
    [SerializeField] TMP_Text playerIronStoreText;

    private AllResources currentResources = new AllResources();
    public AllResources CurrentResources => currentResources;



    public static StoredResources instance;

    private void UpdateText()
    {
        playerOreStoreText.text = $"Добыто камня: {currentResources.ore}";
        playerTreeStoreText.text = $"Добыто дерева: {currentResources.tree}";
        playerIronStoreText.text = $"Добыто железа: {currentResources.iron}";
    }

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


        // убирает игроку руду
    public void DecreaseResources(AllResources resources)
    {
        currentResources -= resources;
        UpdateText();
    }
    public void AddResources(AllResources resources)
    {
        currentResources += resources;
        UpdateText();
    }
}
