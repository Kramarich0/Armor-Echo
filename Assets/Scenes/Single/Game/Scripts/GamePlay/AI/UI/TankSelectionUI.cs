using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Text;

public class TankSelectionUI : MonoBehaviour
{
    [Header("UI")]
    public Button nextButton;
    public Button prevButton;
    public Button startButton;
    public TMP_Text tankNameText;
    public RawImage previewImage;
    [Header("Tank Info UI (assign in inspector)")]
    public TMP_Text descriptionText; 
    public TMP_Text detailsText;   
    public TMP_Text bulletsText;    


    [Header("Preview Camera & RenderTexture")]
    public Camera previewCamera;
    public Transform previewSpawnPoint;

    [Header("Tanks list")]
    public TankDefinition[] availableTanks;

    private int index = 0;
    private GameObject currentPreview;
    private readonly float rotationSpeed = 20f;

    void Start()
    {
        nextButton.onClick.AddListener(NextTank);
        prevButton.onClick.AddListener(PrevTank);
        startButton.onClick.AddListener(StartBattle);

        ShowTank(index);
    }

    void Update()
    {
        if (currentPreview != null)
        {
            currentPreview.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void NextTank()
    {
        index = (index + 1) % availableTanks.Length;
        ShowTank(index);
    }

    void PrevTank()
    {
        index--;
        if (index < 0) index = availableTanks.Length - 1;
        ShowTank(index);
    }

    void ShowTank(int idx)
    {
        var def = availableTanks[idx];
        if (def == null) return;

        tankNameText.text = def.tankName;
        descriptionText.text = string.IsNullOrWhiteSpace(def.description) ? "Описание отсутствует." : def.description;


        detailsText.text = BuildTankAndGunSummary(def);

        bulletsText.text = BuildBulletsSummary(def.primaryGun);

        if (currentPreview != null) Destroy(currentPreview);

        currentPreview = Instantiate(def.previewPrefab, previewSpawnPoint.position, previewSpawnPoint.rotation);

        foreach (var rb in currentPreview.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        foreach (var col in currentPreview.GetComponentsInChildren<Collider>()) col.enabled = false;

        previewCamera.transform.LookAt(currentPreview.transform);
    }

    void StartBattle()
    {
        PlayerSelection.selectedTank = availableTanks[index];
        SceneManager.LoadScene(SceneNames.SelectLevel);
    }

    string BuildTankAndGunSummary(TankDefinition def)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Класс: {def.tankClass}");
        sb.AppendLine($"HP: {def.health:F0}");
        sb.AppendLine($"Макс (km/h): вперёд {def.maxForwardSpeedKmh:F1} / назад {def.maxBackwardSpeedKmh:F1}");
        sb.AppendLine($"MoveSpeed (логика): {def.moveSpeed:F2}, rotationSpeed: {def.rotationSpeed:F1}, turretRotationSpeed: {def.turretRotationSpeed:F1}");
        sb.AppendLine($"Физика: maxMotorTorque={def.maxMotorTorque:F0}, maxBrakeTorque={def.maxBrakeTorque:F0}, moveResponse={def.moveResponse:F2}, turnResponse={def.turnResponse:F2}");
        sb.AppendLine($"Обнаружение/стрейф: detect={def.detectionRadius:F0}m, strafeRadius={def.strafeRadius:F1}m, strafeSpeed={def.strafeSpeed:F2}");
        sb.AppendLine($"Spread (base/moving/stationary): {def.baseSpreadDegrees:F2}° / {def.movingSpreadFactor:F2} / {def.stationarySpreadFactor:F2}");
        sb.AppendLine();

        var gun = def.primaryGun;
        if (gun != null)
        {
            sb.AppendLine($"Пушка: {gun.gunName}");
            sb.AppendLine($"  Калибр: {gun.caliber} мм  |  Интервал выстрела: {gun.fireInterval:F2}s  |  FireRate: {gun.FireRate:F2} rps");
            sb.AppendLine($"  Подъём/углы: liftSpeed={gun.liftSpeed:F2}, minAngle={gun.minGunAngle}°, maxAngle={gun.maxGunAngle}°");
            sb.AppendLine($"  Дальность: {gun.shootRange:F0} m  |  gravity:{gun.bulletUseGravity}");
            sb.AppendLine($"  Слотов снарядов: {(gun.bullets != null ? gun.bullets.Length : 0)}  |  debugLogs: {gun.debugLogs}");
        }
        else
        {
            sb.AppendLine("Пушка: отсутствует");
        }

        return sb.ToString();
    }

    string BuildBulletsSummary(GunDefinition gun)
    {
        if (gun == null) return "Снарядов нет.";

        var sb = new StringBuilder();
        if (gun.bullets == null || gun.bullets.Length == 0)
        {
            sb.AppendLine("Снарядов нет в определении пушки.");
            return sb.ToString();
        }

        for (int i = 0; i < gun.bullets.Length; i++)
        {
            var slot = gun.bullets[i];
            var bullet = slot.bullet;
            if (bullet == null)
            {
                sb.AppendLine($"{i + 1}. (пустой слот)");
                continue;
            }

            // muzzle velocity: prefer slot.muzzleVelocity if >0, otherwise try gun.GetMuzzleVelocity
            float muzzleFromSlot = slot.muzzleVelocity;
            float muzzleFromGun = 0f;
            try
            {
                muzzleFromGun = gun.GetMuzzleVelocity(bullet);
            }
            catch { muzzleFromGun = 0f; }

            float muzzle = muzzleFromSlot > 0.0001f ? muzzleFromSlot : muzzleFromGun;

            sb.AppendLine($"{i + 1}. {bullet.bulletName} ({bullet.type})");
            sb.AppendLine($"   Калибр: {bullet.caliber} мм | Масса: {bullet.massKg:F2} kg | Пасп. скорость: {bullet.referenceVelocity:F0} m/s");
            sb.AppendLine($"   Muzzle (этой пушкой): {(muzzle > 0f ? muzzle.ToString("F0") + " m/s" : "-")}");
            sb.AppendLine($"   Урон: {bullet.damage} | Пробитие (начальное): {bullet.penetration:F0} mm | minPen: {bullet.minPenetration:F0} mm");
            sb.AppendLine($"   Баллистика: ballisticK={bullet.ballisticK:F6}, deMarreK={bullet.deMarreK:F2}, minSpeed={bullet.minSpeed:F0} m/s");
            sb.AppendLine($"   Рикошет: ricochetAngle={bullet.ricochetAngle:F0}°, normalization={bullet.normalization:F1}, ignoreAngle={bullet.ignoreAngle}");
            sb.AppendLine($"   OvermatchFactor: {bullet.overmatchFactor:F2} | SplashRadius: {bullet.splashRadius:F2} m");
            sb.AppendLine($"   useGravity: {bullet.useGravity} | visualPrefab: {(bullet.visualPrefab ? bullet.visualPrefab.name : "—")}");
            sb.AppendLine("");
        }

        return sb.ToString();
    }

}
