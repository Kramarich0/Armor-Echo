using UnityEngine;

public class TurretAiming : MonoBehaviour
{
    public Transform turretPivot;
    public Transform gunPivot;
    public Transform cameraTransform;
    public TankSniperView sniperView;
    public float aimDistance = 200f;
    public AudioClip yawLoopSound;

    public float yawMinVolume;
    public float yawMaxVolume;
    public Vector2 yawPitchRange;
    public float yawStartThreshold;
    public float yawStopThreshold;
    public float pitchSnapAngle;
    public float audioFadeSpeed;

    public Tank owner;

    TurretAimingImpl impl;

    void Start()
    {
        impl = new TurretAimingImpl(this);
        impl.Start();
    }

    void LateUpdate()
    {
        impl.LateUpdate();
    }
}
