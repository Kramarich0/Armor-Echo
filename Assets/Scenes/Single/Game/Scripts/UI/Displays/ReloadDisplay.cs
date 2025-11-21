using UnityEngine;
using TMPro;
using Serilog;

[RequireComponent(typeof(TextMeshProUGUI))]
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

        if (reloadText != null && isReloading)
        {
            reloadText.text = $"<color=red>Перезарядка... {currentReloadTime:F1}с</color>";
            gameObject.SetActive(true);
        }
        else
        {
            reloadText.text = "<color=green>Готово</color>";
            gameObject.SetActive(true);
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