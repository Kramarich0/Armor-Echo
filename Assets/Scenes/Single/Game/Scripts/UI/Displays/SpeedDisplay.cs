using UnityEngine;
using TMPro;
using Serilog;
using UnityEngine.Localization.Components;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SpeedDisplay : MonoBehaviour
{
    private TextMeshProUGUI speedText;

    void Start()
    {
        speedText = GetComponent<TextMeshProUGUI>();
        if (speedText == null)
        {
            Log.Error("[UI] No TextMeshProUGUI component found on {ObjectName}", gameObject.name);
        }
    }

    public void SetSpeed(int speedKmh)
    {
        if (!TryGetComponent<LocalizeStringEvent>(out var lse)) return;

        LocalizationHelper.SetLocalizedText(lse, "speed_display", Mathf.RoundToInt(speedKmh));

        lse.gameObject.SetActive(true);
    }

}