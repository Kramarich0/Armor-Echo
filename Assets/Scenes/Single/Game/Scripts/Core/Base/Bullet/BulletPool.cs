using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPool : MonoBehaviour
{
    [SerializeField] private Bullet prefab;
    [SerializeField] private int initialSize = 20;

    public BulletDefinition[] supportedDefinitions;
    public string poolName;

    private ObjectPool<Bullet> pool;
    public bool debugLogs = false;

    void Awake()
    {
        pool = new ObjectPool<Bullet>(
            createFunc: CreateBullet,
            actionOnGet: OnBulletGet,
            actionOnRelease: OnBulletRelease,
            actionOnDestroy: Destroy,
            collectionCheck: true,
            defaultCapacity: initialSize,
            maxSize: 500
        );

        var prewarm = new List<Bullet>();
        for (int i = 0; i < initialSize; i++)
        {
            prewarm.Add(pool.Get());
        }
        foreach (var bullet in prewarm)
        {
            pool.Release(bullet);
        }
    }

    private Bullet CreateBullet()
    {
        var bullet = Instantiate(prefab, transform);
        bullet.gameObject.SetActive(false);
        bullet.SetPool(pool);
        return bullet;
    }

    private void OnBulletGet(Bullet bullet)
    {
        bullet.CleanupBeforeSpawn();
    }

    private void OnBulletRelease(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    public Bullet SpawnBullet(Vector3 position, Vector3 velocity, BulletDefinition definition = null, TeamEnum team = TeamEnum.Neutral, string shooterName = null, Collider[] ignoreColliders = null)
    {
        var bullet = pool.Get();
        bullet.transform.SetPositionAndRotation(position, Quaternion.LookRotation(velocity.normalized));

        bullet.Initialize(velocity, team, shooterName, ignoreColliders, definition);

        bullet.gameObject.SetActive(true);
        if (debugLogs) Log.Debug("[Pool] Spawn at {Position} vel={Velocity} team={Team} def={Def}",
                    position, velocity.magnitude, team, definition != null ? definition.name : "default");

        return bullet;
    }

    public bool HandlesDefinition(BulletDefinition def)
    {
        if (def == null) return false;

        if (supportedDefinitions != null && supportedDefinitions.Any(d => d == def)) return true;

        if (prefab != null && def.visualPrefab != null)
        {
            if (string.Equals(prefab.gameObject.name, def.visualPrefab.name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (prefab != null && !string.IsNullOrEmpty(def.bulletName))
        {
            if (prefab.gameObject.name.IndexOf(def.bulletName, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

}
