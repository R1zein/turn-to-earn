using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPicker : MonoBehaviour
{
    //объект который должен быть выделен
    Selectable currentSelectable = null;

    private void LateUpdate()
    {
        //точка куда луч попал
        RaycastHit hit;
        //луч из центра экрана
        Ray screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width/2, Screen.height/2));
        if (Physics.Raycast(screenRay, out hit))
        {
            Selectable selectable = hit.collider.gameObject.GetComponentInParent<Selectable>();

            if (selectable)
            {
                if (currentSelectable && currentSelectable != selectable)
                    currentSelectable.Deselect();

                currentSelectable = selectable;
                selectable.Select();
            }
            else
            {
                if (currentSelectable)
                {
                    currentSelectable.Deselect();
                    currentSelectable = null;
                }
            }
        }
        else if (currentSelectable)
        {
            currentSelectable.Deselect();
            currentSelectable = null;
        }
    }
}
