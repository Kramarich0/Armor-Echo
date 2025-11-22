using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class MapBoundaryVisualizer : MonoBehaviour
{
    [Header("Walls")]
    public string wallTag = "Wall";
    public float lineHeight = 0.5f;
    public float lineWidth = 0.1f;
    public Color lineColor = Color.red;
    public float padding = 0f;

    [Header("Glow Settings")]
    public float glowHeightOffset = 0.05f;
    public float glowWidthMultiplier = 4f;
    public float glowAlpha = 0.8f;

    private LineRenderer lrMain;
    private LineRenderer lrGlow;

    private Material mainMaterial;
    private Material glowMaterial;

    void OnEnable() => BuildBoundary();
    void OnValidate() => BuildBoundary();

    void OnDisable()
    {
        CleanupMaterials();
    }

    void OnDestroy()
    {
        CleanupMaterials();
        if (lrGlow != null)
        {
            if (Application.isPlaying)
                Destroy(lrGlow.gameObject);
            else
                DestroyImmediate(lrGlow.gameObject);
        }
    }

    private void CleanupMaterials()
    {
        if (mainMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(mainMaterial);
            else
                DestroyImmediate(mainMaterial);
        }

        if (glowMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(glowMaterial);
            else
                DestroyImmediate(glowMaterial);
        }
    }

    [ContextMenu("Rebuild Boundary")]
    public void BuildBoundary()
    {
        if (!lrMain) lrMain = GetComponent<LineRenderer>();
        if (!lrMain) return;

        lrMain.loop = true;
        lrMain.widthMultiplier = lineWidth;

        // Create or reuse main material
        if (mainMaterial == null)
        {
            mainMaterial = new Material(Shader.Find("Unlit/Color"));
        }
        mainMaterial.color = lineColor;
        lrMain.sharedMaterial = mainMaterial; // Use sharedMaterial with your own instance

        if (!lrGlow)
        {
            GameObject glowObj = new GameObject("GlowLine");
            glowObj.transform.SetParent(transform, false);
            lrGlow = glowObj.AddComponent<LineRenderer>();
        }

        lrGlow.loop = true;
        lrGlow.widthMultiplier = lineWidth * glowWidthMultiplier;
        lrGlow.useWorldSpace = true;

        // Create or reuse glow material
        if (glowMaterial == null)
        {
            glowMaterial = new Material(Shader.Find("Unlit/Transparent"));
        }
        glowMaterial.color = lineColor;
        lrGlow.sharedMaterial = glowMaterial;

        GameObject[] walls = GameObject.FindGameObjectsWithTag(wallTag);
        if (walls.Length == 0)
        {
            lrMain.positionCount = 0;
            lrGlow.positionCount = 0;
            return;
        }

        List<Vector3> allCorners = new List<Vector3>();
        foreach (var w in walls)
        {
            BoxCollider bc = w.GetComponent<BoxCollider>();
            if (!bc) continue;

            Vector3 halfSize = Vector3.Scale(bc.size * 0.5f, w.transform.lossyScale);
            Vector3 center = bc.center;

            Vector3[] corners = new Vector3[4]
            {
                center + new Vector3(-halfSize.x, 0, -halfSize.z),
                center + new Vector3(-halfSize.x, 0, halfSize.z),
                center + new Vector3(halfSize.x, 0, halfSize.z),
                center + new Vector3(halfSize.x, 0, -halfSize.z)
            };

            for (int i = 0; i < 4; i++)
                allCorners.Add(w.transform.TransformPoint(corners[i]));
        }

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var c in allCorners)
        {
            minX = Mathf.Min(minX, c.x);
            maxX = Mathf.Max(maxX, c.x);
            minZ = Mathf.Min(minZ, c.z);
            maxZ = Mathf.Max(maxZ, c.z);
        }

        minX -= padding; maxX += padding;
        minZ -= padding; maxZ += padding;

        Vector3[] verts = new Vector3[4]
        {
            new Vector3(minX, lineHeight, minZ),
            new Vector3(minX, lineHeight, maxZ),
            new Vector3(maxX, lineHeight, maxZ),
            new Vector3(maxX, lineHeight, minZ)
        };

        lrMain.positionCount = verts.Length;
        lrMain.SetPositions(verts);

        Vector3[] glowVerts = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; i++)
            glowVerts[i] = verts[i] + Vector3.up * glowHeightOffset;

        lrGlow.positionCount = glowVerts.Length;
        lrGlow.SetPositions(glowVerts);

        // Update glow alpha
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(lineColor, 0f),
                new GradientColorKey(lineColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(glowAlpha, 0f),
                new GradientAlphaKey(glowAlpha, 1f)
            }
        );
        lrGlow.colorGradient = g;

        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 0f);
        widthCurve.AddKey(0.05f, 1f);
        widthCurve.AddKey(0.95f, 1f);
        widthCurve.AddKey(1f, 0f);
        lrGlow.widthCurve = widthCurve;
    }
}