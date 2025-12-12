using System.Collections;
using Serilog;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
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
    public Image loadingThumbnail;
    public TextMeshProUGUI loadingLevelName;

    [Header("Level Thumbnails")]
    public Sprite[] levelThumbnails;
    [Header("Level Names (store localization keys here)")]
    public string[] levelNames;

    [Header("Grid Settings")]
    public RectTransform gridParent;

    private GameObject[] levelCards;
    private LevelCard[] levelCardComponents;
    private bool isLoadingLevel = false;

    private int[] cachedStars;
    private int[] cachedScores;
    private bool[] cachedUnlocked;
    private bool[] cachedCompleted;

    private Coroutine spawnRoutine;
    private Coroutine loadRoutine;

    private void Start()
    {
        if (levelSelectCanvas == null || levelCardPrefab == null)
        {
            Log.Error("LevelSelectManager: Canvas или Prefab не назначены!");
            return;
        }

        if (gridParent == null)
        {
            Log.Debug("GridParent не назначен, создаём автоматически на Canvas");
            GameObject gridObj = new("GridParent", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObj.transform.SetParent(levelSelectCanvas.transform, false);
            gridParent = gridObj.GetComponent<RectTransform>();
        }

        if (!gridParent.TryGetComponent<GridLayoutGroup>(out var grid))
            grid = gridParent.gameObject.AddComponent<GridLayoutGroup>();

        levelCards = new GameObject[totalLevels];
        levelCardComponents = new LevelCard[totalLevels];

        cachedStars = new int[totalLevels];
        cachedScores = new int[totalLevels];
        cachedUnlocked = new bool[totalLevels];
        cachedCompleted = new bool[totalLevels];

        for (int i = 0; i < totalLevels; i++)
        {
            int lvl = i + 1;
            cachedStars[i] = PlayerPrefs.GetInt($"Level{lvl}_Stars", 0);
            cachedScores[i] = PlayerPrefs.GetInt($"Level{lvl}_Score", 0);
            cachedUnlocked[i] = (lvl == 1) || PlayerPrefs.GetInt($"Level{lvl}_Unlocked", 0) == 1;
            cachedCompleted[i] = PlayerPrefs.GetInt($"Level{lvl}_Completed", 0) == 1;
        }

        spawnRoutine = StartCoroutine(SpawnLevelCards());

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    private IEnumerator SpawnLevelCards()
    {
        for (int i = 0; i < totalLevels; i++)
        {
            GameObject cardGo = Instantiate(levelCardPrefab, gridParent, false);
            levelCards[i] = cardGo;

            if (!cardGo.TryGetComponent<LevelCard>(out var lc)) lc = cardGo.AddComponent<LevelCard>();
            lc.InitCache();
            levelCardComponents[i] = lc;

            if (!cardGo.TryGetComponent<LevelCardClick>(out var clickHandler))
                clickHandler = cardGo.AddComponent<LevelCardClick>();
            clickHandler.level = i + 1;
            clickHandler.manager = this;

            string key = (cachedUnlocked[i] && levelNames != null && i < levelNames.Length) ? levelNames[i] : "level_card_locked";
            int stars = Mathf.Clamp(cachedStars[i], 0, 3);
            lc.UpdateUI(GetLevelThumbnail(i + 1), key, cachedUnlocked[i], cachedCompleted[i], cachedScores[i], stars);

            if (i % 6 == 0)
                yield return null;
        }

        spawnRoutine = null;
        yield break;
    }


    public void PlayLevel(int level)
    {
        if (level < 1 || level > totalLevels) return;
        if (!IsLevelUnlocked(level) || isLoadingLevel) return;

        if (loadingThumbnail != null)
            loadingThumbnail.sprite = GetLevelThumbnail(level);

        string levelKey = (levelNames != null && level - 1 < levelNames.Length) ? levelNames[level - 1] : $"Level {level}";

        if (loadingLevelName != null && loadingLevelName.TryGetComponent<LocalizeStringEvent>(out var nameLse))
        {
            LocalizationHelper.SetLocalizedText(nameLse, levelKey);
        }
        else if (loadingLevelName != null)
        {
            var localized = new LocalizedString { TableReference = "GameStrings", TableEntryReference = levelKey };
            localized.GetLocalizedStringAsync().Completed += op =>
            {
                loadingLevelName.text = op.Result;
            };
        }

        isLoadingLevel = true;

        if (loadRoutine != null) StopCoroutine(loadRoutine);
        loadRoutine = StartCoroutine(ShowLoadingAndLoad(level));
    }

    private IEnumerator ShowLoadingAndLoad(int level)
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);
        yield return null;

        if (levelSelectCanvas != null) levelSelectCanvas.SetActive(false);

        yield return StartCoroutine(LoadLevelWithStyle($"Level{level}"));

        loadRoutine = null;
    }

    private IEnumerator LoadLevelWithStyle(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float displayedProgress = 0f;
        float timer = 0f;
        float minDisplayTime = 1.5f;
        float smoothSpeed = 0.9f;

        float dotTimer = 0f;
        float dotInterval = 0.25f;

        while (!op.isDone)
        {
            float delta = Time.unscaledDeltaTime;
            timer += delta;

            float targetProgress = Mathf.Clamp01(op.progress / 0.9f);
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, delta * smoothSpeed);
            if (progressBar != null)
                progressBar.value = displayedProgress;

            dotTimer += delta;
            if (dotTimer >= dotInterval)
            {
                dotTimer = 0f;
                int dots = (int)(Time.unscaledTime % 3) + 1;

                if (loadingText != null)
                {
                    string originalText = loadingText.text.Split('.')[0]; 
                    loadingText.text = originalText + new string('.', dots);
                }
            }

            if (op.progress >= 0.9f && timer >= minDisplayTime)
            {
                while (displayedProgress < 1f)
                {
                    delta = Time.unscaledDeltaTime;
                    displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, delta * smoothSpeed);
                    if (progressBar != null) progressBar.value = displayedProgress;
                    yield return null;
                }

                op.allowSceneActivation = true;
                isLoadingLevel = false;
            }

            yield return null;
        }

    }

    private void OnDisable()
    {
        if (spawnRoutine != null) { StopCoroutine(spawnRoutine); spawnRoutine = null; }
        if (loadRoutine != null) { StopCoroutine(loadRoutine); loadRoutine = null; }
    }

    public void RefreshCard(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= totalLevels) return;
        var lc = levelCardComponents[levelIndex];
        if (lc == null) return;

        int lvl = levelIndex + 1;
        cachedStars[levelIndex] = PlayerPrefs.GetInt($"Level{lvl}_Stars", cachedStars[levelIndex]);
        cachedScores[levelIndex] = PlayerPrefs.GetInt($"Level{lvl}_Score", cachedScores[levelIndex]);
        cachedUnlocked[levelIndex] = lvl == 1 || PlayerPrefs.GetInt($"Level{lvl}_Unlocked", cachedUnlocked[levelIndex] ? 1 : 0) == 1;
        cachedCompleted[levelIndex] = PlayerPrefs.GetInt($"Level{lvl}_Completed", cachedCompleted[levelIndex] ? 1 : 0) == 1;

        string key = (cachedUnlocked[levelIndex] && levelNames != null && levelIndex < levelNames.Length) ? levelNames[levelIndex] : "level_card_locked";
        lc.UpdateUI(GetLevelThumbnail(lvl), key, cachedUnlocked[levelIndex], cachedCompleted[levelIndex], cachedScores[levelIndex], Mathf.Clamp(cachedStars[levelIndex], 0, 3));
    }

    public void HandleLanguageChanged(string languageCode)
    {
        for (int i = 0; i < levelCardComponents.Length; i++)
        {
            if (levelCardComponents[i] == null) continue;
            string key = (cachedUnlocked[i] && levelNames != null && i < levelNames.Length) ? levelNames[i] : "level_card_locked";
            levelCardComponents[i].UpdateUI(GetLevelThumbnail(i + 1), key, cachedUnlocked[i], cachedCompleted[i], cachedScores[i], Mathf.Clamp(cachedStars[i], 0, 3));
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

    public void BackToTankSelect() => SceneManager.LoadSceneAsync(SceneNames.TankSelection);
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
                Log.Debug($"Уровень {level} закрыт. Клик заблокирован.");
            }
        }
    }
}
