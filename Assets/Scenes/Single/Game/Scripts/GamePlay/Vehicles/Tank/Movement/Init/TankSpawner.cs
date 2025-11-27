using System.Linq;
using UnityEngine;

public class TankSpawner : MonoBehaviour
{
    [Header("Prefabs & spawn")]
    public Transform spawnPoint;

    [Header("Global HUD/Scene refs (optional)")]
    public Canvas hudCanvas;
    public string mainCameraTag = "MainCamera";
    public string reloadDisplayTag = "ReloadDisplay";

    public GameObject SpawnPlayerTank(TankDefinition def)
    {
        if (def == null || def.tankPrefab == null)
        {
            Debug.LogError("TankSpawner: def или def.tankPrefab не задан!");
            return null;
        }

        var prefab = def.tankPrefab;

        GameObject instance = Instantiate(
            prefab,
            spawnPoint != null ? spawnPoint.position : Vector3.zero,
            spawnPoint != null ? spawnPoint.rotation : Quaternion.identity
        );

        SetupTankInstance(instance, def);
        return instance;
    }



    void SetupTankInstance(GameObject instance, TankDefinition def)
    {
        if (instance == null) return;
        SetupCameras(instance);

        Tank tank = instance.GetComponentInChildren<Tank>();
        if (tank == null)
        {
            Debug.LogError("Spawned prefab не содержит Tank!");
            return;
        }

        tank.definition = def;

        if (tank.rb == null)
            tank.rb = instance.GetComponentInChildren<Rigidbody>();

        if (tank.leftTrack == null)
        {
            var leftTransform = FindChildByNameContains(instance.transform, "left");
            if (leftTransform != null)
            {
                var wheels = leftTransform.GetComponentsInChildren<WheelCollider>();
                if (wheels != null && wheels.Length > 0)
                    tank.leftTrack = new TankTrack { wheels = wheels };
            }
        }

        if (tank.rightTrack == null)
        {
            var rightTransform = FindChildByNameContains(instance.transform, "right");
            if (rightTransform != null)
            {
                var wheels = rightTransform.GetComponentsInChildren<WheelCollider>();
                if (wheels != null && wheels.Length > 0)
                    tank.rightTrack = new TankTrack { wheels = wheels };
            }
        }

        if (tank.speedDisplay == null)
        {
            tank.speedDisplay = instance.GetComponentInChildren<SpeedDisplay>();
            if (tank.speedDisplay == null && hudCanvas != null)
                tank.speedDisplay = hudCanvas.GetComponentInChildren<SpeedDisplay>();
        }

        Transform gunEndT = FindChildRecursive(instance.transform, "GunEnd");
        var ts = instance.GetComponentInChildren<TankShoot>();
        if (gunEndT == null && ts != null && ts.gunEnd != null)
            gunEndT = ts.gunEnd;

        CrosshairAim crossAim = null;
        if (hudCanvas != null)
            crossAim = hudCanvas.GetComponentInChildren<CrosshairAim>();
        else
            crossAim = FindFirstObjectByType<CrosshairAim>();

        if (crossAim != null && gunEndT != null)
        {
            crossAim.gunEnd = gunEndT;
            var cam = Camera.main ?? FindCameraByTag(mainCameraTag);
            if (cam != null) crossAim.SetCamera(cam);
        }

        var sniperView = instance.GetComponentInChildren<TankSniperView>();
        if (sniperView != null)
        {
            sniperView.gunEnd = gunEndT;
            sniperView.mainCamera = Camera.main ?? FindCameraByTag(mainCameraTag);
            sniperView.crosshairAimUI = FindUIByName("CrosshairAimUI");
            sniperView.crosshairSniperUI = FindUIByName("CrosshairSniperUI");
            sniperView.sniperVignette = FindUIByName("SniperVignette");
            sniperView.tankShoot = ts;
        }

        var turretAiming = instance.GetComponentInChildren<TurretAiming>();
        if (turretAiming != null)
        {
            var cam = Camera.main ?? FindCameraByTag(mainCameraTag);
            if (cam != null) turretAiming.cameraTransform = cam.transform;
            turretAiming.owner = tank;
        }

        if (ts != null)
        {
            if (ts.gunEnd == null && gunEndT != null) ts.gunEnd = gunEndT;
            if (ts.reloadDisplay == null)
            {
                if (hudCanvas != null)
                    ts.reloadDisplay = hudCanvas.GetComponentInChildren<ReloadDisplay>();
                else
                {
                    var go = GameObject.FindWithTag(reloadDisplayTag);
                    if (go != null) ts.reloadDisplay = go.GetComponent<ReloadDisplay>();
                }
            }

            FillBulletSlotsFromDefinition(ts, tank);
        }

    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        if (string.Equals(parent.name, name, System.StringComparison.OrdinalIgnoreCase)) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            var res = FindChildRecursive(c, name);
            if (res != null) return res;
        }
        return null;
    }

    Transform FindChildByNameContains(Transform parent, string contains)
    {
        if (parent == null || string.IsNullOrEmpty(contains)) return null;
        if (parent.name.IndexOf(contains, System.StringComparison.OrdinalIgnoreCase) >= 0) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            var res = FindChildByNameContains(c, contains);
            if (res != null) return res;
        }
        return null;
    }

    Camera FindCameraByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return Camera.main;
        var go = GameObject.FindWithTag(tag);
        if (go != null) return go.GetComponent<Camera>();
        return Camera.main;
    }

    GameObject FindUIByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (hudCanvas != null)
        {
            var t = FindChildRecursive(hudCanvas.transform, name);
            return t != null ? t.gameObject : null;
        }
        var go = GameObject.Find(name);
        return go;
    }

    void FillBulletSlotsFromDefinition(TankShoot tankShoot, Tank tank)
    {
        if (tank == null || tank.PrimaryGun == null)
        {
            Debug.LogWarning("FillBulletSlotsFromDefinition: нет GunDefinition");
            return;
        }

        var defs = tank.PrimaryGun.bullets;
        if (defs == null || defs.Length == 0)
        {
            Debug.LogWarning("GunDefinition не содержит bullets");
            return;
        }

        tankShoot.bulletSlots = new BulletSlot[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            var defEntry = defs[i];
            var bdef = defEntry.bullet;
            var slot = new BulletSlot();
            slot.displayName = bdef != null ? bdef.bulletName : $"Slot{i}";
            slot.type = bdef != null ? bdef.type : BulletType.AP;
            slot.definition = bdef;

            BulletPool pool = null;
            if (BulletPoolManager.Instance != null)
            {
                pool = BulletPoolManager.Instance.GetPoolFor(bdef);
            }
            else
            {
                var pools = FindObjectsByType<BulletPool>(FindObjectsSortMode.None);
                pool = pools.FirstOrDefault(p => p.HandlesDefinition(bdef));
                if (pool == null) pool = pools.FirstOrDefault();
            }

            slot.pool = pool;
            if (slot.pool == null)
                Debug.LogWarning($"Не найден пул для снаряда {slot.displayName}. Установи пул в сцене или добавь соответствие в BulletPool.supportedDefinitions.");

            tankShoot.bulletSlots[i] = slot;
        }
    }

    void SetupCameras(GameObject playerTank)
    {
        if (playerTank == null) return;

        var mainCam = GameObject.Find("CM_MainCam")?.GetComponent<Unity.Cinemachine.CinemachineVirtualCamera>();
        if (mainCam != null)
        {
            var mainPivot = FindChildRecursive(playerTank.transform, $"{playerTank.name}_main_pivot");
            if (mainPivot != null)
                mainCam.Follow = mainPivot;
        }

        var commanderCam = GameObject.Find("CM_CommanderCam")?.GetComponent<Unity.Cinemachine.CinemachineVirtualCamera>();
        if (commanderCam != null)
        {
            var commanderPivot = FindChildRecursive(playerTank.transform, $"{playerTank.name}_commander_pivot");
            if (commanderPivot != null)
                commanderCam.Follow = commanderPivot;
        }
    }

}
