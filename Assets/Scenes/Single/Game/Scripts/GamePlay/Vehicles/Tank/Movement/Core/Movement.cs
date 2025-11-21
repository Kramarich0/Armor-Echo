using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankMovement : MonoBehaviour
{
    [Header("Input")] 
    public InputActionAsset actionsAsset; [Range(0f, 1f)]
    public float inputDeadzone = 0.02f;

    [Header("Tracks")]
    public TankTrack leftTrack;
    public TankTrack rightTrack;

    [Header("Speed")] public SpeedDisplay speedDisplay;
    public float maxForwardSpeed = 8f;
    public float maxBackwardSpeed = 4f;
    [Range(0f, 1f)] public float turnSharpness = 0.7f;
    public float moveResponse = 2.5f;
    public float turnResponse = 3.0f;

    [Header("Torque / Brakes")]
    public float maxMotorTorque = 4500f;
    public float maxBrakeTorque = 5000f;

    [Header("Rigidbody / Safety")]
    public Rigidbody rb; public bool enforceMinimumMass = true;
    public float minRecommendedMass = 2000f;
    public float movingThreshold = 0.15f;

    [Header("Engine Sounds")]
    public AudioClip idleSound;
    public AudioClip driveSound; [Range(0f, 1f)]
    public float minVolume = 0.2f; [Range(0f, 1f)]
    public float maxVolume = .7f; [Range(0.5f, 2f)]
    public float minPitch = 0.7f; [Range(0.5f, 2f)]
    public float maxPitch = 1.3f;
    public float reverseLockDuration = 0.18f;
    public float stationaryTurnBoost = 1.5f;

    TankMovementImpl impl;
    void Awake() { impl = new TankMovementImpl(this); impl.Awake(); }
    void OnEnable() { impl.OnEnable(); }
    void OnDisable() { impl.OnDisable(); }
    void Update() { impl.Update(); }
    void FixedUpdate() { impl.FixedUpdate(); }
}
