using UnityEngine;

public class BotMiner : NPCFacade
{
    public float mineDistance = 5;
    public float cooldown;
    private float timer;

    protected override TargetKind? Kind => TargetKind.BotMiner;

    private void Start() => navigation.LookEverywhere(config.MinerTargets);

    protected override void Navigation() => navigation.Rescan(config.MinerTargets);

    protected override void NPCAnimationControl()
    {
        timer += Time.deltaTime;
        bool canMine = navigation.InAttackRange(mineDistance);
        animator.SetBool("Run", !canMine && motor.IsMoving);
        animator.SetBool("Idle", canMine || !motor.IsMoving);
        if (!canMine)
        {
            navigation.Follow();
            return;
        }

        motor.Stop();
        motor.Face(navigation.PointFrom(AimPoint));
        if (timer >= cooldown && navigation.target.TryGetComponent(out ResourceController node))
        {
            timer = 0f;
            node.TakeHit();
            animator.SetTrigger("Mine");
        }
    }
}
