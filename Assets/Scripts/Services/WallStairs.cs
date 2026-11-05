using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Stairs between the walkways of two placed walls that stand in one line, end to
// end, one BuildHeightStep apart in height. They sit on the lower walkway against
// the joint and climb to the upper one; the number of steps follows the height
// difference, and each step is squashed a little so the last one meets the upper
// deck exactly. Walls further apart than one step get no stairs.
// Steps are only looks: the player climbs an invisible ramp along them, because a
// capsule does not step up boxes by itself.
// The deck boards of the lower wall under the steps are cut out by WallMeshCuts
// (only its look: the collider keeps them), and come back when the stairs go.
// Like a corner, the stairs stand while both walls stand.
public class WallStairs
{
    private const float StepRise = 0.25f;   // the step mesh is modelled at this rise
    private const float StepRun = 0.4f;
    private const float MinRise = 0.2f;     // less than this is no step at all
    private const float MaxRise = 0.55f;    // one BuildHeightStep: 2 steps
    // Where the walkway is in the wall's own frame: deck top and the middle of its width.
    private const float DeckHeight = 3.0f;
    private const float WalkwayMiddle = 0.81f;
    private const float WalkwayWidth = 1.3f;
    private const float RampThickness = 0.1f;
    // The ramp runs on along the lower walkway before the first step, so it is
    // gentler than the steps: a running body still takes off at its top, the less
    // the flatter it is. Not longer than the walkway of one wall, though.
    private const float RampPerRise = 2.6f;
    private const float RampMinLead = 0.4f;
    private const float RampMaxLength = 3.9f;
    private const int RampSegments = 8;
    // Grid snapping leaves walls in line to a few centimetres.
    private const float LineTolerance = 0.3f;
    // Deck boards in the wall's frame: a thin slab at the deck top across the walkway.
    // Joists and rails reach lower or run the whole wall, so they stay.
    private const float DeckBoardsBottom = 2.95f;
    private const float DeckBoardsTop = 3.01f;
    private const float DeckNearEdge = 0.1f;
    private const float DeckFarEdge = 1.5f;
    // Boards are cut only when they lie wholly under the stairs, plus this much.
    private const float DeckCutMargin = 0.05f;

    private class Stair
    {
        public WallSegment Lower;
        public WallSegment Upper;
        public GameObject Root;
        public bool Uses(WallSegment wall) => wall == Lower || wall == Upper;
    }

    [Inject] private GameConfig config;
    [Inject] private BuildingPlacer placer;
    [Inject] private WallMeshCuts cuts;

    private readonly List<WallSegment> walls = new List<WallSegment>();
    private readonly List<Stair> stairs = new List<Stair>();

    public void Add(WallSegment wall)
    {
        foreach (WallSegment other in walls)
            TryConnect(wall, other);
        walls.Add(wall);
    }

    public void Remove(WallSegment wall)
    {
        walls.Remove(wall);
        for (int i = stairs.Count - 1; i >= 0; i--)
        {
            if (!stairs[i].Uses(wall))
                continue;

            cuts.Remove(stairs[i]);
            Object.Destroy(stairs[i].Root);
            stairs.RemoveAt(i);
        }
    }

