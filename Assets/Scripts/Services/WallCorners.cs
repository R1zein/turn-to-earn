using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Closes the corner of two placed walls that meet at a right angle the one right
// way: the 2 x 2 cells at the end of one wall (the butting wall) lie next to the
// 2 x 2 cells at the end of the other (the through wall), sharing a whole edge.
// Touching at a point, by one cell, or anywhere but the ends is a wrong placement:
// no corner, the walls stand as they are and their walkways do not join.
// The corner is inner when both walkways face into the L and outer when both face
// out; walkways facing different ways make none. The walls may differ in height by
// one BuildHeightStep at most, and then a model that climbs between them is used.
// A corner model is made in the through wall's frame at its +X end and mirrored for
// the -X end. Until GameConfig has one, a post and an arm stand in for it.
// A corner has no health of its own: it stands while both of its walls stand.
public class WallCorners
{
    private const int EndCells = 2;
    private const float HeightTolerance = 0.05f;
    // Below this the walls already touch and an arm would only hide inside them.
    private const float MinArmLength = 0.05f;
    private const float ArmMeshLength = 2f;
    // Decks of the arms overlap the walls and each other at the same height: a small
    // drop for each keeps them from flickering.
    private const float DeckDrop = 0.015f;

    private class Corner
    {
        public WallSegment Through;
        public WallSegment Butting;
        public int ThroughEnd;
        public int ButtingEnd;
        public bool IsInner;
        public readonly List<GameObject> Parts = new List<GameObject>();

        public bool Uses(WallSegment wall) => wall == Through || wall == Butting;

        public bool Uses(WallSegment wall, int end) =>
            (wall == Through && end == ThroughEnd) || (wall == Butting && end == ButtingEnd);
    }

    [Inject] private GameConfig config;
    [Inject] private BuildingPlacer placer;
    [Inject] private BuildGrid grid;

    private readonly List<WallSegment> walls = new List<WallSegment>();
    private readonly List<Corner> corners = new List<Corner>();

    public void Add(WallSegment wall)
    {
        foreach (WallSegment other in walls)
            TryJoin(wall, other);
        walls.Add(wall);
    }

    public void Remove(WallSegment wall)
    {
        walls.Remove(wall);
        for (int i = corners.Count - 1; i >= 0; i--)
        {
            if (!corners[i].Uses(wall))
                continue;

            foreach (GameObject part in corners[i].Parts)
                Object.Destroy(part);
            corners.RemoveAt(i);
        }
    }

    private void TryJoin(WallSegment first, WallSegment second)
    {
        Corner corner = Match(first, second);
        if (corner == null || IsEndTaken(corner.Through, corner.ThroughEnd) || IsEndTaken(corner.Butting, corner.ButtingEnd))
            return;

        float step = config.BuildHeightStep;
        float rise = corner.Butting.transform.position.y - corner.Through.transform.position.y;
        int steps = Mathf.RoundToInt(rise / step);
        if (Mathf.Abs(steps) > 1 || Mathf.Abs(rise - steps * step) > HeightTolerance)
            return;

        GameObject model = Model(corner.IsInner, steps);
        if (model != null)
        {
            Transform body = corner.Through.transform;
            Vector3 end = body.TransformPoint(new Vector3(corner.ThroughEnd * WallSegment.HalfLength, 0f, 0f));
            GameObject part = placer.Place(model, end, body.rotation);
            part.transform.localScale = new Vector3(corner.ThroughEnd, 1f, 1f);
            corner.Parts.Add(part);
        }
        else
        {
            AddPostAndArms(corner);
        }
        corners.Add(corner);
    }

