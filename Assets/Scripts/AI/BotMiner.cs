using UnityEngine;

public class BotMiner : NPCFacade
{
    public float mineDistance = 5f;
    public float cooldown;
    private float timer;

    protected override float EngageDistance => mineDistance;

    protected override void NPCAnimationControl(bool engaging)
    {
        timer += Time.deltaTime;
        animator.SetBool("Run", !engaging && motor.IsMoving);
        animator.SetBool("Idle", engaging || !motor.IsMoving);
        if (engaging && timer >= cooldown && Target.TryGetComponent(out ResourceController node))
        {
            timer = 0f;
            node.TakeHit();
            animator.SetTrigger("Mine");
        }
    }
}
