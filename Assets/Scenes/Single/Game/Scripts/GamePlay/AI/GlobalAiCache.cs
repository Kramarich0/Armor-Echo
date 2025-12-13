using UnityEngine;

public static class GlobalAICache
{
    private static TeamComponent[] _enemies;
    private static CapturePoint[] _capturePoints;
    private static float _lastRefresh = 0f;
    private const float RefreshInterval = 0.5f; 

    public static TeamComponent[] GetAllEnemies()
    {
        RefreshIfNeeded();
        return _enemies ?? (_enemies = FindEnemies());
    }

    public static CapturePoint[] GetAllCapturePoints()
    {
        RefreshIfNeeded();
        return _capturePoints ??= FindCapturePoints();
    }

    private static void RefreshIfNeeded()
    {
        if (Time.time - _lastRefresh > RefreshInterval)
        {
            _enemies = null;
            _capturePoints = null;
            _lastRefresh = Time.time;
        }
    }

    private static TeamComponent[] FindEnemies()
    {
        return Object.FindObjectsByType<TeamComponent>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
    }

    private static CapturePoint[] FindCapturePoints()
    {
        return Object.FindObjectsByType<CapturePoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
    }
}