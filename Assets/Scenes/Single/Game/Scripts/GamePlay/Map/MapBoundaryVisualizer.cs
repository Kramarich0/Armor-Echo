

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MapBoundaryMesh : MonoBehaviour
{
    [Header("Settings")]
    public float heightOffset = 0.5f;
    public float lineWidth = 0.2f;
    public Color lineColor = Color.red;
    public int detailPerSide = 50;

    [Header("Terrain")]
    public Terrain terrain;

    [Header("Boundary Corners (unordered)")]
    public Transform[] cornerTransforms = new Transform[4];

    private MeshFilter mf;
    private MeshRenderer mr;
    private Mesh mesh;

    private void OnValidate() => Build();
    private void OnEnable() => Build();

    [ContextMenu("Rebuild Mesh")]
    public void Build()
    {
        if (mf == null) mf = GetComponent<MeshFilter>();
        if (mr == null) mr = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mf.sharedMesh = mesh;
        }

        if (terrain == null) terrain = Terrain.activeTerrain;

        if (cornerTransforms == null ||
            cornerTransforms.Length < 3 ||
            cornerTransforms.Any(t => t == null))
        {
            Debug.LogError("CornerTransforms заданы неверно.");
            return;
        }

        Vector3 center = Vector3.zero;
        foreach (var c in cornerTransforms)
            center += c.position;
        center /= cornerTransforms.Length;

        Vector3[] sortedCorners = cornerTransforms
            .OrderBy(t =>
                Mathf.Atan2(t.position.z - center.z, t.position.x - center.x))
            .Select(t => t.position)
            .ToArray();

        var contour = new List<Vector3>();

        for (int i = 0; i < sortedCorners.Length; i++)
        {
            Vector3 a = sortedCorners[i];
            Vector3 b = sortedCorners[(i + 1) % sortedCorners.Length];

            for (int k = 0; k <= detailPerSide; k++)
            {
                float t = k / (float)detailPerSide;
                contour.Add(Vector3.Lerp(a, b, t));
            }
        }

        int count = contour.Count;
        if (count < 2) return;

        var verts = new Vector3[count * 2];
        var uvs = new Vector2[count * 2];
        var tris = new int[count * 6];

        for (int i = 0; i < count; i++)
        {
            Vector3 p0 = contour[(i - 1 + count) % count];
            Vector3 p1 = contour[i];
            Vector3 p2 = contour[(i + 1) % count];

            Vector3 dir = (p2 - p0);
            dir.y = 0;
            dir.Normalize();

            Vector3 perp = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 left = p1 - perp * (lineWidth * 0.5f);
            Vector3 right = p1 + perp * (lineWidth * 0.5f);

            if (terrain != null)
            {
                left.y = terrain.SampleHeight(left) + heightOffset;
                right.y = terrain.SampleHeight(right) + heightOffset;
            }
            else
            {
                left.y = right.y = heightOffset;
            }

            verts[i * 2] = left;
            verts[i * 2 + 1] = right;

            float u = i / (float)count;
            uvs[i * 2] = new Vector2(0, u);
            uvs[i * 2 + 1] = new Vector2(1, u);
        }

        for (int i = 0; i < count; i++)
        {
            int i0 = i * 2;
            int i1 = ((i + 1) % count) * 2;

            tris[i * 6 + 0] = i0;
            tris[i * 6 + 1] = i1;
            tris[i * 6 + 2] = i0 + 1;

            tris[i * 6 + 3] = i0 + 1;
            tris[i * 6 + 4] = i1;
            tris[i * 6 + 5] = i1 + 1;
        }


        mesh.Clear();

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;

        Vector3[] norms = new Vector3[verts.Length];
        for (int i = 0; i < norms.Length; i++)
            norms[i] = Vector3.up;
        mesh.normals = norms;

        mesh.RecalculateBounds();

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit/Color") ??
                    Shader.Find("Unlit/Color");

        if (mr.sharedMaterial == null)
            mr.sharedMaterial = new Material(sh);
        mr.sharedMaterial.color = lineColor;
        mr.sharedMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

    }
}
