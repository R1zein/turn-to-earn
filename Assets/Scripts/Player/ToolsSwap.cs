using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolsSwap : MonoBehaviour
{
    [SerializeField] private PickaxeController pickaxe;
    [SerializeField] private PickaxeController axe;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Q)) 
        {
            axe.gameObject.SetActive(true);
            pickaxe.gameObject.SetActive(false);
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            axe.gameObject.SetActive(false);
            pickaxe.gameObject.SetActive(true);
        }
    }
}
