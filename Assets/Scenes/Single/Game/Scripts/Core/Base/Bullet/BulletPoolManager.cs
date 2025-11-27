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

        var byHandles = pools.FirstOrDefault(p => p.HandlesDefinition(def));
        if (byHandles != null) return byHandles;

        var byName = pools.FirstOrDefault(p =>
            p != null && p.GetComponent<BulletPool>() != null &&
            p.gameObject.name.IndexOf(def.bulletName ?? "", System.StringComparison.OrdinalIgnoreCase) >= 0
        );
        if (byName != null) return byName;

        return pools.FirstOrDefault();
    }
}
