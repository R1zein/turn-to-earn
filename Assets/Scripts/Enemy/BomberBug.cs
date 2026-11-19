using UnityEngine;

public class BomberBug : NPCFacade
{
    protected override TargetKind? Kind => null;

    private void Start() => navigation.LookEverywhere(config.BomberTargets);

    protected override void Navigation() => navigation.Rescan(config.BomberTargets);

    protected override void NPCAnimationControl()
    {
        bool attacking = navigation.InAttackRange(navigation.attackDistance);
        animator.SetFloat("locomotion", !attacking && motor.IsMoving ? 1f : 0f);
        if (!attacking)
        {
            navigation.Follow();
            return;
        }

        motor.Stop();
        motor.Face(navigation.PointFrom(AimPoint));
        animator.SetTrigger("attack1");
    }

    public void PlayAudio()
    {
    }
}
