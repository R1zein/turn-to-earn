using UnityEngine;
using Zenject;

// A hammer: swings on attack, and the swing's animation event mends the structure
// in front of it. How much each hit gives back is the hammer's quality.
public class RepairTool : MonoBehaviour
{
    private const float Reach = 2f;

    [Tooltip("HP given back by one hit")]
    [SerializeField] private float repairPerHit = 25f;

    [Inject] private InputService input;
    [Inject] private StructureLookup structures;

    private Animator animator;
    private SoundController soundController;

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

    // Animation event of the swing.
    public void Hit()
    {
        Ray screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (!Physics.Raycast(screenRay, out RaycastHit hit, Reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        Structure structure = structures.At(hit);
        if (structure == null || structure.Stats.currHP >= structure.Stats.maxHP)
            return;

        structure.Repair(repairPerHit);
        soundController.PlayMiningSound();
    }
}
