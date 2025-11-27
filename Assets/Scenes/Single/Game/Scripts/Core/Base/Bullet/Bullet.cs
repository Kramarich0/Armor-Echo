using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections.Generic;
using Serilog;
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
        rb.includeLayers = LayerMask.GetMask("Default", "GroundLayer");

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
        cachedPenetration = Ballistics.ComputePenetration(initialMuzzleSpeed, bulletDef, travelledDistance);

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
            cachedPenetration = Ballistics.ComputePenetration(initialMuzzleSpeed, bulletDef, travelledDistance);
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

    #region Collision & physics helpers

    private readonly Dictionary<Collider, float> ignoredCollidersTimed = new Dictionary<Collider, float>();

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
        if (collision.collider?.gameObject == null) return;

        if (debugLogs) Log.Debug("[Bullet] Collided with {ColName}", collision.collider.name);

        TeamEnum? targetTeam = ColliderCache.GetCachedTeam(collision.collider);
        if (targetTeam.HasValue && targetTeam.Value == shooterTeam)
        {
            if (debugLogs) Log.Debug("[Bullet] Hit friendly -> ignore");
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Vector3 contactPoint = contact.point;

        ArmorPlate armorPlate = ColliderCache.FindBestArmorPlateOptimized(collision.collider, contactPoint);

        Vector3 bulletDirection = (lastVelocity.sqrMagnitude > MIN_VELOCITY * MIN_VELOCITY)
            ? lastVelocity.normalized
            : (rb.linearVelocity.sqrMagnitude > MIN_VELOCITY * MIN_VELOCITY ? rb.linearVelocity.normalized : transform.forward);

        if (debugLogs) Log.Debug("[Bullet] collision.collider={Col} contactPoint={Point} contactNormal={Normal} rb.velocity={Vel} lastVelocity={Last}",
                      collision.collider.name, contactPoint, contact.normal, rb.linearVelocity, lastVelocity);

        if (armorPlate != null)
        {
            float effectiveArmor = armorPlate.CalculateEffectiveArmor(bulletDirection, bulletDef, out float rawAngle);

            float speed = cachedSpeed;
            float penetration = cachedPenetration;

            var impact = Ballistics.EvaluateImpact(bulletDef, speed, penetration, effectiveArmor, rawAngle, armorPlate.armorType, out float effArmorAfter);

            if (impact.causedRicochet && !bulletDef.ignoreAngle)
            {
                if (ricochetCount < maxRicochets)
                {
                    ricochetCount++;
                    Vector3 contactNormal = armorPlate.GetSmartWorldNormal(contactPoint);
                    Vector3 reflectDir = Vector3.Reflect(bulletDirection, contactNormal).normalized;

                    float newSpeed = Ballistics.ComputeSpeedAfterRicochet(speed, bulletDef, rawAngle);
                    Vector3 newVel = reflectDir * newSpeed;

                    rb.linearVelocity = newVel;
                    lastVelocity = newVel;

                    if (debugLogs) Log.Debug("[Bullet] RICOCHET: angle={Angle:F1} deg -> reflect dir={Dir} newSpeed={NewSpeed:F1}",
                                  rawAngle, reflectDir, newSpeed);
                    return;
                }
                else
                {
                    if (debugLogs) Log.Debug("[Bullet] RICOCHET but max ricochets reached -> stop");
                    ReturnToPool();
                    return;
                }
            }

            if (impact.brokeSubcaliber)
            {
                penetration = impact.penetration;
                if (debugLogs) Log.Debug("[Bullet] Sub-caliber broken -> reduced penetration to {Pen:F1}mm", penetration);
            }

            if (debugLogs) Log.Debug("[Bullet] Computed: speed={Speed:F1}m/s pen={Pen:F1}mm effArmor={Eff:F1}mm rawAngle={Angle:F1}deg",
                          speed, penetration, effArmorAfter, rawAngle);

            if (penetration >= effArmorAfter)
            {
                if (debugLogs) Log.Debug("[Bullet] PEN: pen={Pen} effArmor={Arm}", penetration, effArmorAfter);

                float residual = Mathf.Clamp01((penetration - effArmorAfter) / Mathf.Max(1f, penetration));
                float speedAfter = speed * (0.4f + 0.6f * residual);
                Vector3 forwardVel = bulletDirection * speedAfter;

                TemporarilyIgnoreColliderOptimized(collision.collider, 0.06f);

                rb.position += bulletDirection * 0.02f;
                rb.linearVelocity = forwardVel;
                lastVelocity = forwardVel;

                IDamageable damageable = ColliderCache.GetCachedDamageable(collision.collider);
                if (damageable != null && !damagedTargets.Contains(damageable))
                {
                    damagedTargets.Add(damageable);
                    damageable.TakeDamage(bulletDef.damage, shooterName);
                }

                return;
            }
            else
            {
                if (debugLogs) Log.Debug("[Bullet] NO PEN: pen={Pen} effArmor={Arm}", penetration, effArmorAfter);

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

            IDamageable damageable = ColliderCache.GetCachedDamageable(collision.collider);
            if (damageable != null && !damagedTargets.Contains(damageable))
            {
                damagedTargets.Add(damageable);
                damageable.TakeDamage(bulletDef.damage, shooterName);
            }

            ReturnToPool();
            return;
        }
    }


    #endregion

    private void TemporarilyIgnoreColliderOptimized(Collider hitCollider, float duration)
    {
        if (hitCollider == null || col == null) return;
        Physics.IgnoreCollision(col, hitCollider, true);
        ignoredCollidersTimed[hitCollider] = duration;
    }

    private void TryApplySplashDamage(Vector3 center, BulletDefinition def)
    {
        float radius = def.splashRadius;
        if (radius <= 0f) return;

        int count = Physics.OverlapSphereNonAlloc(center, radius, ColliderCacheHelper.splashResults);
        float baseDamage = def.damage;

        for (int i = 0; i < count; i++)
        {
            var c = ColliderCacheHelper.splashResults[i];
            var dmg = ColliderCache.GetCachedDamageable(c);
            if (dmg == null) continue;
            if (damagedTargets.Contains(dmg)) continue;
            float dist = Vector3.Distance(c.transform.position, center);
            float mul = 1f - Mathf.Clamp01(dist / radius);
            dmg.TakeDamage(Mathf.CeilToInt(baseDamage * mul), shooterName);
        }
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
        if (TryGetComponent<Collider>(out var c)) ColliderCache.ClearColliderCache(c);
    }

    void OnDisable()
    {
        if (TryGetComponent<Collider>(out var c)) ColliderCache.ClearColliderCache(c);
    }

    public void SetPool(ObjectPool<Bullet> pool)
    {
        this.pool = pool;
    }
}