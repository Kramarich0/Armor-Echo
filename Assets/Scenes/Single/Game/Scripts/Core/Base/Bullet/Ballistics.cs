using UnityEngine;

public static class Ballistics
{
    public static float ComputeSpeed(float muzzleVelocity, BulletDefinition def, float distance)
    {
        if (def == null) return 0f;

        float massFactor = 1f / Mathf.Sqrt(Mathf.Max(def.massKg, 0.01f));
        float v = muzzleVelocity * Mathf.Exp(-def.ballisticK * distance * massFactor);
        return Mathf.Max(def.minSpeed, v);
    }

    public static float ComputePenetration(float muzzleVelocity, BulletDefinition def, float distance)
    {
        if (def == null) return 0f;

        if (def.type == BulletType.HE || def.type == BulletType.HEAT)
            return def.penetration;

        float speedAtDist = ComputeSpeed(muzzleVelocity, def, distance);

        float keRatio = speedAtDist / Mathf.Max(0.0001f, def.referenceVelocity);

        float pen = def.penetration * Mathf.Pow(keRatio, def.deMarreK);

        return Mathf.Max(def.minPenetration, pen);
    }
    
    public static float ComputeSpeedAfterRicochet(float incomingSpeed, BulletDefinition def)
    {
        if (def == null) return incomingSpeed;

        float massFactor = Mathf.Pow(def.massKg / 6.8f, 0.1f);
        return Mathf.Max(1f, incomingSpeed * def.ricochetSpeedLoss * massFactor);
    }
}