using System.Linq;
using UnityEngine;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance { get; private set; }

    private BulletPool[] pools = new BulletPool[0];

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        RefreshPools();
    }

    public void RefreshPools()
    {
        pools = FindObjectsByType<BulletPool>(FindObjectsSortMode.None);
    }

    public BulletPool GetPoolFor(BulletDefinition def)
    {
        if (pools == null || pools.Length == 0)
        {
            RefreshPools();
            if (pools == null || pools.Length == 0) return null;
        }

        foreach (var pool in pools)
        {
            if (pool != null && pool.HandlesDefinition(def))
                return pool;
        }

        string bulletName = def?.bulletName ?? "";
        foreach (var pool in pools)
        {
            if (pool != null &&
                pool.gameObject.name.IndexOf(bulletName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return pool;
        }

        // Fallback
        foreach (var pool in pools)
            if (pool != null) return pool;

        return null;
    }
}
