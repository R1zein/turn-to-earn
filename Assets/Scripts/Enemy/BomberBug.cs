public class BomberBug : NPCFacade
{
    protected override void NPCAnimationControl(bool engaging)
    {
        animator.SetFloat("locomotion", !engaging && motor.IsMoving ? 1f : 0f);
        if (engaging)
            animator.SetTrigger("attack1");
    }

    public void PlayAudio()
    {
    }
}
