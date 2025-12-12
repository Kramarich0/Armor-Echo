// // Assets/Editor/SmartPlatePlacer.cs
// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;
// using System.Linq;

// public class SmartPlatePlacer : EditorWindow
// {
//     public GameObject tankRoot;
//     public GameObject platePrefab; // префаб должен содержать ArmorPlate и BoxCollider (size 1,1,1)
//     public float sampleRadius = 0.5f; // радиус поиска вершин (м)
//     public float thickness = 0.08f;   // толщина плитки (м)
//     public LayerMask hitLayer = ~0;
//     public bool parentToHitCollider = true;

//     [MenuItem("Tools/Armor/Smart Plate Placer")]
//     public static void Open() => GetWindow<SmartPlatePlacer>("Smart Plate Placer");

//     void OnGUI()
//     {
//         GUILayout.Label("Smart Plate Placer", EditorStyles.boldLabel);
//         tankRoot = (GameObject)EditorGUILayout.ObjectField("Tank Root (opt)", tankRoot, typeof(GameObject), true);
//         platePrefab = (GameObject)EditorGUILayout.ObjectField("Plate Prefab", platePrefab, typeof(GameObject), false);
//         sampleRadius = EditorGUILayout.FloatField("Sample Radius (m)", sampleRadius);
//         thickness = EditorGUILayout.FloatField("Thickness (m)", thickness);
//         hitLayer = LayerMaskField("Hit Layer", hitLayer);
//         parentToHitCollider = EditorGUILayout.Toggle("Parent to hit collider", parentToHitCollider);
//         GUILayout.Space(6);
//         EditorGUILayout.HelpBox("ЛКМ в SceneView по модели → создаёт плиту, подгоняет размер и нормаль по локальной области вершин.", MessageType.Info);
//     }

//     static LayerMask LayerMaskField(string label, LayerMask mask)
//     {
//         var layers = Enumerable.Range(0, 32).Select(i => LayerMask.LayerToName(i)).ToArray();
//         int maskVal = 0;
//         for (int i = 0; i < 32; i++) if (((1 << i) & mask) != 0) maskVal |= (1 << i);
//         maskVal = EditorGUILayout.MaskField(label, maskVal, layers);
//         LayerMask newMask = 0;
//         for (int i = 0; i < 32; i++) if ((maskVal & (1 << i)) != 0) newMask |= (1 << i);
//         return newMask;
//     }

//     void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
//     void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

//     void OnSceneGUI(SceneView sv)
//     {
//         Event e = Event.current;
//         if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
//         {
//             Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
//             if (Physics.Raycast(ray, out RaycastHit hit, 100f, hitLayer))
//             {
//                 CreateSmartPlate(hit);
//                 e.Use();
//             }
//         }
//     }

//     void CreateSmartPlate(RaycastHit hit)
//     {
//         if (platePrefab == null)
//         {
//             Debug.LogWarning("Укажи platePrefab с ArmorPlate и BoxCollider.");
//             return;
//         }

//         // Собираем вершины рядом с точкой попадания
//         MeshFilter mf = hit.collider.GetComponent<MeshFilter>();
//         if (mf == null || mf.sharedMesh == null)
//         {
//             Debug.LogWarning("Collider не имеет MeshFilter/mesh — используется простая плита.");
//             CreateSimpleAtHit(hit);
//             return;
//         }

//         Mesh mesh = mf.sharedMesh;
//         Vector3[] verts = mesh.vertices;
//         Transform tr = mf.transform;

//         // собираем мировые вершины в радиусе
//         List<Vector3> nearby = new List<Vector3>(32);
//         for (int i = 0; i < verts.Length; i++)
//         {
//             Vector3 wv = tr.TransformPoint(verts[i]);
//             if ((wv - hit.point).sqrMagnitude <= sampleRadius * sampleRadius)
//                 nearby.Add(wv);
//         }

//         if (nearby.Count == 0)
//         {
//             // fallback: используем hit.point и hit.normal
//             CreateSimpleAtHit(hit);
//             return;
//         }

