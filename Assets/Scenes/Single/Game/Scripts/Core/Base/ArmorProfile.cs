using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
public class ArmorPlate : MonoBehaviour
{
    [Header("Бронеплита")]
    public float thickness = 80f;

    [Header("Тип брони")]
    public ArmorType armorType = ArmorType.RHA;

    private Collider plateCollider;
    [Header("Направление брони")]
    public Vector3 armorNormal = Vector3.forward;

    void Awake()
    {
        plateCollider = GetComponent<Collider>();
        // if (plateCollider == null)
        //     plateCollider = GetComponentInChildren<Collider>(); // не трогать ето избыточно крч
    }

    public Vector3 GetArmorWorldNormal()
    {
        return transform.TransformDirection(armorNormal);
    }


    public Vector3 GetPlateWorldCenter()
    {
        if (plateCollider is BoxCollider box)
            return plateCollider.transform.TransformPoint(box.center);

        if (plateCollider != null)
            return plateCollider.bounds.center;

        return transform.position;
    }

    public Vector3 GetSmartWorldNormal(Vector3 bulletDir)
    {
        Vector3 plateNormal = GetArmorWorldNormal();

        if (Vector3.Dot(plateNormal, -bulletDir) < 0f)
            plateNormal = -plateNormal;

        return plateNormal.normalized;
    }


    public float CalculateEffectiveArmor(Vector3 bulletDirection, BulletDefinition bulletDef, out float rawAngleDeg, out Vector3 outPlateNormal)
    {
        Vector3 plateNormal = GetSmartWorldNormal(bulletDirection);
        outPlateNormal = plateNormal;

        Vector3 bulletInto = -bulletDirection.normalized;

        float rawAngle = Vector3.Angle(plateNormal, bulletInto);
        rawAngleDeg = Mathf.Clamp(rawAngle, 0f, 90f);
        float armorMod = Ballistics.GetArmorTypeModifier(armorType);
        if (bulletDef != null && bulletDef.ignoreAngle)
            return thickness * armorMod;

        float effectiveAngle = Mathf.Max(0f, rawAngle - (bulletDef?.normalization ?? 0f));
        float clampedAngle = Mathf.Min(effectiveAngle, 89f);

        float cos = Mathf.Cos(clampedAngle * Mathf.Deg2Rad);
        cos = Mathf.Max(0.001f, cos);
        float effArmor = thickness / cos;

        effArmor *= armorMod;

        return effArmor;
    }


#if UNITY_EDITOR

    static float cachedMaxThickness = 50f;
    static bool cacheDirty = true;

    void OnEnable()
    {
        cacheDirty = true;
    }

    void OnValidate()
    {
        cacheDirty = true;
    }

    private static void RecomputeMaxThicknessIfNeeded()
    {
        if (!cacheDirty) return;
        cachedMaxThickness = 50f;
        var all = FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].thickness > cachedMaxThickness)
                cachedMaxThickness = all[i].thickness;
        cacheDirty = false;
    }

    private Color GetThicknessColor()
    {
        RecomputeMaxThicknessIfNeeded();
        float normalized = Mathf.InverseLerp(0f, Mathf.Max(10f, cachedMaxThickness), thickness);


        if (normalized < 0.25f) return Color.blue;
        if (normalized < 0.5f) return Color.green;
        if (normalized < 0.75f) return Color.yellow;
        return Color.red;
    }

    private void DrawPlateGizmo(bool selected)
    {
        if (plateCollider == null || plateCollider is not BoxCollider box) return;

        Color fill = GetThicknessColor();
        fill.a = selected ? 0.45f : 0.2f;

        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 worldSize = Vector3.Scale(box.size, box.transform.lossyScale);

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, box.transform.rotation, Vector3.one);

        Gizmos.color = fill;
        Gizmos.DrawCube(Vector3.zero, worldSize);

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Vector3.zero, worldSize);

        Gizmos.matrix = old;

        Vector3 normal = GetArmorWorldNormal();
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(center, normal * 0.25f);
        DrawArrow(center + normal * 0.25f, normal * 0.06f, Color.cyan);

        if (selected)
        {
            float angleToForward = Vector3.Angle(normal, Vector3.forward);
            Handles.Label(center + normal * 0.05f, $"{thickness:F0}mm · {angleToForward:F0}°",
                new GUIStyle(EditorStyles.boldLabel)
                { normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter, fontSize = 11 });
        }
    }

    private void DrawArrow(Vector3 position, Vector3 direction, Color color)
    {
        Gizmos.color = color;
        Quaternion rot = Quaternion.LookRotation(direction.normalized);
        Vector3 right = rot * Quaternion.Euler(0, 160, 0) * Vector3.forward;
        Vector3 left = rot * Quaternion.Euler(0, 200, 0) * Vector3.forward;
        Gizmos.DrawRay(position, right * 0.1f);
        Gizmos.DrawRay(position, left * 0.1f);
    }

    void OnDrawGizmosSelected()
    {
        if (plateCollider == null)
            plateCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (plateCollider == null) return;

        DrawPlateGizmo(true);
    }

    void OnDrawGizmos()
    {
        if (plateCollider == null)
            plateCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (plateCollider == null) return;

        DrawPlateGizmo(false);
    }

#endif
}