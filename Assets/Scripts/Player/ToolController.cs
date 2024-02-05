using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ToolController : MonoBehaviour
{
    private Animator animator;
    private SoundController soundController;
    [SerializeField] private List<MineableResourses> mineableResourses = new List<MineableResourses>();

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
            ResourceController resourceController = hit.collider.gameObject.GetComponentInParent<ResourceController>();
            if (resourceController != null )
            {
                foreach (var resource in mineableResourses)
                {
                    if (resource == resourceController.resourse)
                    {
                        resourceController.TakeHit();
                        soundController.PlayMiningSound();
                    }
                }
            }
        }
    }
}
