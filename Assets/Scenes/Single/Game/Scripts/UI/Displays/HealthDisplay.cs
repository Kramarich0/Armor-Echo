// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using Serilog;

// public class HealthDisplay : MonoBehaviour
// {
//     [SerializeField] private TextMeshProUGUI healthText;
//     [SerializeField] private Image healthBar;

//     private PlayerTankHealth playerHealth;

//     void Start()
//     {
//         var playerObject = GameObject.FindGameObjectWithTag("Player");
//         if (playerObject == null)
//         {
//             Log.Error("[HealthDisplay] Player with tag 'Player' not found on {ObjectName}", gameObject.name);
//             enabled = false;
//             return;
//         }

//         playerHealth = playerObject.GetComponent<PlayerTankHealth>();
//         if (playerHealth == null)
//         {
//             Log.Error("[HealthDisplay] No TankHealth component found on {ObjectName}", gameObject.name);
//             enabled = false;
//             return;
//         }

//         playerHealth.OnHealthChanged += UpdateDisplay;
//         UpdateDisplay(playerHealth.currentHealth, playerHealth.maxHealth);
//     }

//     void UpdateDisplay(float current, float max)
//     {
//         float fill = max > 0.001f ? current / max : 0f;

//         if (healthText != null)
//             healthText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";

//         if (healthBar != null)
//             healthBar.fillAmount = fill;
//     }

//     void OnDestroy()
//     {
//         if (playerHealth != null)
//             playerHealth.OnHealthChanged -= UpdateDisplay;
//     }
// }

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Serilog;

public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthBar;

    private PlayerTankHealth playerHealth;

    public void Initialize(PlayerTankHealth player)
    {
        if (player == null)
        {
            Log.Error("[HealthDisplay] PlayerTankHealth is null on {ObjectName}", gameObject.name);
            enabled = false;
            return;
        }

        playerHealth = player;
        playerHealth.OnHealthChanged += UpdateDisplay;
        UpdateDisplay(playerHealth.currentHealth, playerHealth.maxHealth);
    }

    void UpdateDisplay(float current, float max)
    {
        float fill = max > 0.001f ? current / max : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";

        if (healthBar != null)
            healthBar.fillAmount = fill;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateDisplay;
    }

}
