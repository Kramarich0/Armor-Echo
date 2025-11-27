using UnityEngine;

public class TurretAimingImpl
{
    readonly TurretAiming o;
    readonly TurretYaw yaw;
    readonly TurretPitch pitch;
    readonly TurretAudio audio;

    public TurretAimingImpl(TurretAiming mono)
    {
        o = mono;

        yaw = new TurretYaw(o);
        pitch = new TurretPitch(o);
        audio = new TurretAudio(o, yaw);
    }

    public void Start()
    {
        yaw.Start();
        pitch.Start();
    }

    public void LateUpdate()
    {
        if (!o.enabled) return;

        Vector3 aimPoint = o.cameraTransform.position + o.cameraTransform.forward * o.aimDistance;
        Vector3 worldDir = aimPoint - (o.gunPivot != null ? o.gunPivot.position : o.turretPivot.position);
        if (worldDir.sqrMagnitude < 0.0001f) return;

        bool rotating = yaw.UpdateYaw(worldDir);
        pitch.UpdatePitch(worldDir);

        audio.UpdateAudio(rotating);
    }
}
