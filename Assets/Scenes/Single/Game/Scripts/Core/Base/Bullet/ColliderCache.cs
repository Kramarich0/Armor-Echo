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
        if (dmg == null) dmg = collider.GetComponentInParent(typeof(IDamageable)) as IDamageable;
        if (dmg == null) dmg = collider.GetComponentInChildren(typeof(IDamageable)) as IDamageable;
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
            ArmorPlate best = null; float bestDist = float.MaxValue;
            foreach (var p in cd.armorPlates)
            {
                if (p == null) continue;
                float d = (contactPoint - p.GetPlateWorldCenter()).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }


        var found = new List<ArmorPlate>();


        if (collider.TryGetComponent<ArmorPlate>(out var selfPlate)) found.Add(selfPlate);
        var children = collider.GetComponentsInChildren<ArmorPlate>(true);
        if (children != null && children.Length > 0) found.AddRange(children);
        var parents = collider.GetComponentsInParent<ArmorPlate>(true);
        if (parents != null && parents.Length > 0) found.AddRange(parents);


        if (collider.attachedRigidbody != null)
        {
            var rbPlates = collider.attachedRigidbody.GetComponentsInChildren<ArmorPlate>(true);
            if (rbPlates != null && rbPlates.Length > 0) found.AddRange(rbPlates);
        }


        var rootPlates = collider.transform.root.GetComponentsInChildren<ArmorPlate>(true);
        if (rootPlates != null && rootPlates.Length > 0) found.AddRange(rootPlates);


        var dedup = new List<ArmorPlate>();
        foreach (var p in found) if (p != null && !dedup.Contains(p)) dedup.Add(p);


        if (dedup.Count == 0)
        {
            const float probeRadius = 0.3f;
            Collider[] hits = Physics.OverlapSphere(contactPoint, probeRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var p = hits[i].GetComponentInParent<ArmorPlate>();
                if (p != null && !dedup.Contains(p)) dedup.Add(p);
            }
        }


        if (dedup.Count == 0)
        {
            return null;
        }


        var result = dedup.ToArray();
        var cacheEntry = new CachedColliderData { armorPlates = result };
        cache[collider] = cacheEntry;


        if (result.Length == 1) return result[0];


        ArmorPlate bestPlate = null; float bestD = float.MaxValue;
        foreach (var p in result)
        {
            if (p == null) continue;
            float d = (contactPoint - p.GetPlateWorldCenter()).sqrMagnitude;
            if (d < bestD) { bestD = d; bestPlate = p; }
        }
        return bestPlate;
    }


    public static void ClearColliderCache(Collider collider)
    {
        if (collider != null) cache.Remove(collider);
    }
}