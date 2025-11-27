using UnityEngine;

public class TurretYaw
{
    readonly TurretAiming o;
    float smoothedSpeed;
    public bool WasRotating { get; private set; }

    public float CurrentYaw { get; private set; }

    public TurretYaw(TurretAiming o)
    {
        this.o = o;

        float initYaw = o.turretPivot.localEulerAngles.y;
        if (initYaw > 180f) initYaw -= 360f;
        CurrentYaw = initYaw;
    }
    public void Start()
    {
        float y = o.turretPivot.localEulerAngles.y;
        if (y > 180f) y -= 360f;
    }

    public bool UpdateYaw(Vector3 worldDir)
    {
        Transform yawBase = o.turretPivot.parent != null ? o.turretPivot.parent : o.turretPivot;

        Vector3 localDir = yawBase.InverseTransformDirection(worldDir);
        localDir.y = 0f;

        if (localDir.sqrMagnitude < 0.000001f)
            return false;

        localDir.Normalize();
        float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        float curYaw = o.turretPivot.localEulerAngles.y;
        if (curYaw > 180f) curYaw -= 360f;

        float angleDiff = Mathf.DeltaAngle(curYaw, targetYaw);

        bool shouldRotate = Mathf.Abs(angleDiff) > o.yawStartThreshold;
        bool closeEnough = Mathf.Abs(angleDiff) <= o.yawStopThreshold;

        bool rotating = false;

        if (closeEnough)
        {
            CurrentYaw = targetYaw;
        }
        else if (shouldRotate)
        {
            CurrentYaw = Mathf.MoveTowardsAngle(curYaw, targetYaw, o.owner.TurretRotationSpeed * Time.deltaTime);
            rotating = true;

            float actualSpeed = Mathf.Abs(Mathf.DeltaAngle(curYaw, CurrentYaw)) / Time.deltaTime;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, actualSpeed, Time.deltaTime * 10f);
        }
        else
        {
            CurrentYaw = Mathf.MoveTowardsAngle(curYaw, targetYaw, o.owner.TurretRotationSpeed * Time.deltaTime);
            rotating = WasRotating;
        }

        o.turretPivot.localEulerAngles = new Vector3(0f, CurrentYaw, 0f);
        WasRotating = rotating;
        return rotating;
    }

    public float GetNormalizedSpeed()
    {
        return Mathf.Clamp01(smoothedSpeed / o.owner.TurretRotationSpeed);
    }
}
