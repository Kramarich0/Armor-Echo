using UnityEngine;
using UnityEditor;
using System.Linq;

public class ForceFillTerrain
{
    [MenuItem("Tools/Terrain/Force Create & Fill Terrains (no UI)")]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog("Force Fill Terrains",
            "This will create TerrainData/TerrainLayer assets (if missing) and fully fill ALL terrains in open scenes with a texture found in the project.\n\nMake a backup if unsure. Continue?", "Yes", "Cancel"))
            return;

        // Найти текстуру в проекте (prefers grass/dirt/terrain/albedo/diffuse)
        string[] texGuids = AssetDatabase.FindAssets("t:Texture2D");
        Texture2D pickTex = null;
        if (texGuids != null && texGuids.Length > 0)
        {
            var textures = texGuids.Select(g => AssetDatabase.GUIDToAssetPath(g))
                                   .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p))
                                   .Where(t => t != null).ToArray();

            pickTex = textures.FirstOrDefault(t =>
            {
                var n = t.name.ToLower();
                return n.Contains("grass") || n.Contains("dirt") || n.Contains("terrain") || n.Contains("albedo") || n.Contains("diffuse");
            });

            if (pickTex == null && textures.Length > 0) pickTex = textures[0];
        }

        if (pickTex == null)
        {
            EditorUtility.DisplayDialog("No texture found",
                "No Texture2D assets found in the project. Add at least one texture and retry.", "OK");
            return;
        }

        // Папка для создаваемых TerrainLayer
        string outFolder = "Assets/AutoTerrainLayers";
        if (!AssetDatabase.IsValidFolder(outFolder))
            AssetDatabase.CreateFolder("Assets", "AutoTerrainLayers");

        var terrains = Resources.FindObjectsOfTypeAll<Terrain>().Where(t => !EditorUtility.IsPersistent(t)).ToArray();
        int fixedCount = 0;

        foreach (var terrain in terrains)
        {
            if (terrain == null) continue;

            // Ensure TerrainData exists
            TerrainData td = terrain.terrainData;
            if (td == null)
            {
                td = new TerrainData();
                td.heightmapResolution = 513;
                td.alphamapResolution = 512;
                td.baseMapResolution = 1024;
                td.size = new Vector3(500, 50, 500); // можно потом менять
                string tdPath = $"Assets/AutoTerrain_{terrain.name}_TerrainData.asset";
                tdPath = AssetDatabase.GenerateUniqueAssetPath(tdPath);
                AssetDatabase.CreateAsset(td, tdPath);
                terrain.terrainData = td;
                Debug.Log($"Created TerrainData for {terrain.name} at {tdPath}");
            }
            else
            {
                // Убедимся, что разрешения адекватны
                if (td.alphamapResolution < 16) td.alphamapResolution = 512;
                if (td.baseMapResolution < 256) td.baseMapResolution = 1024;
                if (td.heightmapResolution < 33) td.heightmapResolution = 513;
            }

            // Создаём TerrainLayer с найденной текстурой
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = pickTex;
            layer.normalMapTexture = null;
            layer.tileSize = new Vector2(50, 50);
            string layerPath = AssetDatabase.GenerateUniqueAssetPath($"{outFolder}/TL_{terrain.name}.terrainlayer");
            AssetDatabase.CreateAsset(layer, layerPath);
            AssetDatabase.SaveAssets();

            // Присвоить слой и залить
            terrain.terrainData.terrainLayers = new TerrainLayer[] { layer };

            int w = terrain.terrainData.alphamapResolution;
            int h = terrain.terrainData.alphamapResolution;
            int numLayers = terrain.terrainData.terrainLayers.Length;
            float[,,] alphas = new float[w, h, numLayers];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    for (int l = 0; l < numLayers; l++)
                        alphas[x, y, l] = (l == 0) ? 1f : 0f;

            terrain.terrainData.SetAlphamaps(0, 0, alphas);

            EditorUtility.SetDirty(terrain.terrainData);
            EditorUtility.SetDirty(terrain);
            fixedCount++;
            Debug.Log($"Filled terrain '{terrain.name}' with texture '{pickTex.name}' (layer asset: {layerPath})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"Processed {fixedCount} terrain(s).", "OK");
    }
}
