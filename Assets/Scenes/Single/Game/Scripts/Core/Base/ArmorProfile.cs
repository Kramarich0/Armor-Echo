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
        if (plateCollider == null)
            plateCollider = GetComponentInChildren<Collider>();
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

    public Vector3 GetSmartWorldNormal(Vector3 contactPoint)
    {
        if (plateCollider != null)
        {
            Vector3 closest = plateCollider.ClosestPoint(contactPoint);
            Vector3 n = contactPoint - closest;
            if (n.sqrMagnitude > 1e-6f)
                return n.normalized;
        }
        return transform.forward.normalized;
    }


    public float CalculateEffectiveArmor(Vector3 bulletDirection, BulletDefinition bulletDef, out float rawAngleDeg)
    {
        Vector3 plateNormal = GetArmorWorldNormal();
        Vector3 bulletInto = -bulletDirection.normalized;

        float rawAngle = Vector3.Angle(plateNormal, bulletInto);
        rawAngleDeg = rawAngle;

        if (bulletDef.ignoreAngle)
            return thickness;

        float angle = rawAngle;
        float cal = bulletDef.caliber;
        float t = thickness;

        if (cal > t * 3f)
        {
            angle = 0f;
        }
        else if (cal > t * 2.5f)
        {
            angle *= 0.5f;
        }
        else if (cal > t * 2f)
        {
            angle *= 0.7f;
        }

        float effectiveAngle = Mathf.Max(0f, angle - bulletDef.normalization);
        float clampedAngle = Mathf.Min(effectiveAngle, 89f);

        float effArmor = thickness / Mathf.Cos(clampedAngle * Mathf.Deg2Rad);
        effArmor *= Ballistics.GetArmorTypeModifier(armorType);

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

        int thicknessAxis = 0;
        if (worldSize.y < worldSize.x && worldSize.y < worldSize.z) thicknessAxis = 1;
        else if (worldSize.z < worldSize.x && worldSize.z < worldSize.y) thicknessAxis = 2;

        // worldSize[thicknessAxis] = 0.01f; // плоская плита

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, box.transform.rotation, Vector3.one);

        Gizmos.color = fill;
        Gizmos.DrawCube(Vector3.zero, worldSize);

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Vector3.zero, worldSize);

        Gizmos.matrix = old;

        Vector3 normal = (thicknessAxis == 0 ? box.transform.right : (thicknessAxis == 1 ? box.transform.up : box.transform.forward)).normalized;
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

    private void DrawCompactLabel(Vector3 worldPos, float angle)
    {
        string txt = $"{thickness:F0}mm · {angle:F0}°";

        GUIStyle style = new(EditorStyles.boldLabel)
        {
            normal = { textColor = Color.white },
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
        };

        Handles.Label(worldPos, txt, style);
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