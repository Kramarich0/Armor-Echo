using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public static class ColliderCache
{
    public class CachedColliderData
    {
        public TeamEnum? team;
        public ArmorPlate[] armorPlates;
        public IDamageable damageable;
    }

    private static Collider[] overlapBuffer = new Collider[32];
    private static readonly Dictionary<Collider, CachedColliderData> cache = new();


    public static TeamEnum? GetCachedTeam(Collider collider)
    {
        if (collider == null) return null;
        if (cache.TryGetValue(collider, out var cd) && cd.team.HasValue) return cd.team;


        var data = cd ?? new CachedColliderData();


        if (collider.GetComponentInParent<ITeamProvider>() is ITeamProvider tp)
            data.team = tp.Team;
        else if (collider.GetComponentInParent<TeamComponent>() is TeamComponent tc)
            data.team = tc.team;


        cache[collider] = data;
        return data.team;
    }


    public static IDamageable GetCachedDamageable(Collider collider)
    {
        if (collider == null) return null;
        if (cache.TryGetValue(collider, out var cd) && cd.damageable != null) return cd.damageable;


        IDamageable dmg = null;
        dmg = collider.GetComponent(typeof(IDamageable)) as IDamageable;
        dmg ??= collider.GetComponentInParent(typeof(IDamageable)) as IDamageable;
        dmg ??= collider.GetComponentInChildren(typeof(IDamageable)) as IDamageable;
        if (dmg == null && collider.attachedRigidbody != null)
            dmg = collider.attachedRigidbody.GetComponentInParent(typeof(IDamageable)) as IDamageable
            ?? collider.attachedRigidbody.GetComponentInChildren(typeof(IDamageable)) as IDamageable;

        if (!cache.TryGetValue(collider, out cd)) cd = new CachedColliderData();
        cd.damageable = dmg;
        cache[collider] = cd;
        return dmg;
    }
    public static ArmorPlate FindBestArmorPlateOptimized(Collider collider, Vector3 contactPoint)
    {
        if (collider == null) return null;

        if (cache.TryGetValue(collider, out var cd) && cd.armorPlates != null && cd.armorPlates.Length > 0)
        {
            if (cd.armorPlates.Length == 1) return cd.armorPlates[0];

            ArmorPlate closest = null;
            float minDist = float.MaxValue;
            foreach (var p in cd.armorPlates)
            {
                if (p == null) continue;
                float dist = (contactPoint - p.GetPlateWorldCenter()).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = p;
                }
            }
            return closest;
        }

        var dedup = new HashSet<ArmorPlate>();

        if (collider.TryGetComponent<ArmorPlate>(out var selfPlate)) dedup.Add(selfPlate);
        foreach (var p in collider.GetComponentsInChildren<ArmorPlate>(true)) dedup.Add(p);
        foreach (var p in collider.GetComponentsInParent<ArmorPlate>(true)) dedup.Add(p);

        if (collider.attachedRigidbody != null)
        {
            foreach (var p in collider.attachedRigidbody.GetComponentsInChildren<ArmorPlate>(true))
                dedup.Add(p);
        }

        foreach (var p in collider.transform.root.GetComponentsInChildren<ArmorPlate>(true))
            dedup.Add(p);

        if (dedup.Count == 0)
        {
            const float probeRadius = 0.3f;
            int hitCount = Physics.OverlapSphereNonAlloc(contactPoint, probeRadius, overlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                var p = overlapBuffer[i].GetComponentInParent<ArmorPlate>();
                if (p != null) dedup.Add(p);
            }
        }

        if (dedup.Count == 0) return null;

        if (!cache.TryGetValue(collider, out cd)) cd = new CachedColliderData();
        cd.armorPlates = dedup.ToArray();
        cache[collider] = cd;

        ArmorPlate bestPlate = null;
        float bestDist = float.MaxValue;
        foreach (var p in dedup)
        {
            float d = (contactPoint - p.GetPlateWorldCenter()).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                bestPlate = p;
            }
        }

        return bestPlate;
    }

    public static void ClearColliderCache(Collider collider)
    {
        if (collider != null) cache.Remove(collider);
    }
}