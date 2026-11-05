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

    private GridFootprint footprint;
    private int quarterTurns;
    private bool isClaimed;

    // Placed by the player, not part of the level.
    public bool IsClaimed => isClaimed;

    // What it cost, to pay back when dismantled.
    public AllResources Price { get; private set; } = new AllResources();

    public IReadOnlyList<GridSocket> Sockets => sockets;

    // The cells it stands on, once placed.
    public GridFootprint Footprint => footprint;

    // World height of the top of the mesh, once placed.
    public float Top { get; private set; }

    // Size on the grid after turning by this many quarter turns.
    public Vector2Int SizeTurned(int quarterTurns) =>
        quarterTurns % 2 == 0 ? size : new Vector2Int(size.y, size.x);

    // Where the prefab's pivot goes for its cells to be centred on `centre`.
    public Vector3 PivotFor(Vector3 centre, Quaternion rotation) =>
        centre - rotation * new Vector3(footprintCentre.x, 0f, footprintCentre.y);

    // A socket's cell, as an offset from the footprint's corner, after quarter turns.
    public Vector2Int TurnedCell(GridSocket socket, int turns)
    {
        Vector2Int c = socket.cell;
        switch (turns)
        {
            case 1: return new Vector2Int(c.y, size.x - 1 - c.x);
            case 2: return new Vector2Int(size.x - 1 - c.x, size.y - 1 - c.y);
            case 3: return new Vector2Int(size.y - 1 - c.y, c.x);
            default: return c;
        }
    }

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
        grid.Occupy(this, cells);
    }

    private void OnDestroy()
    {
        if (isClaimed)
            grid.Release(this, footprint);
    }

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