//         // средняя нормаль: усредняем нормали вершин, если есть
//         Vector3 avgNormal = Vector3.zero;
//         if (mesh.normals != null && mesh.normals.Length == verts.Length)
//         {
//             for (int i = 0; i < verts.Length; i++)
//             {
//                 Vector3 wv = tr.TransformPoint(verts[i]);
//                 if ((wv - hit.point).sqrMagnitude <= sampleRadius * sampleRadius)
//                     avgNormal += tr.TransformDirection(mesh.normals[i]);
//             }
//             if (avgNormal.sqrMagnitude < 1e-6f) avgNormal = hit.normal;
//             avgNormal.Normalize();
//         }
//         else
//         {
//             avgNormal = hit.normal;
//         }

//         // Переход в локальную плоскость плиты
//         Quaternion plateRot = Quaternion.LookRotation(avgNormal);
//         Vector3 center = Vector3.zero;
//         foreach (var p in nearby) center += p;
//         center /= nearby.Count;

//         // Преобразуем все точки в локальное пространство плиты
//         Quaternion inv = Quaternion.Inverse(plateRot);
//         Vector3 min = inv * (nearby[0] - center);
//         Vector3 max = min;
//         for (int i = 1; i < nearby.Count; i++)
//         {
//             Vector3 lp = inv * (nearby[i] - center);
//             min = Vector3.Min(min, lp);
//             max = Vector3.Max(max, lp);
//         }

//         // Определяем размер X/Y (с запасом) и Z=thickness
//         Vector3 size = max - min;
//         size.x = Mathf.Max(0.05f, size.x + 0.02f);
//         size.y = Mathf.Max(0.05f, size.y + 0.02f);
//         size.z = Mathf.Max(0.005f, thickness);

//         // Создаём плиту из префаба
//         GameObject plate = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
//         Undo.RegisterCreatedObjectUndo(plate, "Create Smart Plate");
//         plate.transform.position = center;
//         plate.transform.rotation = plateRot;
//         plate.transform.localScale = Vector3.one;

//         // Настраиваем BoxCollider (если есть)
//         BoxCollider bc = plate.GetComponent<BoxCollider>();
//         if (bc != null)
//         {
//             bc.center = (min + max) * 0.5f;
//             bc.size = size;
//         }
//         else
//         {
//             // если нет, добавим
//             bc = plate.AddComponent<BoxCollider>();
//             bc.center = (min + max) * 0.5f;
//             bc.size = size;
//         }

//         // ArmorPlate настройка
//         ArmorPlate ap = plate.GetComponent<ArmorPlate>();
//         if (ap == null) ap = plate.AddComponent<ArmorPlate>();
//         ap.thickness = thickness * 1000f; // если твои единицы мм, поправь; иначе используй m
//         ap.armorNormal = plate.transform.forward;
//         ap.armorType = ArmorType.RHA;

//         // Родитель
//         if (parentToHitCollider && hit.collider != null)
//             plate.transform.SetParent(hit.collider.transform, true);
//         else if (tankRoot != null)
//             plate.transform.SetParent(tankRoot.transform, true);

//         Debug.Log($"Smart plate created. Nearby verts: {nearby.Count}. Size: {size}");
//     }

//     void CreateSimpleAtHit(RaycastHit hit)
//     {
//         GameObject plate = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
//         Undo.RegisterCreatedObjectUndo(plate, "Create Simple Plate");
//         plate.transform.position = hit.point;
//         plate.transform.rotation = Quaternion.LookRotation(hit.normal);
//         plate.transform.localScale = new Vector3(0.5f, 0.5f, thickness);
//         ArmorPlate ap = plate.GetComponent<ArmorPlate>() ?? plate.AddComponent<ArmorPlate>();
//         ap.thickness = thickness * 1000f;
//         ap.armorType = ArmorType.RHA;
//         ap.armorNormal = plate.transform.forward;
//         if (parentToHitCollider && hit.collider != null)
//             plate.transform.SetParent(hit.collider.transform, true);
//         else if (tankRoot != null)
//             plate.transform.SetParent(tankRoot.transform, true);
//     }
// }
