using System.Collections.Generic;
using UnityEngine;

// Body of a base site: hidden until chosen, then its parts are switched on one by one.
public class Fortification : MonoBehaviour
{
    public List<GameObject> FortParts = new List<GameObject>();
    public string ID;

    public void BuildParts(string partName)
    {
        foreach (GameObject obj in FortParts)
        {
            if (obj.name == partName)
            {
                obj.SetActive(true);
            }
        }
    }
}
