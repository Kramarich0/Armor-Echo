using Serilog;
using UnityEngine;

public class AIWeapons
{
    readonly TankAI owner;
    public AIWeapons(TankAI owner) { this.owner = owner; }

    public void ShootAt(Transform target, BulletType bulletType = BulletType.APHE)
    {
        if (owner == null) return;
        if (owner.gunEnd == null)
        {
            if (owner.debugLogs)
                Log.Warning("[AIWeapons] gunEnd is null, cannot shoot on {OwnerName}", owner.name);
            return;
        }

        if (!owner.perception.HasLineOfSight(target))
        {
            if (owner.debugLogs)
                Log.Debug("[AIWeapons] No line of sight to target {TargetName}, skipping shot", target.name);
            return;
        }

        var bulletSlot = owner.GetBulletByType(bulletType);
        if (bulletSlot == null || bulletSlot.pool == null || bulletSlot.definition == null)
        {
            if (owner.debugLogs)
                Log.Warning("[AIWeapons] No valid bullet slot for {BulletType} on {OwnerName}", bulletType, owner.name);
            return;
        }

        float muzzleVel = owner.GetMuzzleVelocity(bulletSlot.definition);
        Vector3 predicted = owner.combat.PredictTargetPosition(target, muzzleVel);
        Vector3 aim = predicted - owner.gunEnd.position;

        Vector3 launchVelocity = CalculateLaunchVelocity(aim, muzzleVel);
        Vector3 finalVelocity = ApplySpread(launchVelocity, owner.gunEnd);

        string shooterDisplay = (owner.teamComp != null && !string.IsNullOrEmpty(owner.teamComp.DisplayName))
                 ? owner.teamComp.DisplayName
                 : owner.gameObject.name;

        bulletSlot.pool.SpawnBullet(
          owner.gunEnd.position,
          finalVelocity,
          bulletSlot.definition,
          owner.teamComp?.team ?? TeamEnum.Neutral,
          shooterDisplay,
          owner.cachedColliders
      );

        owner.shootSource?.Play();
    }

    private Vector3 CalculateLaunchVelocity(Vector3 aim, float speed)
    {
        if (owner == null) return Vector3.zero;
        if (speed <= 0f) return aim.normalized * 0f;

        if (owner.BulletUseGravity)
        {
            Vector3 horizontal = new Vector3(aim.x, 0f, aim.z);
            float distance = horizontal.magnitude;
            float height = aim.y;

            if (distance < 0.1f)
                return aim.normalized * speed;

            float v = speed;
            float g = Mathf.Abs(Physics.gravity.y);

            float v2 = v * v;
            float v4 = v2 * v2;
            float discriminant = v4 - g * (g * distance * distance + 2 * height * v2);

            if (discriminant < 0)
                return aim.normalized * speed;

            float sqrtDisc = Mathf.Sqrt(discriminant);
            float angleLow = Mathf.Atan2(v2 - sqrtDisc, g * distance);

            Vector3 flatDir = horizontal.normalized;
            Vector3 axis = Vector3.Cross(flatDir, Vector3.up);
            Quaternion launchQuat = Quaternion.AngleAxis(angleLow * Mathf.Rad2Deg, axis);
            Vector3 launchDir = launchQuat * flatDir;
            return launchDir * v;
        }
        else
        {
            return aim.normalized * speed;
        }
    }

    private Vector3 ApplySpread(Vector3 velocity, Transform gunEnd)
    {
        float moveSpeed = 0f;
        if (owner.rb != null) moveSpeed = owner.rb.linearVelocity.magnitude;
        else if (owner.agent != null) moveSpeed = owner.agent.velocity.magnitude;

        float speedFactor = Mathf.Clamp01(moveSpeed / Mathf.Max(0.1f, owner.MoveSpeed));
        float spreadDeg = owner.BaseSpreadDegrees * Mathf.Lerp(owner.StationarySpreadFactor, owner.MovingSpreadFactor, speedFactor);

        Quaternion spreadRot = Quaternion.Euler(
            Random.Range(-spreadDeg, spreadDeg),
            Random.Range(-spreadDeg, spreadDeg),
            0f
        );

        return spreadRot * velocity;
    }
}
