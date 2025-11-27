using UnityEngine;
using System;
using System.Collections.Generic;

public static partial class Ballistics
{
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

    private struct NoseProperties
    {
        public float ricochetMod;
        public float angleSlope;
        public float shatterReduction;
        public float fragility;

        public NoseProperties(float ricochetMod, float angleSlope, float shatterReduction, float fragility)
        {
            this.ricochetMod = ricochetMod;
            this.angleSlope = angleSlope;
            this.shatterReduction = shatterReduction;
            this.fragility = fragility;
        }
    }

    private static readonly Dictionary<NoseType, NoseProperties> noseLookup = new()
    {
        { NoseType.Sharp,        new NoseProperties(1.3f, 1f/70f, 0.45f, 1.2f) },
        { NoseType.Blunt,        new NoseProperties(0.6f, 1f/120f, 0.8f, 0.8f) },
        { NoseType.BallisticCap, new NoseProperties(0.85f, 1f/100f, 0.65f, 0.95f) },
        { NoseType.APC,          new NoseProperties(0.5f, 1f/150f, 0.75f, 0.7f) },
        { NoseType.APCBC,        new NoseProperties(0.35f, 1f/170f, 0.85f, 0.6f) },
        { NoseType.SabotCore,    new NoseProperties(1.4f, 1f/60f, 0.5f, 1.4f) },
    };

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
        float massFactor = Mathf.Pow(Mathf.Max(def.massKg, 0.001f), 0.25f);
        float caliberFactor = Mathf.Pow(Mathf.Max(def.caliber, 1f), 0.1f);

        float pen = def.penetration * Mathf.Pow(keRatio, def.deMarreK) * massFactor * caliberFactor;
        return Mathf.Max(def.minPenetration, pen);
    }

    public static ImpactResult EvaluateImpact(BulletDefinition def, float currentSpeed, float basePenetration,
                                           float armorThickness, float rawAngleDeg, ArmorType armorType, out float effectiveArmorOut)
    {
        ImpactResult res = new() { penetration = basePenetration };
        effectiveArmorOut = armorThickness;
        if (def == null) return res;

        float armorTypeMod = GetArmorTypeModifier(armorType);
        effectiveArmorOut *= armorTypeMod;

        float ratio = def.caliber / Mathf.Max(0.0001f, effectiveArmorOut);
        float overmatchFactor = (ratio >= def.overmatchFactor) ? Mathf.Clamp01((ratio - def.overmatchFactor) / 2f) : 0f;
        float kineticFactor = (def.referenceVelocity > 0f) ? Mathf.Clamp01(currentSpeed / def.referenceVelocity) : 1f;
        float angle = Mathf.Clamp(rawAngleDeg, 0f, 90f);

        NoseProperties nose = GetNoseProperties(def);
        BulletTypeModifiers typeMod = typeLookup.ContainsKey(def.type) ? typeLookup[def.type] : new BulletTypeModifiers(0.75f, 0.75f);

        effectiveArmorOut *= Mathf.Pow(typeMod.armorMul, overmatchFactor) * kineticFactor;
        angle *= Mathf.Pow(typeMod.angleMul, overmatchFactor);
        effectiveArmorOut = Mathf.Max(effectiveArmorOut, armorThickness * 0.25f);

        if (def.type == BulletType.HEAT)
        {
            effectiveArmorOut *= Mathf.Lerp(1f, 0.85f, overmatchFactor);
            angle *= Mathf.Lerp(1f, 0.8f, overmatchFactor);
        }

        float finalRicochetAngle = def.ricochetAngle * nose.ricochetMod;
        float anglePenalty = Mathf.Clamp(1f + Mathf.Clamp(angle, 0f, 85f) * nose.angleSlope, 1f, 5f);
        effectiveArmorOut *= anglePenalty;

        if (!def.ignoreAngle && angle > finalRicochetAngle)
            res.causedRicochet = true;

        if (IsSubCaliber(def) && ShouldBreakSubcaliber(def, currentSpeed, angle))
        {
            res.brokeSubcaliber = true;
            res.penetration *= nose.shatterReduction;
        }

        res.penetration = Mathf.Max(def.minPenetration, res.penetration);
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

    public static float ComputeSpeedAfterRicochet(float incomingSpeed, BulletDefinition def, float angleDeg)
    {
        if (def == null) return incomingSpeed;
        float a = Mathf.Clamp(angleDeg, 0f, 90f) * Mathf.Deg2Rad;
        float factor = 0.2f + 0.8f * Mathf.Cos(a);
        float v = incomingSpeed * factor * Mathf.Pow(Mathf.Max(def.massKg, 0.001f), 0.1f);
        if (IsSubCaliber(def)) v *= 0.8f;
        return Mathf.Max(1f, v);
    }

    public static bool IsSubCaliber(BulletDefinition def) =>
        def != null && (def.type == BulletType.APCR || def.type == BulletType.APDS || def.type == BulletType.HVAP);

    private static NoseProperties GetNoseProperties(BulletDefinition def)
    {
        if (def == null) return noseLookup[NoseType.Sharp];
        if (noseLookup.TryGetValue(def.noseType, out var props)) return props;
        if (IsSubCaliber(def)) return noseLookup[NoseType.SabotCore];
        if (def.type == BulletType.APHE) return noseLookup[NoseType.BallisticCap];
        return noseLookup[NoseType.APCBC];
    }

    public static float GetArmorTypeModifier(ArmorType type)
    {
        if (armorTypeModifiers.TryGetValue(type, out float mod)) return mod;
        return 1f;
    }
}