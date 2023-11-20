using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PickaxeController : MonoBehaviour
{
    private Animator animator;
    private SoundController soundController;

    private void Start()
    {
        animator = GetComponent<Animator>();
        soundController = GetComponent<SoundController>();
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Hit");
        }
    }

    public void Hit()
    {
        //точка куда луч попал
        RaycastHit hit;
        //луч из центра экрана
        Ray screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width/2, Screen.height/2));
        //проверяет попадания луча 
        if (Physics.Raycast(screenRay, out hit, 1.5f))
        {
            StoneController stone = hit.collider.gameObject.GetComponentInParent<StoneController>();
            if (stone != null)
            {
                stone.TakeHit();
                soundController.PlayMiningSound();
            }            
        }
    }
}
