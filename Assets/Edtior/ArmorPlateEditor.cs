using UnityEngine;
using UnityEditor;

public class ArmorPlateBatchEditor : EditorWindow
{
    private float thickness = 80f;
    private ArmorType armorType = ArmorType.RHA;
    private Vector3 normal = Vector3.forward;

    [MenuItem("Tools/Armor/Batch Plate Editor")]
    public static void Open()
    {
        GetWindow<ArmorPlateBatchEditor>("Batch Armor Editor");
    }

    void OnGUI()
    {
        GUILayout.Label("Массовое редактирование бронеплит", EditorStyles.boldLabel);

        thickness = EditorGUILayout.FloatField("Толщина", thickness);
        armorType = (ArmorType)EditorGUILayout.EnumPopup("Тип брони", armorType);
        normal = EditorGUILayout.Vector3Field("Нормаль", normal);

        GUILayout.Space(10);
        if (GUILayout.Button("Применить к выделенным плиткам"))
        {
            ApplyToSelectedPlates();
        }
    }

    void ApplyToSelectedPlates()
    {
        var selectedPlates = Selection.GetFiltered<ArmorPlate>(SelectionMode.Editable | SelectionMode.Deep);

        if (selectedPlates.Length == 0)
        {
            Debug.LogWarning("Выдели хотя бы одну бронеплиту!");
            return;
        }

        Undo.RecordObjects(selectedPlates, "Batch Armor Edit");

        foreach (var plate in selectedPlates)
        {
            plate.thickness = thickness;
            plate.armorType = armorType;
            plate.armorNormal = normal;

            // Обновляем BoxCollider визуально
            if (plate.TryGetComponent<BoxCollider>(out var col))
            {
                col.size = new Vector3(col.size.x, col.size.y, thickness);
            }
        }

        Debug.Log($"Применено к {selectedPlates.Length} бронеплитам.");
    }
}
