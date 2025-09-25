using UnityEngine;
using UnityEngine.UI;

public class BuildFort : MonoBehaviour
{
    public GameObject fort;
public void Build(Transform locc)
    {
        Instantiate(fort, locc.position, locc.rotation);
    }
}
