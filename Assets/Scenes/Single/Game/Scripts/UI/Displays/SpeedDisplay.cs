using UnityEngine;
using TMPro;
using Serilog;

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
        if (speedText != null)
            speedText.text = Mathf.RoundToInt(speedKmh) + " км/ч";
    }

}