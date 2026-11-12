using UnityEngine;

// A ladder the player climbs. While the player's body is in its trigger the legs
// climb instead of walking (PlayerMovement.Ladder). The ladder leans on a deck:
// `rails` run from its foot to its top in its own frame, and the climb ends at
// `topHeight` above its foot, where the player steps off toward what it leans on.
[RequireComponent(typeof(Collider))]
public class Ladder : MonoBehaviour
{
    [Tooltip("Direction of the rails from the foot up, in the ladder's own frame")]
    [SerializeField] private Vector3 rails = Vector3.up;
    [Tooltip("Height of the deck it leads to above its foot, m")]
    [SerializeField] private float topHeight = 3f;

    private PlayerMovement climber;

    // Along the rails, upward.
    public Vector3 Up => transform.TransformDirection(rails).normalized;

    // Level, from the climber to what the ladder leans on.
    public Vector3 Toward => Vector3.ProjectOnPlane(Up, Vector3.up).normalized;

    // World height of the deck at its top.
    public float TopY => transform.position.y + topHeight;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement legs))
        {
            climber = legs;
            legs.Ladder = this;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement legs) && legs == climber)
            LetGo();
    }

    // Taken down (the wall upgraded or dismantled) with the player on it.
    private void OnDisable()
    {
        if (climber != null)
            LetGo();
    }

    private void LetGo()
    {
        if (climber.Ladder == this)
            climber.Ladder = null;
        climber = null;
    }
}
