using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    private static readonly WaitForSeconds _waitForSeconds0_1 = new(0.1f);

    [Header("Level Select")]
    public GameObject levelSelectCanvas;
    public GameObject levelCardPrefab;
    public Sprite placeholderSprite;
    public int totalLevels = 10;

    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public Slider progressBar;
    public TextMeshProUGUI loadingText;

    [Header("Level Thumbnails")]
    public Sprite[] levelThumbnails;
    [Header("Level Names")]
    public string[] levelNames;

    [Header("Grid Settings")]
    public RectTransform gridParent;

    private GameObject[] levelCards;
    private void Start()
    {
        if (levelSelectCanvas == null || levelCardPrefab == null)
        {
            Debug.LogError("LevelSelectManager: Canvas или Prefab не назначены!");
            return;
        }

        if (gridParent == null)
        {
            Debug.Log("GridParent не назначен, создаём автоматически на Canvas");
            GameObject gridObj = new("GridParent", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObj.transform.SetParent(levelSelectCanvas.transform, false);
            gridParent = gridObj.GetComponent<RectTransform>();
        }

        if (!gridParent.TryGetComponent<GridLayoutGroup>(out var grid)) grid = gridParent.gameObject.AddComponent<GridLayoutGroup>();

        levelCards = new GameObject[totalLevels];

        for (int i = 0; i < totalLevels; i++)
        {
            GameObject card = Instantiate(levelCardPrefab, gridParent, false);
            levelCards[i] = card;

            if (!card.TryGetComponent<LevelCardClick>(out var clickHandler))
                clickHandler = card.AddComponent<LevelCardClick>();
            clickHandler.level = i + 1;
            clickHandler.manager = this;

            UpdateSingleLevelCard(card, i + 1, i);
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    private void UpdateSingleLevelCard(GameObject card, int level, int index)
    {
        var images = card.GetComponentsInChildren<Image>(true);
        TextMeshProUGUI levelName = null;
        TextMeshProUGUI scoreText = null;

        foreach (var img in images)
        {
            if (img.name.ToLower().Contains("thumbnail"))
            {
                img.sprite = IsLevelUnlocked(level) ? GetLevelThumbnail(level) : placeholderSprite;
                break;
            }
        }

        var texts = card.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            string n = text.name.ToLower();
            if (n.Contains("name")) levelName = text;
            else if (n.Contains("score")) scoreText = text;
        }

        Image checkmark = null;
        foreach (var img in images)
        {
            if (img.name.ToLower().Contains("check"))
            {
                checkmark = img;
                break;
            }
        }

        bool unlocked = IsLevelUnlocked(level);
        bool completed = IsLevelCompleted(level);

        if (levelName != null)
        {
            string entryKey = unlocked && levelNames != null && index < levelNames.Length
                ? levelNames[index]
                : "level_card_locked";
            var lseName = levelName.GetComponent<LocalizeStringEvent>();
            LocalizationHelper.SetLocalizedText(lseName, entryKey);
        }

        if (checkmark != null)
            checkmark.gameObject.SetActive(completed);

        if (unlocked && completed)
        {
            int stars = PlayerPrefs.GetInt($"Level{level}_Stars", 0);
            stars = Mathf.Clamp(stars, 0, 3);
            int totalStarsToShow = 3;

            Transform starsContainer = FindChildByName(card, "Stars");
            if (starsContainer == null)
            {
                Debug.LogError($"Карточка уровня {level}: Не найден контейнер 'Stars' по всему дереву!");
                if (scoreText != null) scoreText.text = "";
                return;
            }

            Transform templateStarsContainer = FindChildByName(levelCardPrefab, "Stars");
            if (templateStarsContainer == null)
            {
                Debug.LogError($"Шаблон {levelCardPrefab.name}: Не найден контейнер 'Stars' по всему дереву!");
                if (scoreText != null) scoreText.text = "";
                return;
            }

            var templateStarImages = templateStarsContainer.GetComponentsInChildren<Image>(true);
            if (templateStarImages.Length < 2)
            {
                Debug.LogError($"Шаблон {levelCardPrefab.name}: В контейнере 'Stars' найдено {templateStarImages.Length} Image компонентов (нужно минимум 2)!");
                if (scoreText != null) scoreText.text = "";
                return;
            }

            Sprite templateFilledSprite = templateStarImages[0].sprite;
            Sprite templateEmptySprite = templateStarImages[1].sprite;

            for (int c = starsContainer.childCount - 1; c >= 0; c--)
                DestroyImmediate(starsContainer.GetChild(c).gameObject);

            for (int s = 0; s < totalStarsToShow; s++)
            {
                GameObject starObj = new($"Star_{s}", typeof(Image));
                Image starImage = starObj.GetComponent<Image>();
                starImage.transform.SetParent(starsContainer, false);

                bool isFilled = s < stars;
                starImage.sprite = isFilled ? templateFilledSprite : templateEmptySprite;

            }

            int score = PlayerPrefs.GetInt($"Level{level}_Score", 0);
            if (scoreText != null)
            {
                var lseScore = scoreText.GetComponent<LocalizeStringEvent>();
                LocalizationHelper.SetLocalizedText(lseScore, "level_card_score", score);
            }
        }
        else
        {
            Transform starsContainer = FindChildByName(card, "Stars");
            if (starsContainer != null)
            {
                for (int c = starsContainer.childCount - 1; c >= 0; c--)
                    DestroyImmediate(starsContainer.GetChild(c).gameObject);
            }

            if (scoreText != null)
            {
                var lseScore = scoreText.GetComponent<LocalizeStringEvent>();
                if (lseScore != null)
                {
                    lseScore.enabled = false;
                }

                scoreText.text = "";
            }
        }
    }

    private Transform FindChildByName(GameObject parent, string name)
    {
        Transform[] transforms = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in transforms)
        {
            if (t.name == name)
                return t;
        }
        return null;
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
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * 0.25f);

            if (progressBar != null)
                progressBar.value = displayedProgress;

            if (loadingText != null)
            {
                if (loadingText.TryGetComponent<LocalizeStringEvent>(out var lse))
                {
                    int dots = Mathf.FloorToInt(Time.time % 3) + 1;
                    LocalizationHelper.SetLocalizedText(lse, "loading_text", dots);
                }
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

    private void HandleLanguageChanged(string languageCode)
    {
        if (levelCards != null)
        {
            for (int i = 0; i < levelCards.Length; i++)
            {
                UpdateSingleLevelCard(levelCards[i], i + 1, i);
            }
        }
    }

    public bool IsLevelUnlocked(int level) => level == 1 || PlayerPrefs.GetInt($"Level{level}_Unlocked", 0) == 1;
    private bool IsLevelCompleted(int level) => PlayerPrefs.GetInt($"Level{level}_Completed", 0) == 1;

    private Sprite GetLevelThumbnail(int level)
    {
        if (levelThumbnails != null && level - 1 < levelThumbnails.Length)
            return levelThumbnails[level - 1];
        return placeholderSprite;
    }

    public void BackToMainMenu() => SceneManager.LoadScene(SceneNames.MainMenu);
}

public class LevelCardClick : MonoBehaviour, IPointerClickHandler
{
    public int level;
    public LevelSelectManager manager;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
        {
            if (manager.IsLevelUnlocked(level))
            {
                manager.PlayLevel(level);
            }
            else
            {
                Debug.Log($"Уровень {level} закрыт. Клик заблокирован.");
            }
        }
    }
}