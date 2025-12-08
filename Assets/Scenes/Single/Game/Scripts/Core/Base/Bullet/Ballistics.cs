using UnityEngine;
using System;
using System.Collections.Generic;

public static partial class Ballistics
{
    private const float SD_EXPONENT = 0.04f;
    private const float KE_RATIO_MIN = 0.7f;
    private const float KE_RATIO_MAX = 1.6f;
    private const float DEFAULT_RICOCHET_THRESHOLD = 20000f;

    private struct BulletTypeModifiers
    {
        public float armorMul;
        public float angleMul;
        public BulletTypeModifiers(float armorMul, float angleMul)
        {
            this.armorMul = armorMul;
            this.angleMul = angleMul;
        }
    }

    private static readonly Dictionary<BulletType, BulletTypeModifiers> typeLookup = new()
    {
        { BulletType.AP,   new BulletTypeModifiers(0.6f, 0.65f) },
        { BulletType.HVAP, new BulletTypeModifiers(0.6f, 0.65f) },
        { BulletType.APHE, new BulletTypeModifiers(0.75f, 0.8f) },
        { BulletType.APCR, new BulletTypeModifiers(0.7f, 0.7f) },
        { BulletType.APDS, new BulletTypeModifiers(0.7f, 0.7f) },
        { BulletType.HEAT, new BulletTypeModifiers(1f, 1f) },
    };

    private static readonly Dictionary<ArmorType, float> armorTypeModifiers = new()
    {
        { ArmorType.RHA, 1.0f },
        { ArmorType.Cast, 0.9f },
        { ArmorType.FaceHardened, 1.1f },
        { ArmorType.Composite, 1.2f },
        { ArmorType.HighHardness, 1.15f },
        { ArmorType.AddOn, 0.85f }
    };

    public struct ImpactResult
    {
        public float penetration;
        public bool brokeSubcaliber;
        public bool causedRicochet;
    }

    public static float ComputeSpeed(float muzzleVelocity, BulletDefinition def, float distance)
    {
        if (def == null) return 0f;
        float dragPower = 1.1f * (IsSubCaliber(def) ? 1.2f : 1f);
        float v = muzzleVelocity / Mathf.Pow(1f + def.ballisticK * distance, dragPower);
        return Mathf.Max(def.minSpeed, v);
    }

    public static float ComputePenetration(float muzzleVelocity, BulletDefinition def, float distance)
    {

        if (def == null) return 0f;
        if (def.type == BulletType.HE || def.type == BulletType.HEAT) return def.penetration;

        float v = ComputeSpeed(muzzleVelocity, def, distance);
        float keRatio = v / Mathf.Max(0.0001f, def.referenceVelocity);
        keRatio = Mathf.Clamp(keRatio, KE_RATIO_MIN, KE_RATIO_MAX);
        float caliberM = Mathf.Max(0.001f, def.caliber / 1000f);
        float sd = def.massKg / (caliberM * caliberM);
        float sdNormalized = Mathf.Log10(Mathf.Max(0.1f, sd)) - 3.0f;
        float sdFactor = 1f + sdNormalized * 0.05f;
        sdFactor = Mathf.Clamp(sdFactor, 0.95f, 1.10f);
        float pen = def.penetration * Mathf.Pow(keRatio, def.deMarreK) * sdFactor;

        if (def.minPenetration > 0f)
            pen = Mathf.Max(def.minPenetration, pen);
        Debug.Log($"ComputePen: v={v:F1}, keRatio={keRatio:F3}, sd={sd:F2}, sdNormalized={sdNormalized:F2}, sdFactor={sdFactor:F3}, deMarreK={def.deMarreK:F2}, basePenDef={def.penetration:F1}");

        return Mathf.Max(def.minPenetration, pen);
    }

