using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Closes the corner where two placed walls meet at a right angle. The palisade
// lines of the two walls cross at one point: a post goes there, and a short arm
// (a piece of the same wall) runs from each wall's end to that post. Working with
// the lines, not with cells, covers every way the walls can touch on the grid.
// A corner has no health of its own: it stands while both of its walls stand.
public class WallCorners
{
    // How far past its end a wall may stop short of the crossing: two walls touching
    // only at a corner of their footprints leave almost the whole wall thickness.
    private const float ReachPastEnd = 2.05f;
    // How far the crossing may lie inside a wall and still count as its end: walls
    // side by side cross this deep, because a palisade runs inside its footprint.
    private const float ReachInsideEnd = 0.75f;
    // Below this the walls already touch and an arm would only hide inside them.
    private const float MinArmLength = 0.05f;
    private const float ArmMeshLength = 2f;
    // Walls further apart in height than this are not one line of defence.
    private const float MaxHeightStep = 1.5f;
    // Decks of the arms overlap the walls and each other at the same height: a small
    // drop for each keeps them from flickering.
    private const float DeckDrop = 0.015f;

    private class Corner
    {
        public WallSegment First;
        public WallSegment Second;
        public int FirstEnd;
        public int SecondEnd;
        public readonly List<GameObject> Parts = new List<GameObject>();

        public bool Uses(WallSegment wall) => wall == First || wall == Second;

        public bool Uses(WallSegment wall, int end) =>
            (wall == First && end == FirstEnd) || (wall == Second && end == SecondEnd);
    }

    [Inject] private GameConfig config;
    [Inject] private BuildingPlacer placer;

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
        Vector3 firstPosition = first.transform.position;
        Vector3 secondPosition = second.transform.position;
        if (Mathf.Abs(Vector3.Dot(first.Along, second.Along)) > 0.01f
            || Mathf.Abs(firstPosition.y - secondPosition.y) > MaxHeightStep)
            return;

        // The walls are perpendicular, so each one's offset along its own line to the
        // crossing is simply the other wall's position projected on that line.
        float firstOffset = Vector3.Dot(secondPosition - firstPosition, first.Along);
        float secondOffset = Vector3.Dot(firstPosition - secondPosition, second.Along);
        if (!IsAtEnd(firstOffset) || !IsAtEnd(secondOffset))
            return;

        int firstEnd = firstOffset > 0f ? 1 : -1;
        int secondEnd = secondOffset > 0f ? 1 : -1;
        if (IsEndTaken(first, firstEnd) || IsEndTaken(second, secondEnd))
            return;

        Vector3 crossing = firstPosition + first.Along * firstOffset;
        crossing.y = Mathf.Min(firstPosition.y, secondPosition.y);

        var corner = new Corner { First = first, Second = second, FirstEnd = firstEnd, SecondEnd = secondEnd };
        corner.Parts.Add(placer.Place(config.WallCornerPost, crossing, Quaternion.identity));
        AddArm(corner, first, firstEnd, Mathf.Abs(firstOffset), DeckDrop);
        AddArm(corner, second, secondEnd, Mathf.Abs(secondOffset), DeckDrop * 2f);
        corners.Add(corner);
    }

    private static bool IsAtEnd(float offset)
    {
        float pastEnd = Mathf.Abs(offset) - WallSegment.HalfLength;
        return pastEnd >= -ReachInsideEnd && pastEnd <= ReachPastEnd;
    }

    private bool IsEndTaken(WallSegment wall, int end)
    {
        foreach (Corner corner in corners)
            if (corner.Uses(wall, end))
                return true;
        return false;
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
