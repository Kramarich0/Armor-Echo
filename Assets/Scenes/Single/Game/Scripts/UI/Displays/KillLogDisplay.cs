using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Serilog;

[RequireComponent(typeof(TextMeshProUGUI))]
[RequireComponent(typeof(CanvasGroup))]
public class KillLogDisplay : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI killLogText;
    public int maxEntries = 5;
    public float fadeTime = 5f;
    public float fadeSpeed = 5f;

    private CanvasGroup canvasGroup;

    private class LogEntry
    {
        public string text;
        public float timeAdded;
        public LogEntry(string t)
        {
            text = t;
            timeAdded = Time.time;
        }
    }

    private readonly List<LogEntry> entries = new();
    private bool subscribed = false;

    private void Awake()
    {
        if (killLogText == null)
            killLogText = GetComponent<TextMeshProUGUI>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    private void OnEnable() => TrySubscribe();
    private void OnDisable() => Unsubscribe();

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnKillLogUpdated += OnKillLogUpdated;
            subscribed = true;
        }
        else
        {
            StartCoroutine(WaitAndSubscribe());
        }
    }

    private IEnumerator WaitAndSubscribe()
    {
        float timeout = 5f;
        float t0 = Time.time;
        while (GameManager.Instance == null && Time.time - t0 < timeout)
            yield return null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnKillLogUpdated += OnKillLogUpdated;
            subscribed = true;
        }
        else
        {
            Log.Warning("[KillLogDisplay] GameManager.Instance not found within timeout for {ObjectName}", gameObject.name);
        }
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (GameManager.Instance != null)
            GameManager.Instance.OnKillLogUpdated -= OnKillLogUpdated;
        subscribed = false;
    }

    private void OnKillLogUpdated(string entry)
    {
        entries.Add(new LogEntry(entry));
        while (entries.Count > maxEntries)
            entries.RemoveAt(0);

        RefreshText();
    }

    private void Update()
    {
        bool changed = false;
        float currentTime = Time.time;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (currentTime - entries[i].timeAdded >= fadeTime)
            {
                entries.RemoveAt(i);
                changed = true;
            }
        }

        float targetAlpha = entries.Count > 0 ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        if (changed)
            RefreshText();
    }

    private void RefreshText()
    {
        if (killLogText == null) return;

        killLogText.text = "";
        foreach (var e in entries)
        {
            killLogText.text += e.text + "\n";
        }
    }
}
