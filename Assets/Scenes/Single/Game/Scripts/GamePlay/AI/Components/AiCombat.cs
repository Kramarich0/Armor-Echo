using UnityEngine;

public class AICombat
{
    readonly TankAI owner;

    float pitchVelocity = 0f;

    const float DefaultPitchSmoothTime = 0.08f;
    const float DefaultPitchDeadzone = 0.6f;

    public AICombat(TankAI owner) { this.owner = owner; }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }

    public void AimAt(Transform t)
    {
        if (t == null) return;

        if (owner.turret != null)
        {
            Vector3 dir = t.position - owner.turret.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Vector3 forwardDir = dir.normalized * (owner.invertTurretForward ? -1f : 1f);
                Quaternion targetRot = Quaternion.LookRotation(forwardDir, Vector3.up);
                owner.turret.rotation = Quaternion.RotateTowards(owner.turret.rotation, targetRot, owner.RotationSpeed * Time.deltaTime);
            }
        }

        if (owner.gun == null || owner.gunEnd == null) return;
        if (owner.tankDefinition.primaryGun == null || owner.tankDefinition.primaryGun.bullets.Length == 0) return;

        BulletDefinition bullet = owner.tankDefinition.primaryGun.bullets[0].bullet;
        float projectileSpeed = owner.GetMuzzleVelocity(bullet);
        Vector3 predicted = PredictTargetPosition(t, projectileSpeed);

        Vector3 toTargetWorld = predicted - owner.gun.position;
        if (toTargetWorld.sqrMagnitude < 0.000001f) return;

        Quaternion desiredWorldRot = Quaternion.LookRotation(toTargetWorld.normalized, Vector3.up);
        Quaternion turretWorldRot = owner.turret != null ? owner.turret.rotation : Quaternion.identity;
        Quaternion desiredLocalRot = Quaternion.Inverse(turretWorldRot) * desiredWorldRot;
        Vector3 desiredLocalEuler = desiredLocalRot.eulerAngles;

        bool pitchUsesLocalX = owner.gunUsesLocalXForPitch;
        float targetPitch = pitchUsesLocalX ? NormalizeAngle(desiredLocalEuler.x) : NormalizeAngle(desiredLocalEuler.z);
        targetPitch = Mathf.Clamp(targetPitch, owner.MinGunAngle, owner.MaxGunAngle);

        Vector3 curEuler = owner.gun.localEulerAngles;
        float currentPitch = pitchUsesLocalX ? NormalizeAngle(curEuler.x) : NormalizeAngle(curEuler.z);
        float delta = Mathf.DeltaAngle(currentPitch, targetPitch);

        float deadzone = DefaultPitchDeadzone;
        float smoothTime = DefaultPitchSmoothTime;

        if (Mathf.Abs(delta) <= deadzone)
        {
            currentPitch = targetPitch;
            pitchVelocity = 0f;
        }
        else
        {
            float maxDelta = owner.RotationSpeed * Time.deltaTime;
            float newPitch = Mathf.MoveTowardsAngle(currentPitch, targetPitch, maxDelta);
            float smooth = Mathf.SmoothDampAngle(currentPitch, newPitch, ref pitchVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            currentPitch = smooth;
        }

        Vector3 newLocalEuler = curEuler;
        if (pitchUsesLocalX) newLocalEuler.x = currentPitch; else newLocalEuler.z = currentPitch;
        owner.gun.localEulerAngles = newLocalEuler;
    }

    public Vector3 PredictTargetPosition(Transform t, float projectileSpeed)
    {
        Vector3 targetPos = t.position;
        Rigidbody rb = owner.targetRigidbody;
        Vector3 targetVel = Vector3.zero;
        if (owner.targetAgent != null)
            targetVel = owner.targetAgent.velocity;
        else if (rb != null)
            targetVel = rb.linearVelocity;

        Vector3 dir = targetPos - owner.gunEnd.position;
        float dist = dir.magnitude;
        float time = dist / Mathf.Max(0.001f, projectileSpeed);

        for (int i = 0; i < 2; i++)
        {
            Vector3 predicted = targetPos + targetVel * time;
            Vector3 toPred = predicted - owner.gunEnd.position;
            float d = toPred.magnitude;
            time = d / Mathf.Max(0.001f, projectileSpeed);
        }

        return targetPos + targetVel * time;
    }

}
