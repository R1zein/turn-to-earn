using UnityEngine;
using Zenject;

// A building that stands on the build grid. Knows its size in cells - how much it
// BLOCKS, which need not match its mesh: neighbours are meant to overlap a little
// so the parts of a wall line join up. Once placed it holds its cells until
// destroyed. BuildService tints it while a new building is being aimed into it.
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

    private GridFootprint footprint;
    private bool isClaimed;

    // World height of the top of the mesh, once placed.
    public float Top { get; private set; }

    // Size on the grid after turning by this many quarter turns.
    public Vector2Int SizeTurned(int quarterTurns) =>
        quarterTurns % 2 == 0 ? size : new Vector2Int(size.y, size.x);

    // Where the prefab's pivot goes for its cells to be centred on `centre`.
    public Vector3 PivotFor(Vector3 centre, Quaternion rotation) =>
        centre - rotation * new Vector3(footprintCentre.x, 0f, footprintCentre.y);

    public void Claim(GridFootprint cells)
    {
        footprint = cells;
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
