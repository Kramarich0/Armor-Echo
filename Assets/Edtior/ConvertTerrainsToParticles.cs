using UnityEngine;
using UnityEditor;
using System.Linq;

public class FixAllTerrainsToBuiltIn
{
    [MenuItem("Tools/Fix Terrains → Assign TerrainLayer (Built-in)")]
    public static void FixTerrains()
    {
        if (!EditorUtility.DisplayDialog("Fix Terrains",
            "This will create TerrainLayer assets and assign them to all terrains in the open scenes. Make a backup if you want. Continue?", "Yes", "Cancel"))
            return;

        // Собираем все доступные текстуры в проекте
        string[] texGuids = AssetDatabase.FindAssets("t:Texture2D");
        if (texGuids == null || texGuids.Length == 0)
        {
            Debug.LogError("No textures found in project. Put at least one texture (grass/dirt) into Assets.");
            return;
        }

        // Список Texture2D ассетов
        var textures = texGuids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p))
            .Where(t => t != null)
            .ToArray();

        if (textures.Length == 0)
        {
            Debug.LogError("No valid Texture2D assets found.");
            return;
        }

        // Попытка найти подходящую текстуру по имени (grass, dirt, terrain, albedo, diffuse)
        Texture2D pickTex = textures.FirstOrDefault(t =>
        {
            var n = t.name.ToLower();
            return n.Contains("grass") || n.Contains("dirt") || n.Contains("terrain") || n.Contains("albedo") || n.Contains("diffuse");
        });

        if (pickTex == null)
            pickTex = textures[0]; // fallback — первая попавшаяся

        // Попытка найти нормаль карту по имени
        Texture2D pickNormal = textures.FirstOrDefault(t =>
        {
            var n = t.name.ToLower();
            return n.Contains("normal") || n.EndsWith("_n") || n.Contains("norm");
        });

        // Папка для создаваемых TerrainLayer
        string outFolder = "Assets/ConvertedTerrainLayers";
        if (!AssetDatabase.IsValidFolder(outFolder))
            AssetDatabase.CreateFolder("Assets", "ConvertedTerrainLayers");

        int terrainsFixed = 0;
        var terrains = Resources.FindObjectsOfTypeAll<Terrain>().Where(t => !EditorUtility.IsPersistent(t)).ToArray();

        foreach (var terrain in terrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;

            // Если у террейна уже есть слои с текстурами, и они заполнены — пропускаем
            var existing = terrain.terrainData.terrainLayers;
            bool hasValid = existing != null && existing.Length > 0 && existing.Any(l => l != null && l.diffuseTexture != null);
            if (hasValid)
            {
                Debug.Log($"Terrain '{terrain.name}' already has valid TerrainLayer(s). Skipping.");
                continue;
            }

            // Создаём один TerrainLayer с найденной текстурой
            TerrainLayer newLayer = new TerrainLayer();
            newLayer.diffuseTexture = pickTex;
            if (pickNormal != null) newLayer.normalMapTexture = pickNormal;
            newLayer.tileSize = new Vector2(10, 10);
            newLayer.tileOffset = Vector2.zero;

            string safeName = $"TL_{terrain.name}";
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{outFolder}/{safeName}.terrainlayer");
            AssetDatabase.CreateAsset(newLayer, assetPath);

            terrain.terrainData.terrainLayers = new TerrainLayer[] { newLayer };
            EditorUtility.SetDirty(terrain);
            terrainsFixed++;
            Debug.Log($"Assigned new TerrainLayer to terrain '{terrain.name}' (asset: {assetPath})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Fix Terrains", $"Done. Fixed {terrainsFixed} terrain(s).", "OK");
    }
}
