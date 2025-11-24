using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections.Generic;
using Serilog;
using System.Collections;
using System.Linq;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public bool debugLogs = false;
    [Header("Bullet Settings")]
    [SerializeField] private readonly float lifeTime = 8f;
    [SerializeField] private BulletDefinition bulletDef;
    private GameObject currentVisual;
    private Rigidbody rb;
    private Collider col;
    private TeamEnum shooterTeam;
    private string shooterName;

    private float lifetimeTimer;
    private ObjectPool<Bullet> pool;
    private readonly HashSet<Collider> ignoredColliders = new();

    private bool isInPool = true;
    public event Action<Bullet> OnBulletExpired;
    private Vector3 lastVelocity = Vector3.zero;
    private const float MIN_VELOCITY = 0.001f;
    private int ricochetCount = 0;
    private readonly int maxRicochets = 1;
    private float travelledDistance = 0f;

    private readonly HashSet<IDamageable> damagedTargets = new();
    private readonly List<Collider> _removeBuffer = new(8);


    private static readonly Dictionary<Collider, CachedColliderData> colliderCache = new();
    private class CachedColliderData
    {
        public TeamEnum? team;
        public ArmorPlate[] armorPlates;
        public IDamageable damageable;
    }

    private static readonly Collider[] splashResults = new Collider[32];

    private readonly Dictionary<Collider, float> ignoredCollidersTimed = new Dictionary<Collider, float>();

    private float cachedSpeed = 0f;
    private float cachedPenetration = 0f;
    private float initialMuzzleSpeed = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rb == null || col == null)
        {
            Log.Error("[Bullet] Missing Rigidbody or Collider on bullet prefab.");
            return;
        }

        if (bulletDef == null)
        {
            Log.Warning("[Bullet] bulletDef is null on {BulletName}. Using fallback defaults.", name);
            bulletDef = ScriptableObject.CreateInstance<BulletDefinition>();
        }

        travelledDistance = 0f;

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = bulletDef.useGravity;

        col.enabled = false;
    }

    public void Initialize(Vector3 velocity, TeamEnum shooter, string shooterName = null, Collider[] ignoreWith = null, BulletDefinition definition = null)
    {
        if (rb == null || col == null)
        {
            Log.Warning("[Bullet] Initialize called but rigidbody/collider missing.");
            return;
        }

        isInPool = false;
        col.enabled = true;
        ricochetCount = 0;

        if (definition != null)
            bulletDef = definition;

        SetVisual(bulletDef.visualPrefab);

        ignoredColliders.Clear();
        ignoredCollidersTimed.Clear();

        if (ignoreWith != null)
        {
            foreach (var oc in ignoreWith)
            {
                if (oc == null) continue;
                Physics.IgnoreCollision(col, oc, true);
                ignoredColliders.Add(oc);
            }
        }

        damagedTargets.Clear();

        rb.linearVelocity = velocity;
        lastVelocity = velocity;
        rb.useGravity = bulletDef.useGravity;
        initialMuzzleSpeed = velocity.magnitude;

        col.enabled = true;

        shooterTeam = shooter;
        this.shooterName = shooterName;

        lifetimeTimer = lifeTime;

        cachedSpeed = velocity.magnitude;
        cachedPenetration = ComputePenetrationAtDistance(bulletDef);

        if (debugLogs)
        {
            Log.Debug("[Bullet] Init vel={Vel} team={Team} shooter={Shooter} dmg={Dmg} ignores={IgnoreCnt}",
              velocity.magnitude, shooter, shooterName, bulletDef.damage, ignoreWith?.Length ?? 0);
        }
    }

    void FixedUpdate()
    {
        if (isInPool) return;

        UpdateTimedIgnoredColliders();

        if (rb != null)
        {
            cachedSpeed = rb.linearVelocity.magnitude;
            travelledDistance += cachedSpeed * Time.fixedDeltaTime;
        }

        if (Time.frameCount % 3 == 0)
        {
            cachedPenetration = ComputePenetrationAtDistance(bulletDef);
        }

        lifetimeTimer -= Time.fixedDeltaTime;
        if (lifetimeTimer <= 0f)
            ReturnToPool();

        if (rb != null)
        {
            if (rb.linearVelocity.sqrMagnitude > MIN_VELOCITY * MIN_VELOCITY)
                lastVelocity = rb.linearVelocity;
        }
    }

    private void UpdateTimedIgnoredColliders()
    {
        _removeBuffer.Clear();

        foreach (var kvp in ignoredCollidersTimed)
        {
            float newTime = kvp.Value - Time.fixedDeltaTime;

            if (newTime <= 0f)
            {
                if (kvp.Key != null && col != null)
                    Physics.IgnoreCollision(col, kvp.Key, false);

                _removeBuffer.Add(kvp.Key);
            }
            else
            {
                // next
            }
        }

        for (int i = 0; i < _removeBuffer.Count; i++)
        {
            ignoredCollidersTimed.Remove(_removeBuffer[i]);
        }

        foreach (var key in ignoredCollidersTimed.Keys.ToList())
        {
            ignoredCollidersTimed[key] -= Time.fixedDeltaTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isInPool || collision == null) return;
        if (collision.collider.gameObject == null) return;

        if (debugLogs) Log.Debug("[Bullet] Collided with {ColName}", collision.collider.name);

        TeamEnum? targetTeam = GetCachedTeam(collision.collider);

        if (targetTeam.HasValue && targetTeam.Value == shooterTeam)
        {
            if (debugLogs) Log.Debug("[Bullet] Hit friendly -> ignore");
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Vector3 contactPoint = contact.point;
        // DebugDumpColliderHierarchy(collision.collider, contactPoint);
        ArmorPlate armorPlate = FindBestArmorPlateOptimized(collision, contactPoint);

        Vector3 bulletDirection = (lastVelocity.sqrMagnitude > MIN_VELOCITY * MIN_VELOCITY)
            ? lastVelocity.normalized
            : (rb.linearVelocity.sqrMagnitude > MIN_VELOCITY * MIN_VELOCITY ? rb.linearVelocity.normalized : transform.forward);


        if (debugLogs) Log.Debug("[Bullet] collision.collider={Col} contactPoint={Point} contactNormal={Normal} rb.velocity={Vel} lastVelocity={Last}",
                      collision.collider.name,
                      contactPoint,
                      contact.normal,
                      rb.linearVelocity,
                      lastVelocity);

        if (armorPlate != null)
        {
            float effectiveArmor = armorPlate.CalculateEffectiveArmor(bulletDirection, bulletDef, out float rawAngle);
            float speed = cachedSpeed;
            float penetration = cachedPenetration;

            if (bulletDef.caliber >= armorPlate.thickness * bulletDef.overmatchFactor)
            {
                float ratio = bulletDef.caliber / armorPlate.thickness;
                float overmatchFactor = Mathf.Clamp01((ratio - bulletDef.overmatchFactor) / 2f);

                float kineticFactor = 1f;
                if (bulletDef.type == BulletType.AP || bulletDef.type == BulletType.HVAP ||
                    bulletDef.type == BulletType.APHE || bulletDef.type == BulletType.APCR || bulletDef.type == BulletType.APDS)
                {
                    float v0 = initialMuzzleSpeed > 0f ? initialMuzzleSpeed : 0f;
                    kineticFactor = Mathf.Clamp01(speed / Mathf.Max(0.0001f, v0));
                }

                switch (bulletDef.type)
                {
                    case BulletType.AP:
                    case BulletType.HVAP:
                        effectiveArmor *= Mathf.Pow(0.6f, overmatchFactor) * kineticFactor;
                        rawAngle *= Mathf.Pow(0.65f, overmatchFactor);
                        break;

                    case BulletType.APHE:
                    case BulletType.APCR:
                    case BulletType.APDS:
                        effectiveArmor *= Mathf.Pow(0.7f, overmatchFactor) * kineticFactor;
                        rawAngle *= Mathf.Pow(0.7f, overmatchFactor);

                        if ((bulletDef.type == BulletType.APCR || bulletDef.type == BulletType.APDS) && rawAngle > 70f)
                        {
                            if (Random.value < 0.2f)
                            {
                                penetration *= 0.5f;
                                if (debugLogs) Log.Debug("[Bullet] Sub-caliber broken due to high impact angle");
                            }
                        }
                        break;

                    case BulletType.HEAT:
                        effectiveArmor *= Mathf.Lerp(1f, 0.85f, overmatchFactor);
                        rawAngle *= Mathf.Lerp(1f, 0.8f, overmatchFactor);
                        break;

                    default:
                        effectiveArmor *= Mathf.Pow(0.75f, overmatchFactor);
                        rawAngle *= Mathf.Pow(0.75f, overmatchFactor);
                        break;
                }

                effectiveArmor = Mathf.Max(effectiveArmor, armorPlate.thickness * 0.25f);
                rawAngle = Mathf.Clamp(rawAngle, 0f, 90f);

                if (debugLogs) Log.Debug("[Bullet] Realistic Overmatch + speed applied: type={Type} newEffArmor={Eff:F1}mm newAngle={Angle:F1}deg ratio={Ratio:F2} speedFactor={SpeedFactor:F2}",
                             bulletDef.type, effectiveArmor, rawAngle, ratio, kineticFactor);
            }

            if (debugLogs) Log.Debug("[Bullet] Computed: speed={Speed:F1}m/s pen={Pen:F1}mm effArmor={Eff:F1}mm rawAngle={Angle:F1}deg",
                          speed, penetration, effectiveArmor, rawAngle);

            if (!bulletDef.ignoreAngle && rawAngle > bulletDef.ricochetAngle)
            {
                if (ricochetCount < maxRicochets)
                {
                    ricochetCount++;
                    Vector3 contactNormal = armorPlate.GetSmartWorldNormal(contactPoint);
                    Vector3 reflectDir = Vector3.Reflect(bulletDirection, contactNormal).normalized;
                    float newSpeed = Mathf.Max(1f, speed * bulletDef.ricochetSpeedLoss);
                    Vector3 newVel = reflectDir * newSpeed;

                    rb.linearVelocity = newVel;
                    lastVelocity = newVel;

                    if (debugLogs) Log.Debug("[Bullet] RICOCHET: angle={Angle:F1} deg -> reflect dir={Dir} newSpeed={NewSpeed:F1}", rawAngle, reflectDir, newSpeed);
                    return;
                }
                else
                {
                    if (debugLogs) Log.Debug("[Bullet] RICOCHET but max ricochets reached -> stop");
                    ReturnToPool();
                    return;
                }
            }

            if (penetration >= effectiveArmor)
            {
                if (debugLogs) Log.Debug("[Bullet] PEN: pen={Pen} effArmor={Arm}", penetration, effectiveArmor);

                float residual = Mathf.Clamp01((penetration - effectiveArmor) / Mathf.Max(1f, penetration));
                float speedAfter = speed * (0.4f + 0.6f * residual);
                Vector3 forwardVel = bulletDirection * speedAfter;

                TemporarilyIgnoreColliderOptimized(collision.collider, 0.06f);

                rb.position += bulletDirection * 0.02f;
                rb.linearVelocity = forwardVel;
                lastVelocity = forwardVel;

                IDamageable damageable = GetCachedDamageable(collision.collider);
                if (damageable != null)
                {
                    if (!damagedTargets.Contains(damageable))
                    {
                        damagedTargets.Add(damageable);
                        damageable.TakeDamage(bulletDef.damage, shooterName);
                    }
                    else
                    {
                        if (debugLogs) Log.Debug("[Bullet] Skipped duplicate damage for same target.");
                    }
                }

                return;
            }
            else
            {
                if (debugLogs) Log.Debug("[Bullet] NO PEN: pen={Pen} effArmor={Arm}", penetration, effectiveArmor);

                if (bulletDef.type == BulletType.HE)
                {
                    TryApplySplashDamage(contactPoint, bulletDef);
                }

                ReturnToPool();
                return;
            }

        }

        else
        {
            if (debugLogs) Log.Debug("[Bullet] No ArmorPlate found for collider {Col}", collision.collider.name);

            IDamageable damageable = GetCachedDamageable(collision.collider);
            if (damageable != null)
            {
                if (!damagedTargets.Contains(damageable))
                {
                    damagedTargets.Add(damageable);
                    damageable.TakeDamage(bulletDef.damage, shooterName);
                }
                else
                {
                    if (debugLogs) Log.Debug("[Bullet] Skipped duplicate damage for same target (no armorPlate).");
                }
            }

            ReturnToPool();
            return;
        }

    }

    private void DebugDumpColliderHierarchy(Collider collider, Vector3 contactPoint)
    {
        if (collider == null) { if (debugLogs) Log.Debug("[Bullet][DBG] DebugDump: collider == null"); return; }

        var go = collider.gameObject;
        if (debugLogs) Log.Debug("[Bullet][DBG] Collider GO: {Name} pos={Pos} root={Root}", go.name, go.transform.position, go.transform.root.name);

        var comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            if (debugLogs) Log.Debug("[Bullet][DBG] Component on {GO}: {Type}", go.name, c.GetType().Name);
        }

        var t = go.transform;
        int depth = 0;
        while (t != null)
        {
            if (debugLogs) Log.Debug("[Bullet][DBG] Parent[{Depth}] {Name} (pos={Pos})", depth, t.name, t.position);
            if (t.TryGetComponent<ArmorPlate>(out var plate))
                if (debugLogs) Log.Debug("[Bullet][DBG] Found ArmorPlate on transform {Name}", t.name);
            t = t.parent;
            depth++;
        }

        var hits = Physics.OverlapSphere(contactPoint, 0.3f);
        for (int i = 0; i < hits.Length; i++)
        {
            var p = hits[i].GetComponentInParent<ArmorPlate>();
            if (p != null)
                if (debugLogs) Log.Debug("[Bullet][DBG] OverlapSphere found ArmorPlate {Plate} on collider {C}", p.name, hits[i].name);
        }
    }


    private void TemporarilyIgnoreColliderOptimized(Collider hitCollider, float duration)
    {
        if (hitCollider == null || col == null) return;
        Physics.IgnoreCollision(col, hitCollider, true);
        ignoredCollidersTimed[hitCollider] = duration;
    }

    private ArmorPlate FindBestArmorPlateOptimized(Collision collision, Vector3 contactPoint)
    {
        Collider collider = collision.collider;
        if (collider == null) return null;

        if (colliderCache.TryGetValue(collider, out CachedColliderData cachedData))
        {
            if (cachedData.armorPlates != null && cachedData.armorPlates.Length > 0)
            {
                if (cachedData.armorPlates.Length == 1) return cachedData.armorPlates[0];
                ArmorPlate best = null; float bestDist = float.MaxValue;
                foreach (var p in cachedData.armorPlates)
                {
                    if (p == null) continue;
                    float d = (contactPoint - p.GetPlateWorldCenter()).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = p; }
                }
                return best;
            }
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
            if (debugLogs) Log.Debug("[Bullet] FindBestArmorPlateOptimized: no plates found for collider {C}. Not caching negative result.", collider.name);
            return null;
        }

        var result = dedup.ToArray();
        var cache = new CachedColliderData { armorPlates = result };
        colliderCache[collider] = cache;

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


    private TeamEnum? GetCachedTeam(Collider collider)
    {
        if (!colliderCache.TryGetValue(collider, out CachedColliderData cachedData))
        {
            cachedData = new CachedColliderData();


            if (collider.GetComponentInParent<ITeamProvider>() is ITeamProvider tp)
                cachedData.team = tp.Team;
            else if (collider.GetComponentInParent<TeamComponent>() is TeamComponent tc)
                cachedData.team = tc.team;

            colliderCache[collider] = cachedData;
        }

        return cachedData.team;
    }

    private IDamageable GetCachedDamageable(Collider collider)
    {
        if (collider == null) return null;

        if (colliderCache.TryGetValue(collider, out CachedColliderData cached) && cached.damageable != null)
            return cached.damageable;
        if (collider.GetComponent(typeof(IDamageable)) is not IDamageable dmg)
        {
            dmg = collider.GetComponentInParent(typeof(IDamageable)) as IDamageable;
        }
        if (dmg == null)
        {
            dmg = collider.GetComponentInChildren(typeof(IDamageable)) as IDamageable;
        }

        if (dmg == null && collider.attachedRigidbody != null)
        {
            dmg = collider.attachedRigidbody.GetComponentInParent(typeof(IDamageable)) as IDamageable
                  ?? collider.attachedRigidbody.GetComponentInChildren(typeof(IDamageable)) as IDamageable;
        }

        if (dmg != null)
        {
            if (!colliderCache.TryGetValue(collider, out cached))
                cached = new CachedColliderData();
            cached.damageable = dmg;
            colliderCache[collider] = cached;
        }
        else
        {
            if (debugLogs) Log.Debug("[Bullet] GetCachedDamageable: no IDamageable found for collider {C}", collider.name);
        }

        return dmg;
    }

    public static void ClearColliderCache(Collider collider)
    {
        if (collider != null)
            colliderCache.Remove(collider);
    }


    private void TryApplySplashDamage(Vector3 center, BulletDefinition def)
    {
        float radius = def.splashRadius;
        if (radius <= 0f) return;

        int count = Physics.OverlapSphereNonAlloc(center, radius, splashResults);
        float baseDamage = def.damage;

        for (int i = 0; i < count; i++)
        {
            var c = splashResults[i];
            var dmg = GetCachedDamageable(c);
            if (dmg == null) continue;
            if (damagedTargets.Contains(dmg)) continue;
            float dist = Vector3.Distance(c.transform.position, center);
            float mul = 1f - Mathf.Clamp01(dist / radius);
            dmg.TakeDamage(Mathf.CeilToInt(baseDamage * mul), shooterName);
        }
    }

    private float ComputeSpeed(float distance)
    {
        float massFactor = 1f / Mathf.Sqrt(Mathf.Max(bulletDef.massKg, 0.01f));
        float v = initialMuzzleSpeed * Mathf.Exp(-bulletDef.ballisticK * distance * massFactor);

        return Mathf.Max(bulletDef.minSpeed, v);
    }

    private float ComputePenetrationAtDistance(BulletDefinition def)
    {
        if (def == null) return 0f;

        if (def.type == BulletType.HE || def.type == BulletType.HEAT)
            return def.penetration;

        float v0 = initialMuzzleSpeed > 0f ? initialMuzzleSpeed : 0f;
        float vDist = ComputeSpeed(travelledDistance);

        float keRatio = (0.5f * def.massKg * vDist * vDist) / Mathf.Max(0.0001f, 0.5f * def.massKg * v0 * v0);
        float pen = def.penetration * Mathf.Pow(Mathf.Clamp01(keRatio), def.deMarreK);

        return Mathf.Max(def.minPenetration, pen);
    }


    public void CleanupBeforeSpawn()
    {
        foreach (var oc in ignoredColliders)
            if (oc != null) Physics.IgnoreCollision(col, oc, false);

        ignoredColliders.Clear();
        ignoredCollidersTimed.Clear();

        travelledDistance = 0f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        col.enabled = false;

        lifetimeTimer = 0f;
        // isInPool = false;

        damagedTargets.Clear();

        cachedSpeed = 0f;
        cachedPenetration = 0f;
    }

    private void ReturnToPool()
    {
        if (isInPool) return;
        isInPool = true;
        if (debugLogs) Log.Debug("Пуля вернулась в пул!");
        col.enabled = false;

        foreach (var oc in ignoredColliders)
            if (oc != null) Physics.IgnoreCollision(col, oc, false);

        ignoredColliders.Clear();
        ignoredCollidersTimed.Clear();
        damagedTargets.Clear();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        pool?.Release(this);
        OnBulletExpired?.Invoke(this);
    }

    private void SetVisual(GameObject visualPrefab)
    {

        if (currentVisual != null)
        {
            string currentName = currentVisual.name.Replace("(Clone)", "").Trim();
            string targetName = visualPrefab.name.Replace("(Clone)", "").Trim();

            if (currentName == targetName)
                return;

            Destroy(currentVisual);
        }

        if (visualPrefab != null)
        {
            currentVisual = Instantiate(visualPrefab, transform);
            currentVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    void OnEnable()
    {
        if (TryGetComponent<Collider>(out var c)) ClearColliderCache(c);
    }

    void OnDisable()
    {
        if (TryGetComponent<Collider>(out var c)) ClearColliderCache(c);
    }

    public void SetPool(ObjectPool<Bullet> pool)
    {
        this.pool = pool;
    }
}