    private void TryConnect(WallSegment first, WallSegment second)
    {
        // Same turn: the walkways are on the same side and continue each other.
        if (Quaternion.Angle(first.transform.rotation, second.transform.rotation) > 1f)
            return;

        Vector3 offset = second.transform.position - first.transform.position;
        float along = Vector3.Dot(offset, first.Along);
        Vector3 across = Vector3.ProjectOnPlane(offset - first.Along * along, Vector3.up);
        float rise = Mathf.Abs(offset.y);
        if (Mathf.Abs(Mathf.Abs(along) - WallSegment.HalfLength * 2f) > LineTolerance
            || across.magnitude > LineTolerance
            || rise < MinRise || rise > MaxRise)
            return;

        WallSegment lower = offset.y > 0f ? first : second;
        WallSegment upper = lower == first ? second : first;
        // Which end of the lower wall touches the upper one.
        int end = Vector3.Dot(upper.transform.position - lower.transform.position, lower.Along) > 0f ? 1 : -1;
        float run = StepCount(rise) * StepRun;
        Vector2 span = end > 0
            ? new Vector2(WallSegment.HalfLength - run, WallSegment.HalfLength)
            : new Vector2(-WallSegment.HalfLength, -WallSegment.HalfLength + run);
        var stair = new Stair { Lower = lower, Upper = upper, Root = Build(lower, end, rise) };
        stairs.Add(stair);
        // The deck boards under the steps, in the lower wall's own frame.
        var boards = new Bounds(
            new Vector3((span.x + span.y) * 0.5f, (DeckBoardsBottom + DeckBoardsTop) * 0.5f, (DeckNearEdge + DeckFarEdge) * 0.5f),
            new Vector3(span.y - span.x + DeckCutMargin * 2f, DeckBoardsTop - DeckBoardsBottom, DeckFarEdge - DeckNearEdge));
        cuts.Add(stair, lower, lower.transform, new[] { boards }, false);
    }

    private static int StepCount(float rise) => Mathf.Max(1, Mathf.RoundToInt(rise / StepRise));

    private GameObject Build(WallSegment lower, int end, float rise)
    {
        int steps = StepCount(rise);
        float stepRise = rise / steps;
        float run = steps * StepRun;

        // The stairs' own frame climbs along +X towards the joint; facing the other end
        // means a half turn, which the symmetric step mesh does not mind.
        Transform body = lower.transform;
        Quaternion rotation = body.rotation * Quaternion.Euler(0f, end > 0 ? 0f : 180f, 0f);
        Vector3 start = body.TransformPoint(new Vector3(end * (WallSegment.HalfLength - run), DeckHeight, WalkwayMiddle));

        var root = new GameObject("WallStairs");
        root.transform.SetPositionAndRotation(start, rotation);
        for (int i = 0; i < steps; i++)
        {
            GameObject step = placer.Place(config.WallStairStep, root.transform.TransformPoint(new Vector3(i * StepRun, i * stepRise, 0f)), rotation);
            step.transform.SetParent(root.transform, true);
            step.transform.localScale = new Vector3(1f, stepRise / StepRise, 1f);
        }

        // Ramp from the lower deck to the upper one, level at both ends: a straight
        // ramp throws the body up when it runs onto the flat upper deck.
        var ramp = new GameObject("Ramp");
        ramp.transform.SetParent(root.transform, false);
        float length = Mathf.Clamp(rise * RampPerRise, run + RampMinLead, Mathf.Max(RampMaxLength, run + RampMinLead));
        for (int i = 0; i < RampSegments; i++)
            AddRampSegment(ramp.transform, RampPoint(i, run, length, rise), RampPoint(i + 1, run, length, rise));
        return root;
    }

    // Smoothstep from the lower deck to the upper one, ending at the joint.
    private static Vector3 RampPoint(int i, float run, float length, float rise)
    {
        float u = (float)i / RampSegments;
        return new Vector3(run - length + length * u, rise * u * u * (3f - 2f * u), 0f);
    }

    private static void AddRampSegment(Transform ramp, Vector3 from, Vector3 to)
    {
        var segment = new GameObject("Segment");
        segment.transform.SetParent(ramp, false);
        Vector3 chord = to - from;
        float slope = Mathf.Atan2(chord.y, chord.x);
        Vector3 down = new Vector3(Mathf.Sin(slope), -Mathf.Cos(slope), 0f) * (RampThickness * 0.5f);
        segment.transform.localPosition = (from + to) * 0.5f + down;
        segment.transform.localRotation = Quaternion.Euler(0f, 0f, slope * Mathf.Rad2Deg);
        // A little longer than the chord, so neighbours overlap and leave no seam to catch on.
        segment.AddComponent<BoxCollider>().size = new Vector3(chord.magnitude + 0.06f, RampThickness, WalkwayWidth);
    }
}
