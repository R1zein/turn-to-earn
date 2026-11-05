using System.Collections.Generic;
using UnityEngine;

// Hides the parts of placed walls that other pieces replace: deck boards under
// stairs, a palisade standing across the way round an outer corner. A cut is a set
// of boxes in the frame of whoever asks for it; a triangle of the wall goes when all
// three of its vertices lie in one box, so whole boards and logs go and long joists
// and rails stay. Works on copies of the wall's mesh, so the asset and the other
// walls stay whole; a cut that frees the way also leaves the wall's MeshCollider.
// Every owner's cuts on a wall are applied together, so stairs and a corner on the
// same wall do not undo each other.
public class WallMeshCuts
{
    private class Cut
    {
        public object Owner;
        public Transform Frame;
        public Bounds[] Boxes;
        public bool FreesWay;
    }

    private class WallCuts
    {
        public Mesh Original;
        public Mesh Shown;
        public Mesh Solid;
        public readonly List<Cut> Cuts = new List<Cut>();
    }

    private readonly Dictionary<WallSegment, WallCuts> walls = new Dictionary<WallSegment, WallCuts>();

    public void Add(object owner, WallSegment wall, Transform frame, IReadOnlyList<Bounds> boxes, bool freesWay)
    {
        if (!walls.TryGetValue(wall, out WallCuts cuts))
        {
            cuts = new WallCuts { Original = wall.GetComponent<MeshFilter>().sharedMesh };
            walls.Add(wall, cuts);
        }
        cuts.Cuts.Add(new Cut { Owner = owner, Frame = frame, Boxes = new List<Bounds>(boxes).ToArray(), FreesWay = freesWay });
        Rebuild(wall, cuts);
    }

    public void Remove(object owner)
    {
        foreach (KeyValuePair<WallSegment, WallCuts> pair in new List<KeyValuePair<WallSegment, WallCuts>>(walls))
        {
            if (pair.Value.Cuts.RemoveAll(cut => cut.Owner == owner) > 0)
                Rebuild(pair.Key, pair.Value);
        }
    }

    // The wall itself is going: drop its copies.
    public void Forget(WallSegment wall)
    {
        if (!walls.TryGetValue(wall, out WallCuts cuts))
            return;

        DestroyCopies(cuts);
        walls.Remove(wall);
    }

    private void Rebuild(WallSegment wall, WallCuts cuts)
    {
        DestroyCopies(cuts);
        var filter = wall.GetComponent<MeshFilter>();
        var collider = wall.GetComponent<MeshCollider>();
        if (cuts.Cuts.Count == 0)
        {
            filter.sharedMesh = cuts.Original;
            collider.sharedMesh = cuts.Original;
            walls.Remove(wall);
            return;
        }

        cuts.Shown = Without(cuts.Original, wall.transform, cuts.Cuts, false);
        filter.sharedMesh = cuts.Shown;
        bool freesWay = cuts.Cuts.Exists(cut => cut.FreesWay);
        cuts.Solid = freesWay ? Without(cuts.Original, wall.transform, cuts.Cuts, true) : null;
        collider.sharedMesh = freesWay ? cuts.Solid : cuts.Original;
    }

    private static void DestroyCopies(WallCuts cuts)
    {
        if (cuts.Shown != null)
            Object.Destroy(cuts.Shown);
        if (cuts.Solid != null)
            Object.Destroy(cuts.Solid);
        cuts.Shown = null;
        cuts.Solid = null;
    }

    private static Mesh Without(Mesh original, Transform wall, List<Cut> cuts, bool onlyFreeingWay)
    {
        Vector3[] vertices = original.vertices;
        var hidden = new bool[vertices.Length];
        foreach (Cut cut in cuts)
        {
            if (onlyFreeingWay && !cut.FreesWay)
                continue;

            Matrix4x4 toFrame = cut.Frame.worldToLocalMatrix * wall.localToWorldMatrix;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (hidden[i])
                    continue;
                Vector3 point = toFrame.MultiplyPoint3x4(vertices[i]);
                foreach (Bounds box in cut.Boxes)
                {
                    if (box.Contains(point))
                    {
                        hidden[i] = true;
                        break;
                    }
                }
            }
        }

        Mesh mesh = Object.Instantiate(original);
        mesh.name = original.name + " (cut)";
        for (int sub = 0; sub < original.subMeshCount; sub++)
        {
            int[] triangles = original.GetTriangles(sub);
            var kept = new List<int>(triangles.Length);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (hidden[triangles[i]] && hidden[triangles[i + 1]] && hidden[triangles[i + 2]])
                    continue;
                kept.Add(triangles[i]);
                kept.Add(triangles[i + 1]);
                kept.Add(triangles[i + 2]);
            }
            mesh.SetTriangles(kept, sub);
        }
        return mesh;
    }
}
