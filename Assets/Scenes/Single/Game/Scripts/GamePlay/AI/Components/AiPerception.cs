// пока не коммитить!
using Serilog;
using UnityEngine;
using UnityEngine.AI;

public class AIPerception
{
    readonly TankAI owner;
    private static TeamComponent[] allEnemiesCache;
    private static CapturePoint[] allCapturePointsCache;
    private float enemyCheckTimer = 0f;
    private float captureCheckTimer = 0f;


    private readonly RaycastHit[] losHits = new RaycastHit[16];
    public AIPerception(TankAI owner)
    {
        this.owner = owner;
        CacheEnemiesAndCapturePoints();
    }

    public static void InvalidateCaches()
    {
        allEnemiesCache = null;
        allCapturePointsCache = null;
    }

    private void CacheEnemiesAndCapturePoints()
    {
        if (allEnemiesCache == null || allEnemiesCache.Length == 0)
            allEnemiesCache = Object.FindObjectsByType<TeamComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allCapturePointsCache == null || allCapturePointsCache.Length == 0)
            allCapturePointsCache = Object.FindObjectsByType<CapturePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    public void UpdatePerception()
    {
        enemyCheckTimer -= Time.deltaTime;
        captureCheckTimer -= Time.deltaTime;

        if (enemyCheckTimer <= 0f)
        {
            FindNearestEnemy();
            enemyCheckTimer = 0.3f;
        }

        if (captureCheckTimer <= 0f)
        {
            FindNearestCapturePoint();
            captureCheckTimer = 0.5f;
        }
    }

    public void FindNearestEnemy()
    {
        float bestDist = float.MaxValue;
        Transform best = null;
        foreach (var tc in allEnemiesCache)
        {
            if (tc == null || tc == owner.teamComp) continue;
            if (tc.team == owner.teamComp.team) continue;

            float d = Vector3.SqrMagnitude(tc.transform.position - owner.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = tc.transform;
            }
        }
        owner.currentTarget = best;
        owner.targetRigidbody = best != null ? best.GetComponent<Rigidbody>() : null;
    }

    public void FindNearestCapturePoint()
    {
        float bestDist = float.MaxValue;
        CapturePoint best = null;

        foreach (var cp in allCapturePointsCache)
        {
            if (cp == null) continue;
            if (cp.GetControllingTeam() == owner.teamComp.team) continue;

            float d = Vector3.SqrMagnitude(cp.transform.position - owner.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = cp;
            }
        }

        owner.currentCapturePointTarget = best;
        if (owner.debugLogs && best != null)
        {
            Log.Debug("[AI] Found capture point: {CapturePointName} at distance {Distance}",
                      best.gameObject.name, Mathf.Sqrt(bestDist));
        }
    }

    public bool HasLineOfSight(Transform t)
    {
        if (t == null || owner.gunEnd == null) return false;

        Vector3 from = owner.gunEnd.position + owner.gunEnd.forward * 0.15f;
        Vector3 to = t.position + Vector3.up * 1.2f;
        Vector3 dir = to - from;
        float maxDist = Mathf.Min(owner.ShootRange, dir.magnitude);

        int hitCount = Physics.RaycastNonAlloc(from, dir.normalized, losHits, maxDist);
        if (hitCount == 0) return true;

        float closestDist = float.MaxValue;
        RaycastHit closestHit = default;
        for (int i = 0; i < hitCount; i++)
        {
            if (losHits[i].distance < closestDist)
            {
                closestDist = losHits[i].distance;
                closestHit = losHits[i];
            }
        }
        var hitTransform = closestHit.collider.transform;

        if (closestHit.collider.isTrigger) return true;
        if (hitTransform.IsChildOf(owner.transform)) return true;
        if (hitTransform == t || hitTransform.IsChildOf(t)) return true;
        if (closestHit.collider.TryGetComponent<TeamComponent>(out var team))
            return team.team != owner.teamComp.team;

        return false;
    }


    public void DrawGizmos()
    {
        if (!owner.debugGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(owner.transform.position, owner.DetectionRadius);

        if (owner.currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(owner.transform.position, owner.currentTarget.position);
        }

        if (owner.currentCapturePointTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(owner.transform.position, owner.currentCapturePointTarget.transform.position);
        }
    }
}
