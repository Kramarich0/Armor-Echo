using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CoolLevelSelectManager : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_1 = new(0.1f);
    [Header("Level Select")]
    public GameObject levelSelectCanvas;
    public Button[] levelButtons;
    public Image[] levelCheckmarks;

    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public Slider progressBar;
    public TextMeshProUGUI loadingText;

    private void Awake()
    {
        foreach (var btn in levelButtons)
            btn.interactable = false;

        if (levelCheckmarks != null)
            foreach (var chk in levelCheckmarks)
                chk.gameObject.SetActive(false);

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    private void Start()
    {
        UpdateLevelButtons();
    }

    public void PlayLevel(int level)
    {
        if (!IsLevelUnlocked(level)) return;
        StartCoroutine(LoadLevelWithStyle($"Level{level}"));
    }

    private IEnumerator LoadLevelWithStyle(string sceneName)
    {
        if (levelSelectCanvas != null)
            levelSelectCanvas.SetActive(false);

        loadingScreen.SetActive(true);

        float displayedProgress = 0f;
        float timer = 0f;
        float minDisplayTime = 3f;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            timer += Time.unscaledDeltaTime;

            float targetProgress = Mathf.Clamp01(op.progress / 0.9f);
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * .25f);

            if (progressBar != null)
                progressBar.value = displayedProgress;

            if (loadingText != null)
            {
                int dots = Mathf.FloorToInt(Time.time % 3) + 1;
                loadingText.text = "Загрузка" + new string('.', dots);
            }

            if (displayedProgress >= 0.99f && timer >= minDisplayTime)
            {
                if (progressBar != null) progressBar.value = 1f;
                yield return _waitForSeconds0_1;
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void UpdateLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int level = i + 1;
            levelButtons[i].interactable = IsLevelUnlocked(level);

            if (levelCheckmarks != null && levelCheckmarks.Length > i)
                levelCheckmarks[i].gameObject.SetActive(IsLevelCompleted(level));
        }
    }

    private bool IsLevelUnlocked(int level) => level == 1 || PlayerPrefs.GetInt($"Level{level}_Unlocked", 0) == 1;
    private bool IsLevelCompleted(int level) => PlayerPrefs.GetInt($"Level{level}_Completed", 0) == 1;

    public void BackToMainMenu() => SceneManager.LoadScene("MainMenu");
}
