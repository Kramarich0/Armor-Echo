// using UnityEngine;
// using UnityEditor;
// using System.IO;

// public class ConvertAllMaterialsToParticles
// {
//     [MenuItem("Tools/Convert All Materials To Particles/Standard Unlit")]
//     public static void ConvertAll()
//     {
//         string[] materials = Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories);
//         int count = 0;

//         foreach (string matPath in materials)
//         {
//             Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
//             if (!mat) continue;

//             Shader particleShader = Shader.Find("Particles/Standard Unlit");
//             if (particleShader != null)
//             {
//                 mat.shader = particleShader;
//                 EditorUtility.SetDirty(mat);
//                 count++;
//             }
//             else
//             {
//                 Debug.LogWarning("Shader Particles/Standard Unlit not found!");
//             }
//         }

//         AssetDatabase.SaveAssets();
//         Debug.Log($"Converted {count} materials to Particles/Standard Unlit!");
//     }
// }
