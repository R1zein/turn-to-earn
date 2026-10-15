using System.Collections.Generic;
using UnityEngine;

// Body of a base site: hidden until chosen, then its parts are switched on one by
// one. A part is built when its object is active; every part starts switched off.
public class Fortification : MonoBehaviour
{
    public List<GameObject> FortParts = new List<GameObject>();
    public string ID;

    public bool IsPartBuilt(string partName)
    {
        GameObject part = FindPart(partName);
        return part != null && part.activeSelf;
    }

    // False when the site has no such part or it is already built.
    public bool TryBuildPart(string partName)
    {
        GameObject part = FindPart(partName);
        if (part == null || part.activeSelf)
            return false;

        part.SetActive(true);
        return true;
    }

    private GameObject FindPart(string partName)
    {
        foreach (GameObject obj in FortParts)
        {
            if (obj.name == partName)
                return obj;
        }
        return null;
    }
}
