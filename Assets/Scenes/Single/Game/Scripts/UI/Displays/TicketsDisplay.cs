using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Serilog;

public class TicketsDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    public Image friendlyFill;
    public Image enemyFill;
    public TextMeshProUGUI friendlyText;
    public TextMeshProUGUI enemyText;

    [Header("Animation")]
    public float smoothSpeed = 2f;

    private float currentFriendlyFill = 1f;
    private float currentEnemyFill = 1f;
    private float targetFriendlyFill = 1f;
    private float targetEnemyFill = 1f;

    void Start()
    {
        StartCoroutine(DelayedInit());
    }

    IEnumerator DelayedInit()
    {
        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            GameManager.Instance.IsLevelInitialized
        );
        SetupUI();
    }

    void SetupUI()
    {
        if (GameManager.Instance == null)
        {
            Log.Error("[TicketsDisplay] GameManager is null after waiting");
            return;
        }

        GameManager.Instance.OnTicketsChanged += OnTicketsChanged;
        OnTicketsChanged(
            GameManager.Instance.friendlyTickets,
            GameManager.Instance.enemyTickets
        );
        Log.Debug("[TicketsDisplay] Successfully subscribed to tickets events");
    }

    private void OnTicketsChanged(int friendly, int enemy)
    {
        int maxFriendly = GameManager.Instance.initialFriendlyTickets;
        int maxEnemy = GameManager.Instance.initialEnemyTickets;

        if (maxFriendly <= 0) maxFriendly = 1;
        if (maxEnemy <= 0) maxEnemy = 1;

        targetFriendlyFill = Mathf.Clamp01((float)friendly / maxFriendly);
        targetEnemyFill = Mathf.Clamp01((float)enemy / maxEnemy);

        if (friendlyText != null) friendlyText.text = $"{friendly}";
        if (enemyText != null) enemyText.text = $"{enemy}";

        Log.Debug("[Tickets] Targets: Friendly={FriendlyFill:P0}, Enemy={EnemyFill:P0} | Current tickets: Friendly={FriendlyTickets}, Enemy={EnemyTickets}",
                  targetFriendlyFill, targetEnemyFill, friendly, enemy);
    }

    void Update()
    {
        if (Mathf.Abs(currentFriendlyFill - targetFriendlyFill) > 0.001f)
            currentFriendlyFill = Mathf.MoveTowards(currentFriendlyFill, targetFriendlyFill, smoothSpeed * Time.deltaTime);
        else
            currentFriendlyFill = targetFriendlyFill;

        if (Mathf.Abs(currentEnemyFill - targetEnemyFill) > 0.001f)
            currentEnemyFill = Mathf.MoveTowards(currentEnemyFill, targetEnemyFill, smoothSpeed * Time.deltaTime);
        else
            currentEnemyFill = targetEnemyFill;

        if (friendlyFill != null) friendlyFill.fillAmount = currentFriendlyFill;
        if (enemyFill != null) enemyFill.fillAmount = currentEnemyFill;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTicketsChanged -= OnTicketsChanged;
    }
}