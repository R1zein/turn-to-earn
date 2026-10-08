using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;



public class ToolController : MonoBehaviour
{
    private Animator animator;
    private SoundController soundController;
    [SerializeField] private List<MineableResourses> mineableResourses = new List<MineableResourses>();

    [Inject] private InputService input;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        soundController = GetComponent<SoundController>();
    }

    // Only the tool in hand is active, so only it swings.
    private void OnEnable()
    {
        input.OnAttack += Swing;
    }

    private void OnDisable()
    {
        input.OnAttack -= Swing;
    }

    private void Swing()
    {
        animator.SetTrigger("Hit");
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
