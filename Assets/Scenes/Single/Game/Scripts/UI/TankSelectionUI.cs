using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Text;
using Serilog;

public class TankSelectionUI : MonoBehaviour
{
    [Header("UI")]
    public Button nextButton;
    public Button prevButton;
    public Button actionButton;
    public TMP_Text tankNameText;
    public TMP_Text descriptionText;
    public TMP_Text detailsText;
    public TMP_Text bulletsText;
    public TMP_Text priceLabel;
    public TMP_Text balanceText;

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
        actionButton.onClick.AddListener(OnActionButtonClicked);
        // PlayerPrefs.DeleteAll(); // удалить потом !!!
        for (int i = 0; i < availableTanks.Length; i++)
        {
            if (IsTankUnlocked(availableTanks[i]))
            {
                index = i;
                break;
            }
        }
        ShowTank(index);
        UpdateBalanceUI();
    }

    void OnActionButtonClicked()
    {
        var tank = availableTanks[index];
        if (IsTankUnlocked(tank))
        {
            PlayerSelection.selectedTank = tank;
            SceneManager.LoadSceneAsync(SceneNames.SelectLevel);
        }
        else
        {
            if (CurrencyManager.TrySpend(tank.priceInStars))
            {
                UnlockTank(tank);
                ShowTank(index);
                UpdateBalanceUI();
            }
            else
            {
                Log.Debug("Not enough stars!");
                ToastHelper.Instance.Show("На балансе не хватает денег");
            }
        }
    }

    void UpdateBalanceUI()
    {
        if (balanceText != null)
        {
            int balance = CurrencyManager.GetBalance();
            balanceText.text = $"Баланс: {balance}";
        }
    }

    bool IsTankUnlocked(TankDefinition tank)
    {
        if (tank.isUnlocked)
            return true;

        return PlayerPrefs.GetInt($"UnlockedTank_{tank.name}", 0) == 1;
    }

    void UnlockTank(TankDefinition tank)
    {
        PlayerPrefs.SetInt($"UnlockedTank_{tank.name}", 1);
        PlayerPrefs.Save();
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
        descriptionText.text = string.IsNullOrWhiteSpace(def.description)
            ? "Описание отсутствует."
            : def.description;

        detailsText.text = BuildTankAndGunSummary(def);
        bulletsText.text = BuildBulletsSummary(def.primaryGun);

        if (currentPreview != null) Destroy(currentPreview);

        if (def.previewPrefab != null)
        {
            currentPreview = Instantiate(def.previewPrefab, previewSpawnPoint.position, previewSpawnPoint.rotation);
            foreach (var rb in currentPreview.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
            foreach (var col in currentPreview.GetComponentsInChildren<Collider>()) col.enabled = false;
        }

        bool unlocked = IsTankUnlocked(def);
        priceLabel.text = unlocked
            ? "В наличии"
            : $"Купить за {def.priceInStars}";

        actionButton.GetComponentInChildren<TMP_Text>().text = unlocked
            ? "Начать бой"
            : "Купить";
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
