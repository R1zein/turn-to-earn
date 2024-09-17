using TMPro;
using UnityEngine;

public class PriceTextBuilder : MonoBehaviour
{
    public Ghost ghost;
    public TMP_Text costText;
    private void Start()
    {
        costText.text = $"Дерево: {ghost.requiredResources.tree} \n Железо: {ghost.requiredResources.iron} \n Камень: {ghost.requiredResources.ore}";
    }

}

