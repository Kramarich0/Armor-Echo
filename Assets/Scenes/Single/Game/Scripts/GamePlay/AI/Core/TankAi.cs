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
    public bool invertBodyForward = false;
    public bool invertTurretForward = false;
    public bool gunUsesLocalXForPitch = true;
    [Header("Другое")]
    public bool enableStrafeWhileShooting = true;
    public LayerMask capturePointsLayer = -1;
    public float capturePointDetectionRadius = 60f;

    public float MoveSpeed => tankDefinition != null ? tankDefinition.MaxForwardSpeed : 0f;
    public float RotationSpeed => tankDefinition != null ? tankDefinition.rotationSpeed : 90f;
    public float TurretRotationSpeed => tankDefinition != null ? tankDefinition.turretRotationSpeed : 90f;

    public float ShootRange => tankDefinition != null && tankDefinition.primaryGun != null ? tankDefinition.primaryGun.shootRange : 0f;
    public int MaxGunAngle => tankDefinition != null && tankDefinition.primaryGun != null ? tankDefinition.primaryGun.maxGunAngle : 0;
    public int MinGunAngle => tankDefinition != null && tankDefinition.primaryGun != null ? tankDefinition.primaryGun.minGunAngle : 0;
    public float FireRate => tankDefinition != null && tankDefinition.primaryGun != null ? tankDefinition.primaryGun.FireRate : 0f;
    public bool BulletUseGravity => tankDefinition == null || tankDefinition.primaryGun == null || tankDefinition.primaryGun.bulletUseGravity;
    public AudioClip ShootSound => tankDefinition != null && tankDefinition.primaryGun != null ? tankDefinition.primaryGun.shootSound : null;
    public float DetectionRadius => tankDefinition != null ? tankDefinition.detectionRadius : 0f;
    public float StrafeRadius => tankDefinition != null ? tankDefinition.strafeRadius : 4f;
    public float StrafeSpeed => tankDefinition != null ? tankDefinition.strafeSpeed : 1f;
    public float BaseSpreadDegrees => tankDefinition != null ? tankDefinition.baseSpreadDegrees : 1f;
    public float MovingSpreadFactor => tankDefinition != null ? tankDefinition.movingSpreadFactor : 2f;
    public float StationarySpreadFactor => tankDefinition != null ? tankDefinition.stationarySpreadFactor : 1f;

    public float MaxMotorTorque => tankDefinition != null ? tankDefinition.maxMotorTorque : 1000f;
    public float MaxBrakeTorque => tankDefinition != null ? tankDefinition.maxBrakeTorque : 1000f;
    public float MoveResponse => tankDefinition != null ? tankDefinition.moveResponse : 0.1f;
    public float TurnResponse => tankDefinition != null ? tankDefinition.rotationSpeed : 0.1f; // if your field name differs, adjust
    public float MaxForwardSpeed => tankDefinition != null ? tankDefinition.MaxForwardSpeed : 6f;
    public float MaxBackwardSpeed => tankDefinition != null ? tankDefinition.MaxBackwardSpeed : 3f;
    public float TurnSharpness => tankDefinition != null ? tankDefinition.turnSharpness : 1f;
    public float ReverseLockDuration => tankDefinition != null ? tankDefinition.reverseLockDuration : 0.4f;
    public float MovingThreshold => tankDefinition != null ? tankDefinition.movingThreshold : 0.1f;

    public AudioClip IdleSound => tankDefinition != null ? tankDefinition.idleSound : null;
    public AudioClip DriveSound => tankDefinition != null ? tankDefinition.driveSound : null;
    public float MinIdleVolume => tankDefinition != null ? tankDefinition.minIdleVolume : 0.1f;
    public float MaxIdleVolume => tankDefinition != null ? tankDefinition.maxIdleVolume : 0.5f;
    public float MinDriveVolume => tankDefinition != null ? tankDefinition.minDriveVolume : 0.1f;
    public float MaxDriveVolume => tankDefinition != null ? tankDefinition.maxDriveVolume : 0.7f;
    public float MinIdlePitch => tankDefinition != null ? tankDefinition.minIdlePitch : 0.9f;
    public float MaxIdlePitch => tankDefinition != null ? tankDefinition.maxIdlePitch : 1.2f;
    public float MinDrivePitch => tankDefinition != null ? tankDefinition.minDrivePitch : 0.9f;
    public float MaxDrivePitch => tankDefinition != null ? tankDefinition.maxDrivePitch : 1.2f;

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

    public TankClass CurrentTankClass => tankDefinition != null ? tankDefinition.tankClass : TankClass.Light;

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

        // teamComp may be assigned in AIInit.Awake; ensure null-safety
        teamComp = GetComponent<TeamComponent>();

        navAvailable = agent != null && agent.isOnNavMesh;

        cachedColliders = GetComponentsInParent<Collider>();
        combat = new AICombat(this);
        navigation = new AINavigation(this);
        perception = new AIPerception(this);
        weapons = new AIWeapons(this);
        stateHandler = new AIStateHandler(this, perception, navigation, combat, weapons);

        impl = new TankAIImpl(this);
        impl.Awake();
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
}