    public static ImpactResult EvaluateImpact(BulletDefinition def, float currentSpeed, float basePenetration,
                                                float effectiveArmor, float rawAngleDeg,
                                                out float effectiveArmorOut)
    {
        ImpactResult res = new() { penetration = basePenetration };
        effectiveArmorOut = Mathf.Max(0.001f, effectiveArmor);

        if (def == null) return res;

        float angleToNormal = Mathf.Clamp(rawAngleDeg, 0f, 90f);

        float ratio = def.caliber / Mathf.Max(0.0001f, effectiveArmorOut);
        float overmatchFactor = 0f;
        if ((def.type == BulletType.AP || IsSubCaliber(def)) && ratio >= def.overmatchFactor)
            overmatchFactor = Mathf.Clamp01((ratio - def.overmatchFactor) / 2f);

        BulletTypeModifiers typeMod = typeLookup.ContainsKey(def.type) ? typeLookup[def.type] : new BulletTypeModifiers(0.75f, 0.75f);

        float pen = basePenetration;
        if (pen <= 0f)
        {
            pen = ComputePenetration(currentSpeed, def, 0f);
        }
        pen *= Mathf.Pow(typeMod.armorMul, overmatchFactor);

        float kineticEnergy = 0.5f * def.massKg * currentSpeed * currentSpeed;
        float ricochetEnergyThreshold = (DEFAULT_RICOCHET_THRESHOLD > 0f) ? DEFAULT_RICOCHET_THRESHOLD : DEFAULT_RICOCHET_THRESHOLD;

        float finalRicochetAngle = def.ricochetAngle;
        if (IsSubCaliber(def))
            finalRicochetAngle *= 0.6f;

        res.causedRicochet = !def.ignoreAngle && angleToNormal > finalRicochetAngle && kineticEnergy > ricochetEnergyThreshold;

        if (IsSubCaliber(def) && ShouldBreakSubcaliber(def, currentSpeed, angleToNormal))
        {
            res.brokeSubcaliber = true;
            float shatterReduction = GetShatterReductionFor(def);
            pen *= shatterReduction;
        }

        res.penetration = Mathf.Max(def.minPenetration, pen);
        effectiveArmorOut = Mathf.Max(0.001f, effectiveArmorOut);
        return res;
    }


    public static bool ShouldBreakSubcaliber(BulletDefinition def, float currentSpeed, float impactAngleDeg)
    {
        if (def == null || !IsSubCaliber(def)) return false;

        float baseChance = 0.2f + (impactAngleDeg > 65f ? Mathf.InverseLerp(65f, 85f, impactAngleDeg) * 0.5f : 0f);
        float speedRatio = currentSpeed / Mathf.Max(0.0001f, def.referenceVelocity);
        if (speedRatio < 0.6f) baseChance += (0.6f - speedRatio) * 0.6f;

        float typeMod = def.type switch
        {
            BulletType.APCR => 1f,
            BulletType.APDS => 0.6f,
            BulletType.HVAP => 0.8f,
            _ => 1f
        };

        return UnityEngine.Random.value < Mathf.Clamp01(baseChance * typeMod);
    }
    public static float ComputeSpeedAfterRicochet(float incomingSpeed, BulletDefinition def, float angleDeg, float armorThickness)
    {
        if (def == null) return incomingSpeed;

        float a = Mathf.Clamp(angleDeg, 0f, 90f) * Mathf.Deg2Rad;
        float reflectFactor = 0.2f + 0.8f * Mathf.Cos(a);

        float plasticLoss = Mathf.Clamp01(armorThickness / (armorThickness + def.caliber * 0.5f));
        float v = incomingSpeed * reflectFactor * (1f - 0.5f * plasticLoss);

        if (IsSubCaliber(def)) v *= 0.8f;

        return Mathf.Max(1f, v);
    }


    public static bool IsSubCaliber(BulletDefinition def) =>
        def != null && (def.type == BulletType.APCR || def.type == BulletType.APDS || def.type == BulletType.HVAP);


    public static float GetArmorTypeModifier(ArmorType type)
    {
        if (armorTypeModifiers.TryGetValue(type, out float mod)) return mod;
        return 1f;
    }

    private static float GetShatterReductionFor(BulletDefinition def)
    {
        if (def == null) return 1f;

        if (IsSubCaliber(def))
            return 0.5f;

        return 1f;
    }

}