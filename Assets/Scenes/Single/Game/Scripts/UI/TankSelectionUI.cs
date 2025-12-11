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
    public TMP_Text descriptionText;
    public TMP_Text detailsText;
    public TMP_Text bulletsText;

    [Header("RenderTexture")]
    public Transform previewSpawnPoint;

    [Header("Tanks list")]
    public TankDefinition[] availableTanks;

    private int index = 0;
    private GameObject currentPreview;

    void Start()
    {
        nextButton.onClick.AddListener(NextTank);
        prevButton.onClick.AddListener(PrevTank);
        startButton.onClick.AddListener(StartBattle);

        ShowTank(index);
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

    }

    void StartBattle()
    {
        PlayerSelection.selectedTank = availableTanks[index];
        SceneManager.LoadScene(SceneNames.SelectLevel);
    }

    string BuildTankAndGunSummary(TankDefinition def)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Тип: {def.tankClass}");
        sb.AppendLine($"Здоровье: {def.health:F0}");
        sb.AppendLine($"Макс скорость(км/ч): вперёд {def.maxForwardSpeedKmh:F1} / назад {def.maxBackwardSpeedKmh:F1}");
        sb.AppendLine($"Скорость поворота шасси: {def.rotationSpeed:F1}");
        sb.AppendLine($"Скорость поворота башни: {def.turretRotationSpeed:F1}");
        sb.AppendLine();

        var gun = def.primaryGun;
        if (gun != null)
        {
            sb.AppendLine($"Орудие: {gun.gunName}");
            sb.AppendLine($"Калибр: {gun.caliber} мм");
            sb.AppendLine($"Перезарядка: {gun.fireInterval:F2}с");
            sb.AppendLine($"Углы вертикальной наводки: +{gun.minGunAngle}°/-{gun.maxGunAngle}°");
            sb.AppendLine($"Скорость вертикальной наводки:{gun.liftSpeed:F2}");
            sb.AppendLine($"Снарядов: {(gun.bullets != null ? gun.bullets.Length : 0)}");
        }
        else
        {
            sb.AppendLine("Пушка: отсутствует");
        }

        return sb.ToString();
    }

    string BuildBulletsSummary(GunDefinition gun)
    {
        if (gun == null) return "Орудия нет.";

        var sb = new StringBuilder();

        if (gun.bullets == null || gun.bullets.Length == 0)
        {
            sb.AppendLine("Снарядов нет.");
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

            float muzzle = slot.muzzleVelocity > 0f ? slot.muzzleVelocity : gun.GetMuzzleVelocity(bullet);

            sb.AppendLine($"{i + 1}. {bullet.bulletName} ({bullet.type})");
            sb.AppendLine($"Скорость полета: {(muzzle > 0 ? muzzle.ToString("F0") + " м/с" : "-")}");
            sb.AppendLine($"Урон: {bullet.damage}");
            sb.AppendLine($"Пробитие: {bullet.penetration:F0}");
            sb.AppendLine($"Угол рикошета: {bullet.ricochetAngle:F0}°");
            if (bullet.splashRadius != 0) { sb.AppendLine($"Радиус разлета осколков: {bullet.splashRadius:F2} м"); }
            sb.AppendLine();
        }

        return sb.ToString();
    }

}
