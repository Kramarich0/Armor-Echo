using UnityEngine;
using TMPro;
using Serilog;
using UnityEngine.Localization.Components;

[RequireComponent(typeof(TextMeshProUGUI))]
[RequireComponent(typeof(LocalizeStringEvent))]
public class ReloadDisplay : MonoBehaviour
{
    private TextMeshProUGUI reloadText;
    private float currentReloadTime = 6f;
    private float maxReloadTime = 6f;
    private bool isReloading = false;

    void Start()
    {
        reloadText = GetComponent<TextMeshProUGUI>();
        if (reloadText == null)
        {
            Log.Error("[UI] No TextMeshProUGUI component found on {ObjectName}", gameObject.name);
        }
    }

    public void SetReload(float remainingTime, float maxTime)
    {
        currentReloadTime = remainingTime;
        maxReloadTime = maxTime;
        isReloading = remainingTime > 0f;

        if (!TryGetComponent<LocalizeStringEvent>(out var lse)) return;

        if (isReloading)
        {
            LocalizationHelper.SetLocalizedText(lse, "reload_in_progress", currentReloadTime.ToString("F1"));
        }
        else
        {
            LocalizationHelper.SetLocalizedText(lse, "reload_ready");
        }
    }

    public void Hide()
    {
        isReloading = false;
        if (reloadText != null)
        {
            gameObject.SetActive(false);
        }
    }
}