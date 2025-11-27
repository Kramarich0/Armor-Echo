using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Tank : MonoBehaviour
{
    [Header("Core definition")]
    public TankDefinition definition;

    [Header("References")]
    public Rigidbody rb;
    public TankTrack leftTrack;
    public TankTrack rightTrack;

    [Header("Input")]
    public InputActionAsset actionsAsset;
    [Range(0f, 1f)] public float inputDeadzone = 0.02f;

    [Header("UI")]
    public SpeedDisplay speedDisplay;

    [Header("Physics")]
    public bool enforceMinimumMass = true;
    public float minRecommendedMass = 2000f;

    public float reverseLockDuration = 0.18f;
    public float stationaryTurnBoost = 1.5f;

    TankMovementImpl movementImpl;
    bool movementEnabled = true;
    public bool MovementEnabled => movementEnabled;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movementImpl = new TankMovementImpl(this);
        movementImpl.Awake();
    }

    void OnEnable() => movementImpl.OnEnable();
    void OnDisable() => movementImpl.OnDisable();

    void Update()
    {
        if (movementEnabled)
            movementImpl.Update();
    }

    void FixedUpdate()
    {
        if (movementEnabled)
            movementImpl.FixedUpdate();
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!enabled)
            movementImpl.StopAudio();
    }

    public float RotationSpeed => definition != null ? definition.rotationSpeed : 50f;
    public float TurretRotationSpeed => definition != null ? definition.turretRotationSpeed : 30f;
    public float MaxForwardSpeed => definition != null ? definition.MaxForwardSpeed : 10f;
    public float MaxBackwardSpeed => definition != null ? definition.MaxBackwardSpeed : 5f;
    public float TurnSharpness => definition != null ? definition.turnSharpness : 1.5f;
    public float MoveResponse => definition != null ? definition.moveResponse : 5f;
    public float TurnResponse => definition != null ? definition.turnResponse : 5f;
    public float ReverseLockDuration => definition != null ? definition.reverseLockDuration : 0.5f;
    public float MovingThreshold => definition != null ? definition.movingThreshold : 0.15f;
    public float ShootRange => definition.primaryGun != null ? definition.primaryGun.shootRange : 0f;
    public float LiftSpeed => definition.primaryGun != null ? definition.primaryGun.liftSpeed : 1f;
    public float Caliber => definition.primaryGun != null ? definition.primaryGun.caliber : 1f;
    public int MaxGunAngle => definition.primaryGun != null ? definition.primaryGun.maxGunAngle : 0;
    public int MinGunAngle => definition.primaryGun != null ? definition.primaryGun.minGunAngle : 0;
    public float FireRate => definition.primaryGun != null ? definition.primaryGun.FireRate : 0f;
    public bool BulletUseGravity => definition.primaryGun == null || definition.primaryGun.bulletUseGravity;
    public AudioClip ShootSound => definition.primaryGun != null ? definition.primaryGun.shootSound : null;
    public AudioClip ReloadSound => definition.primaryGun != null ? definition.primaryGun.reloadSound : null;
    public float MaxMotorTorque => definition != null ? definition.maxMotorTorque : 1500f;
    public float MaxBrakeTorque => definition != null ? definition.maxBrakeTorque : 2000f;
    public AudioClip IdleSound => definition != null ? definition.idleSound : null;
    public AudioClip DriveSound => definition != null ? definition.driveSound : null;
    public float MinIdleVolume => definition != null ? definition.minIdleVolume : 0.2f;
    public float MaxIdleVolume => definition != null ? definition.maxIdleVolume : 0.5f;
    public float MinDriveVolume => definition != null ? definition.minDriveVolume : 0f;
    public float MaxDriveVolume => definition != null ? definition.maxDriveVolume : 0.5f;
    public float MinIdlePitch => definition != null ? definition.minIdlePitch : 0.8f;
    public float MaxIdlePitch => definition != null ? definition.maxIdlePitch : 1.2f;
    public float MinDrivePitch => definition != null ? definition.minDrivePitch : 0.8f;
    public float MaxDrivePitch => definition != null ? definition.maxDrivePitch : 1.3f;

    public GunDefinition PrimaryGun => definition != null ? definition.primaryGun : null;

    public float GetMuzzleVelocity(BulletDefinition bullet)
    {
        if (PrimaryGun != null) return PrimaryGun.GetMuzzleVelocity(bullet);
        return 0f;
    }
}
