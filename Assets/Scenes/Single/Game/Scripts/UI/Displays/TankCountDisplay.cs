using Serilog;
using TMPro;
using UnityEngine;

public class TankCountDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI TanksCountText;

    private void Start()
    {
        TryInitialize();
    }

    private void TryInitialize()
    {
        if (GameManager.Instance != null)
        {
            SetupListeners();
            return;
        }

        var existingManager = FindFirstObjectByType<GameManager>();
        if (existingManager != null)
        {
            SetupListeners();
            return;
        }

        Log.Warning("[TankCountDisplay] GameManager not found! Attempting auto-creation for {ObjectName}", gameObject.name);
        GameObject gmObject = new("GameManager");
        gmObject.AddComponent<GameManager>();
        SetupListeners();
    }

    private void SetupListeners()
    {
        GameManager.Instance.OnTankCountChanged += UpdateTankCounters;
        UpdateTankCounters(0, 0);
    }


    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTankCountChanged -= UpdateTankCounters;
        }
    }

    private void UpdateTankCounters(int friendly, int enemy)
    {
        if (TanksCountText == null) return;

        TanksCountText.text = $"{enemy}:{friendly}";
    }

}
