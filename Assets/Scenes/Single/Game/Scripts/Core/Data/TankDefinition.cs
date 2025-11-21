using UnityEngine;

[CreateAssetMenu(fileName = "New Tank", menuName = "Tanks/Tank Definition")]
public class TankDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string tankName;
    public TankClass tankClass;

    [Header("Core Stats")]
    public float health = 100f;
    public float moveSpeed = 5f;
    public float rotationSpeed = 50f;

    [Header("Combat Stats")]
    public float fireRate = 1f;
    public int minGunAngle = -10;
    public int maxGunAngle = 10;
    public float shootRange = 100f;
    public float projectileSpeed = 80f;
    public bool bulletUseGravity = true;

    [Header("AI Behavior")]
    public float detectionRadius = 50f;
    public float strafeRadius = 6f;
    [Range(0.05f, 2f)]
    public float strafeSpeed = 0.8f;
    public float baseSpreadDegrees = 1f;
    public float movingSpreadFactor = 6f;
    public float stationarySpreadFactor = 1f;

    [Header("Movement Physics")]
    public float maxMotorTorque = 1500f;
    public float maxBrakeTorque = 2000f;
    public float moveResponse = 5f;
    public float turnResponse = 5f;
    public float maxForwardSpeed = 10f;
    public float maxBackwardSpeed = 5f;
    public float turnSharpness = 1.5f;
    public float reverseLockDuration = 0.5f;
    public float movingThreshold = 0.15f;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float minIdleVolume = 0.2f;
    [Range(0f, 1f)] public float maxIdleVolume = 0.5f;
    [Range(0f, 1f)] public float minDriveVolume = 0f;
    [Range(0f, 1f)] public float maxDriveVolume = 0.5f;
    [Range(0.5f, 2f)] public float minIdlePitch = 0.8f;
    [Range(0.5f, 2f)] public float maxIdlePitch = 1.2f;
    [Range(0.5f, 2f)] public float minDrivePitch = 0.8f;
    [Range(0.5f, 2f)] public float maxDrivePitch = 1.3f;

    [Header("Audio Clips")]
    public AudioClip idleSound;
    public AudioClip driveSound;
    public AudioClip shootSound;

    [Header("Visual")]
    public Sprite icon;
    [TextArea(3, 5)]
    public string description;
}