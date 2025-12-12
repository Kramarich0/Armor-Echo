using UnityEngine;
using TMPro;
using DG.Tweening;

public class ToastHelper : MonoBehaviour
{
    public static ToastHelper Instance;
    public TMP_Text text;
    public float duration = 2f;
    public float appearDuration = 0.3f;
    public float disappearDuration = 0.3f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        gameObject.SetActive(false);
    }

    public void Show(string message, float? customDuration = null)
    {
        text.text = message;
        gameObject.SetActive(true);

        if (!TryGetComponent<CanvasGroup>(out var cg)) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        cg.DOFade(1f, appearDuration).OnComplete(() =>
        {
            float showTime = customDuration ?? duration;
            Invoke(nameof(Hide), showTime);
        });
    }

    void Hide()
    {
        if (!TryGetComponent<CanvasGroup>(out var cg)) return;

        cg.DOFade(0f, disappearDuration).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}