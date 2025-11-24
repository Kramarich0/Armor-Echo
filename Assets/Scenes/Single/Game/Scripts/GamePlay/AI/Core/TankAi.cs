using Serilog;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AITankHealth))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(TeamComponent))]
[RequireComponent(typeof(Rigidbody))]
public class TankAI : MonoBehaviour
{
    [Header("=== ОСНОВНЫЕ НАСТРОЙКИ ===")]
    [Header("Определение танка")]
    [Tooltip("Обязательно назначить TankDefinition для этого танка")]
    public TankDefinition tankDefinition;

    [Header("=== СИСТЕМА ЗДОРОВЬЯ ===")]
    internal AITankHealth tankHealth;

    [Header("=== ПРЕФАБЫ И ССЫЛКИ ===")]
    [Header("Transform точки")]
    public Transform turret;
    public Transform gun;
    public Transform gunEnd;
    public Transform body;

    [Header("Система снарядов")]
    public BulletSlot[] bulletSlots;

    [Header("Система гусениц")]
    public TankTrack leftTrack;
    public TankTrack rightTrack;

    [Header("=== ОТЛАДКА И НАСТРОЙКИ ===")]
    public bool debugGizmos = true;
    public bool debugLogs = false;

    [Header("Корректировки осей моделей")]
    [Tooltip("Если модель корпуса в сцене смотрит 'назад' относительно forward (Z), включи это")]
    public bool invertBodyForward = false;
    [Tooltip("Если башня у модели смотрит в -Z (назад), включи это")]
    public bool invertTurretForward = false;
    [Tooltip("Если ствол вверх/вниз использует локальную ось X вместо Z, включи это")]
    public bool gunUsesLocalXForPitch = true;
    [Header("Другое")]
    public bool enableStrafeWhileShooting = true;
    public LayerMask capturePointsLayer = -1;
    public float capturePointDetectionRadius = 60f;

    public float MoveSpeed => tankDefinition.moveSpeed;
    public float RotationSpeed => tankDefinition.rotationSpeed;

    public float ShootRange => tankDefinition.primaryGun != null ? tankDefinition.primaryGun.shootRange : 0f;
    public int MaxGunAngle => tankDefinition.primaryGun != null ? tankDefinition.primaryGun.maxGunAngle : 0;
    public int MinGunAngle => tankDefinition.primaryGun != null ? tankDefinition.primaryGun.minGunAngle : 0;
    public float FireRate => tankDefinition.primaryGun != null ? tankDefinition.primaryGun.fireRate : 0f;
    public bool BulletUseGravity => tankDefinition.primaryGun == null || tankDefinition.primaryGun.bulletUseGravity;
    public AudioClip ShootSound => tankDefinition.primaryGun != null ? tankDefinition.primaryGun.shootSound : null;
    public float DetectionRadius => tankDefinition.detectionRadius;
    public float StrafeRadius => tankDefinition.strafeRadius;
    public float StrafeSpeed => tankDefinition.strafeSpeed;
    public float BaseSpreadDegrees => tankDefinition.baseSpreadDegrees;
    public float MovingSpreadFactor => tankDefinition.movingSpreadFactor;
    public float StationarySpreadFactor => tankDefinition.stationarySpreadFactor;

    public float MaxMotorTorque => tankDefinition.maxMotorTorque;
    public float MaxBrakeTorque => tankDefinition.maxBrakeTorque;
    public float MoveResponse => tankDefinition.moveResponse;
    public float TurnResponse => tankDefinition.turnResponse;
    public float MaxForwardSpeed => tankDefinition.maxForwardSpeed;
    public float MaxBackwardSpeed => tankDefinition.maxBackwardSpeed;
    public float TurnSharpness => tankDefinition.turnSharpness;
    public float ReverseLockDuration => tankDefinition.reverseLockDuration;
    public float MovingThreshold => tankDefinition.movingThreshold;

    public AudioClip IdleSound => tankDefinition.idleSound;
    public AudioClip DriveSound => tankDefinition.driveSound;
    public float MinIdleVolume => tankDefinition.minIdleVolume;
    public float MaxIdleVolume => tankDefinition.maxIdleVolume;
    public float MinDriveVolume => tankDefinition.minDriveVolume;
    public float MaxDriveVolume => tankDefinition.maxDriveVolume;
    public float MinIdlePitch => tankDefinition.minIdlePitch;
    public float MaxIdlePitch => tankDefinition.maxIdlePitch;
    public float MinDrivePitch => tankDefinition.minDrivePitch;
    public float MaxDrivePitch => tankDefinition.maxDrivePitch;

    [Header("=== СЛУЖЕБНЫЕ ПЕРЕМЕННЫЕ ===")]
    internal NavMeshAgent agent;
    internal bool navAvailable = false;
    internal TeamComponent teamComp;
    internal NavMeshAgent targetAgent;
    internal AIState currentState = AIState.Idle;
    internal float nextFireTime = 0f;
    internal float strafePhase = 0f;
    internal Transform currentTarget;
    internal float scanTimer = 0f;
    internal readonly float scanInterval = 0.4f;
    internal CapturePoint currentCapturePointTarget = null;
    internal AudioSource idleSource;
    internal AudioSource driveSource;
    internal AudioSource shootSource;
    internal Rigidbody targetRigidbody;

    public TankClass CurrentTankClass => tankDefinition.tankClass;

    [Header("=== КЕШИ ДЛЯ ИИ ===")]
    [HideInInspector] public Transform cachedTransform;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Collider[] cachedColliders;
    [HideInInspector] public AICombat combat;
    [HideInInspector] public AINavigation navigation;
    [HideInInspector] public AIPerception perception;
    [HideInInspector] public AIWeapons weapons;
    [HideInInspector] public AIStateHandler stateHandler;

    TankAIImpl impl;

    void Awake()
    {
        if (tankDefinition == null)
        {
            Log.Error("[TankAI] TankDefinition не назначен для {TankName}", gameObject.name);
            return;
        }

        cachedTransform = transform;
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        navAvailable = agent != null && agent.isOnNavMesh;

        cachedColliders = GetComponentsInParent<Collider>();
        combat = new AICombat(this);
        navigation = new AINavigation(this);
        perception = new AIPerception(this);
        weapons = new AIWeapons(this);
        stateHandler = new AIStateHandler(this, perception, navigation, combat, weapons);


        impl = new TankAIImpl(this);
        impl.Awake();

        if (tankHealth != null)
        {
            tankHealth.maxHealth = tankDefinition.health;
            tankHealth.currentHealth = tankDefinition.health;
        }
    }

    void Start()
    {
        if (tankDefinition == null) return;
        impl.Start();
    }

    void Update()
    {
        if (tankDefinition == null) return;
        impl.Update();
    }

    void OnDrawGizmos()
    {
        impl?.OnDrawGizmos();
    }

    public BulletSlot GetBulletByType(BulletType type)
    {
        if (bulletSlots == null) return null;
        foreach (var slot in bulletSlots)
        {
            if (slot.type == type) return slot;
        }
        return null;
    }

    public float GetMuzzleVelocity(BulletDefinition def)
    {
        GunDefinition g = tankDefinition != null ? tankDefinition.primaryGun : null;
        if (g != null)
            return g.GetMuzzleVelocity(def);

        return 0f;
    }

    // void OnEnable() => AIPerception.InvalidateCaches();
    // void OnDisable() => AIPerception.InvalidateCaches();
    // void OnDestroy() => AIPerception.InvalidateCaches();

}