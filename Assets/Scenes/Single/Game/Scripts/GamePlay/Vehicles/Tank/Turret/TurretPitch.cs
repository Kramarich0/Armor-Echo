using UnityEngine;

public class TurretPitch
{
    readonly TurretAiming o;
    float currentPitch;

    public void Start()
    {
        float p = o.gunPivot.localEulerAngles.x;
        if (p > 180f) p -= 360f;
    }

    public TurretPitch(TurretAiming o)
    {
        this.o = o;

        float initPitch = o.gunPivot.localEulerAngles.x;
        if (initPitch > 180f) initPitch -= 360f;
        currentPitch = initPitch;
    }

    public void UpdatePitch(Vector3 worldDir)
    {
        Vector3 local = o.turretPivot.InverseTransformDirection(worldDir);

        float targetPitch = -Mathf.Atan2(local.y, local.z) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch, o.owner.MinGunAngle, o.owner.MaxGunAngle);

        float curP = o.gunPivot.localEulerAngles.x;
        if (curP > 180f) curP -= 360f;

        float diff = Mathf.DeltaAngle(curP, targetPitch);

        if (Mathf.Abs(diff) <= o.pitchSnapAngle)
            currentPitch = targetPitch;
        else
            currentPitch = Mathf.MoveTowardsAngle(curP, targetPitch, o.owner.LiftSpeed * Time.deltaTime);

        o.gunPivot.localEulerAngles = new Vector3(currentPitch, 0f, 0f);
    }
}