    // The corner the two walls make, if they touch the right way; its parts not built yet.
    private Corner Match(WallSegment first, WallSegment second)
    {
        Vector2Int firstAxis = Axis(first.Cells);
        if (firstAxis == Axis(second.Cells))
            return null;

        for (int firstEnd = -1; firstEnd <= 1; firstEnd += 2)
        {
            for (int secondEnd = -1; secondEnd <= 1; secondEnd += 2)
            {
                Vector2Int step = EndBlock(second, secondEnd) - EndBlock(first, firstEnd);
                bool isEdge = (step.x == 0) != (step.y == 0) && Mathf.Abs(step.x + step.y) == EndCells;
                if (!isEdge)
                    continue;

                // The step from block to block runs along the butting wall and across the
                // through wall: one wall's end face against the other's side.
                bool firstButts = new Vector2Int(Mathf.Abs(step.x), Mathf.Abs(step.y)) == firstAxis * EndCells;
                var corner = new Corner
                {
                    Through = firstButts ? second : first,
                    Butting = firstButts ? first : second,
                    ThroughEnd = firstButts ? secondEnd : firstEnd,
                    ButtingEnd = firstButts ? firstEnd : secondEnd,
                };
                Vector3 towardsThrough = new Vector3(step.x, 0f, step.y).normalized * (firstButts ? 1f : -1f);
                Vector3 towardsThroughBody = corner.Through.Along * -corner.ThroughEnd;

                // Inner: the butting wall is on the through wall's walkway side and its own
                // walkway faces along the through wall. Outer: both the other way round.
                float side = Vector3.Dot(corner.Through.transform.forward, towardsThrough);
                float facing = Vector3.Dot(corner.Butting.transform.forward, towardsThroughBody);
                if (side < -0.5f && facing > 0.5f)
                    corner.IsInner = true;
                else if (!(side > 0.5f && facing < -0.5f))
                    return null;
                return corner;
            }
        }
        return null;
    }

    private static Vector2Int Axis(GridFootprint cells) =>
        cells.Size.x > cells.Size.y ? Vector2Int.right : Vector2Int.up;

    // The corner cell of the 2 x 2 block at the wall's -1 or +1 end (along its X).
    private Vector2Int EndBlock(WallSegment wall, int end)
    {
        GridFootprint cells = wall.Cells;
        Vector2Int axis = Axis(cells);
        Vector2Int last = cells.Min + axis * (Mathf.Max(cells.Size.x, cells.Size.y) - EndCells);
        Vector3 lastCentre = grid.Centre(new GridFootprint(last, Vector2Int.one * EndCells));
        bool lastIsPlusEnd = Vector3.Dot(lastCentre - wall.transform.position, wall.Along) > 0f;
        return (end > 0) == lastIsPlusEnd ? last : cells.Min;
    }

    private GameObject Model(bool isInner, int steps)
    {
        if (isInner)
            return steps == 0 ? config.WallCornerInner : steps < 0 ? config.WallCornerInnerButtLower : config.WallCornerInnerButtHigher;
        return steps == 0 ? config.WallCornerOuter : steps < 0 ? config.WallCornerOuterButtLower : config.WallCornerOuterButtHigher;
    }

    private bool IsEndTaken(WallSegment wall, int end)
    {
        foreach (Corner corner in corners)
            if (corner.Uses(wall, end))
                return true;
        return false;
    }

    // Stand-in until the corner models exist: a post where the palisade lines cross
    // and a piece of wall from each wall's end to it.
    private void AddPostAndArms(Corner corner)
    {
        WallSegment first = corner.Through;
        WallSegment second = corner.Butting;
        Vector3 firstPosition = first.transform.position;
        Vector3 secondPosition = second.transform.position;

        // The walls are perpendicular, so each one's offset along its own line to the
        // crossing is simply the other wall's position projected on that line.
        float firstOffset = Vector3.Dot(secondPosition - firstPosition, first.Along);
        float secondOffset = Vector3.Dot(firstPosition - secondPosition, second.Along);
        Vector3 crossing = firstPosition + first.Along * firstOffset;
        crossing.y = Mathf.Min(firstPosition.y, secondPosition.y);

        corner.Parts.Add(placer.Place(config.WallCornerPost, crossing, Quaternion.identity));
        AddArm(corner, first, corner.ThroughEnd, Mathf.Abs(firstOffset), DeckDrop);
        AddArm(corner, second, corner.ButtingEnd, Mathf.Abs(secondOffset), DeckDrop * 2f);
    }

    private void AddArm(Corner corner, WallSegment wall, int end, float offset, float drop)
    {
        float length = offset - WallSegment.HalfLength;
        if (length < MinArmLength)
            return;

        Transform body = wall.transform;
        Vector3 position = body.position + wall.Along * (end * WallSegment.HalfLength) + Vector3.down * drop;
        GameObject arm = placer.Place(end > 0 ? config.WallArmRight : config.WallArmLeft, position, body.rotation);
        arm.transform.localScale = new Vector3(length / ArmMeshLength, 1f, 1f);
        corner.Parts.Add(arm);
    }
}
