using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

// A place on the edge of a building's footprint where another building can join:
// cells of the two buildings touch and the sockets face each other. `cell` and
// `outward` are in the prefab's frame, x along its X, y along its Z. The objects
// are what the building shows on that side (a tower's rail): `closed` while nothing
// joins, `openLeft` / `openRight` when a walkway joins and lies to the left / right
// of someone looking outward. Left empty on a building that does not change.
[Serializable]
public struct GridSocket
{
    public Vector2Int cell;
    public Vector2Int outward;
    public GameObject closed;
    public GameObject openLeft;
    public GameObject openRight;
}

// A building that stands on the build grid. Knows its size in cells - how much it
// BLOCKS, which need not match its mesh: neighbours are meant to overlap a little
// so the parts of a wall line join up. Once placed it holds its cells until
// destroyed. Sockets say where others join it: BuildService snaps new buildings to
// them, and a joined socket opens the side it is on. BuildService also tints a
// building while a new one is being aimed into it.
public class GridBuilding : MonoBehaviour
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    // Multiplies the texture, so the building stays recognisable, only reddish.
    private static readonly Color ConflictTint = new Color(1f, 0.28f, 0.22f);

    [Inject] private BuildGrid grid;

    [Tooltip("Size in cells: x along the prefab's X, y along its Z")]
    [SerializeField] private Vector2Int size = Vector2Int.one;
    [Tooltip("Centre of those cells in the prefab's own frame (X, Z), in metres: meshes are rarely centred on their pivot")]
    [SerializeField] private Vector2 footprintCentre;
    [SerializeField] private GridSocket[] sockets = new GridSocket[0];
    [Tooltip("Has a walkway on its +Z side, which a tower opens up to")]
    [SerializeField] private bool hasWalkway;
    [Tooltip("Cells outside the footprint it also blocks (the foot of a wall's ladder), in the same frame as the sockets' cells")]
    [SerializeField] private Vector2Int[] extraCells = new Vector2Int[0];

    private readonly List<Vector2Int> occupied = new List<Vector2Int>();
    private GridFootprint footprint;
    private int quarterTurns;
    private bool isClaimed;

    // Placed by the player, not part of the level.
    public bool IsClaimed => isClaimed;

    // What it cost, to pay back when dismantled.
    public AllResources Price { get; private set; } = new AllResources();

    public IReadOnlyList<GridSocket> Sockets => sockets;

    // The rectangle it stands on, once placed. Extra cells are not part of it.
    public GridFootprint Footprint => footprint;

    public int QuarterTurns => quarterTurns;

    // World height of the top of the mesh, once placed.
    public float Top { get; private set; }

    // Size on the grid after turning by this many quarter turns.
    public Vector2Int SizeTurned(int quarterTurns) =>
        quarterTurns % 2 == 0 ? size : new Vector2Int(size.y, size.x);

    // Every cell it blocks when standing on `cells` turned by `turns`: the footprint and the extra cells.
    public IEnumerable<Vector2Int> CellsAt(GridFootprint cells, int turns)
    {
        foreach (Vector2Int cell in cells.Cells)
            yield return cell;
        foreach (Vector2Int extra in extraCells)
            yield return cells.Min + TurnedCell(extra, turns);
    }

    // Where the prefab's pivot goes for its cells to be centred on `centre`.
    public Vector3 PivotFor(Vector3 centre, Quaternion rotation) =>
        centre - rotation * new Vector3(footprintCentre.x, 0f, footprintCentre.y);

    // A cell in the prefab's frame (a socket's, an extra one), as an offset from the
    // footprint's corner, after quarter turns. Cells outside the footprint turn with it.
    public Vector2Int TurnedCell(Vector2Int c, int turns)
    {
        switch (turns)
        {
            case 1: return new Vector2Int(c.y, size.x - 1 - c.x);
            case 2: return new Vector2Int(size.x - 1 - c.x, size.y - 1 - c.y);
            case 3: return new Vector2Int(size.y - 1 - c.y, c.x);
            default: return c;
        }
    }

    public Vector2Int TurnedCell(GridSocket socket, int turns) => TurnedCell(socket.cell, turns);

    public static Vector2Int TurnedOutward(GridSocket socket, int turns)
    {
        Vector2Int o = socket.outward;
        switch (turns)
        {
            case 1: return new Vector2Int(o.y, -o.x);
            case 2: return new Vector2Int(-o.x, -o.y);
            case 3: return new Vector2Int(-o.y, o.x);
            default: return o;
        }
    }

    // Where the sockets of a placed building are on the grid.
    public Vector2Int SocketCell(GridSocket socket) => footprint.Min + TurnedCell(socket, quarterTurns);

    public Vector2Int SocketOutward(GridSocket socket) => TurnedOutward(socket, quarterTurns);

    // The building whose socket faces this one, if any.
    public GridBuilding JoinedAt(GridSocket socket)
    {
        Vector2Int outward = SocketOutward(socket);
        Vector2Int next = SocketCell(socket) + outward;
        GridBuilding other = grid.At(next);
        return other != null && other.HasSocketAt(next, -outward) ? other : null;
    }

    private bool HasSocketAt(Vector2Int cell, Vector2Int outward)
    {
        foreach (GridSocket socket in sockets)
            if (SocketCell(socket) == cell && SocketOutward(socket) == outward)
                return true;
        return false;
    }

    // Shows on each side what suits what has joined there.
    public void RefreshSockets()
    {
        foreach (GridSocket socket in sockets)
        {
            if (socket.closed == null)
                continue;

            GridBuilding joined = JoinedAt(socket);
            bool isOpen = joined != null && joined.hasWalkway;
            Vector2Int outward = SocketOutward(socket);
            var right = new Vector3(outward.y, 0f, -outward.x);
            bool isWalkwayOnRight = isOpen && Vector3.Dot(joined.transform.forward, right) > 0f;

            socket.closed.SetActive(!isOpen);
            socket.openLeft.SetActive(isOpen && !isWalkwayOnRight);
            socket.openRight.SetActive(isWalkwayOnRight);
        }
    }

    public void Claim(GridFootprint cells, int turns, AllResources price)
    {
        footprint = cells;
        quarterTurns = turns;
        Price = price;
        isClaimed = true;
        Top = transform.position.y;
        foreach (Renderer part in GetComponentsInChildren<Renderer>())
            if (!(part is ParticleSystemRenderer))
                Top = Mathf.Max(Top, part.bounds.max.y);
        occupied.Clear();
        occupied.AddRange(CellsAt(cells, turns));
        grid.Occupy(this, occupied);
    }

    // Gives its cells back now rather than when destroyed at the end of the frame:
    // a structure upgraded in its place takes them at once.
    public void Leave()
    {
        if (!isClaimed)
            return;

        isClaimed = false;
        grid.Release(this, occupied);
    }

    private void OnDisable() => Leave();

    public void SetConflict(bool isInConflict)
    {
        var block = new MaterialPropertyBlock();
        foreach (Renderer part in GetComponentsInChildren<Renderer>())
        {
            if (part is ParticleSystemRenderer)
                continue;

            for (int i = 0; i < part.sharedMaterials.Length; i++)
            {
                Material material = part.sharedMaterials[i];
                if (material == null || !material.HasProperty(BaseColor))
                    continue;

                if (isInConflict)
                {
                    block.SetColor(BaseColor, material.GetColor(BaseColor) * ConflictTint);
                    part.SetPropertyBlock(block, i);
                }
                else
                {
                    part.SetPropertyBlock(null, i);
                }
            }
        }
    }
}
