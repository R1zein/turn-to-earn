using System.Collections.Generic;
using UnityEngine;

public class Fortification : MonoBehaviour
{
    public List<GameObject> FortParts = new List<GameObject>();
    public string ID;

    public void BuildParts(GameObject part)
    {
        foreach (GameObject obj in FortParts)
        {
            if (obj.name == part.name)
            {
                obj.SetActive(true);
            }
        }
    }
}