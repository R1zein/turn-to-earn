using UnityEngine;

public class BomberBug :NPCFacade
{


    // Was never found by defenders or turrets (they looked for Enemy), kept that way.
    protected override TargetKind? Kind => null;

    void Start()
    {
        navigation.LookEverywhere(config.BomberTargets);
    }

    protected override void Navigation()
    {
        navigation.Rescan(config.BomberTargets);
    }

    protected override void NPCAnimationControl()
    {
        if (navigation.target == null)
        {
            animator.SetFloat("locomotion", 0);
            return;
        }
        animator.SetFloat("locomotion", 1f);
        if (Vector3.Distance(transform.position, navigation.target.transform.position) <= navigation.attackDistance)
        {
            animator.SetTrigger("attack1");
            return;
        }

    }

    public void PlayAudio()
    {
        
    }
}
