using UnityEngine;

[CreateAssetMenu(fileName = "NewBullet", menuName = "Weapons/Bullet")]
public class BulletDefinition : ScriptableObject
{
    public BulletType type = BulletType.AP;

    [Header("=== CORE (основные характеристики снаряда) ===")]
    public string bulletName = "Unknown bullet";

    [Tooltip("Калибр снаряда в мм (влияет на оверматч и поведение брони).")]
    public int caliber = 76;
    [Tooltip("Масса снаряда в килограммах.")]
    public float massKg = 6.8f;

    [Tooltip("Базовый урон при пробитии.")]
    public int damage = 40;

    [Tooltip("Паспортная пробиваемость при вылете из ствола (мм).")]
    public float penetration = 120f;

    [Tooltip("Использует ли снаряд гравитацию в полёте.")]
    public bool useGravity = true;

    [Header("=== BALLISTICS (падение скорости/пробития по дистанции) ===")]

    [Tooltip("Коэффициент воздушного сопротивления. Чем меньше — тем медленнее теряет скорость (0.0002–0.001).")]
    public float ballisticK = 0.001f;

    [Tooltip("Показатель степени в формуле де Марра. 1.0–1.4 для нормальных AP, 1.5–2.0 для APCR.")]
    public float deMarreK = 1.5f;

    [Tooltip("Минимальная скорость, ниже которой снаряд не опускается (м/с).")]
    public float minSpeed = 30f;

    [Tooltip("Минимальная пробиваемость, ниже которой она не падает.")]
    public float minPenetration = 5f;

    [Header("=== ANGLE & BEHAVIOR (углы, рикошеты, нормализация) ===")]

    [Tooltip("Угол рикошета (в градусах). Если больше — почти гарантирован рикошет.")]
    public float ricochetAngle = 70f;

    [Tooltip("Насколько снаряд 'вклинивается' в броню, уменьшая фактический угол попадания (APCBC = 8–12°).")]
    public float normalization = 5f;

    [Tooltip("Если true — угол вообще игнорируется (кумулятивы).")]
    public bool ignoreAngle = false;

    [Header("=== OVERMATCH (эффект большого калибра) ===")]

    [Tooltip("Если калибр больше толщины брони × этот коэффициент → снаряд получает бонус.")]
    public float overmatchFactor = 1.5f;


    [Header("=== RICOCHET PHYSICS (поведение после рикошета) ===")]

    [Tooltip("На сколько уменьшается скорость после рикошета (0.1–1).")]
    [Range(0.1f, 1f)]
    public float ricochetSpeedLoss = 0.6f;


    [Header("=== SPLASH / HE EFFECTS (фугасы, APHE) ===")]

    [Tooltip("Радиус осколочного урона. 0 = без сплэша (обычные AP).")]
    public float splashRadius = 0f;


    [Header("=== VISUAL (визуал снаряда) ===")]

    [Tooltip("Префаб визуальной модели пули.")]
    public GameObject visualPrefab;
}
