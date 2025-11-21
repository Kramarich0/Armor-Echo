using System.Collections.Generic;
using Serilog;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }
    [Header("UI")]
    public string pausePanelTag = "PausePanel";
    private GameObject pausePanel;
    [Header("Defeat")]
    public string defeatPanelTag = "DefeatPanel";
    [Header("Victory")]
    public string victoryPanelTag = "VictoryPanel";
    private GameObject victoryPanel;
    public TextMeshProUGUI victoryScoreText;
    private GameObject defeatPanel;
    [Header("Input")]
    public InputActionReference pauseAction;
    private bool isPaused = false;
    public bool IsPaused => isPaused;
    private readonly List<PlayerInput> disabledPlayerInputs = new();
    private readonly List<AudioSource> pausedAudioSources = new();
    private bool cursorWasVisible;
    private CursorLockMode cursorWasLockState;
    [Header("Victory")]
    public StarsDisplay victoryStarsDisplay;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        isPaused = false;
        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnEnable()
    {
        if (pauseAction?.action != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction?.action != null)
            pauseAction.action.performed -= OnPausePerformed;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (isPaused || pausePanel == null) return;

        cursorWasVisible = Cursor.visible;
        cursorWasLockState = Cursor.lockState;

        SetPausePanel(true);

        disabledPlayerInputs.Clear();
        foreach (var p in FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!p.enabled) continue;
            p.enabled = false;
            disabledPlayerInputs.Add(p);
        }

        pausedAudioSources.Clear();
        foreach (var audio in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!audio.isPlaying) continue;
            audio.Pause();
            pausedAudioSources.Add(audio);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (!isPaused || pausePanel == null) return;

        SetPausePanel(false);

        foreach (var p in disabledPlayerInputs)
            if (p != null) p.enabled = true;
        disabledPlayerInputs.Clear();

        foreach (var audio in pausedAudioSources)
            if (audio != null)
                audio.UnPause();
        pausedAudioSources.Clear();

        Cursor.visible = cursorWasVisible;
        Cursor.lockState = cursorWasLockState;

        Time.timeScale = 1f;
        isPaused = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Level"))
        {
            if (pausePanel == null)
                pausePanel = FindInactiveByTag(pausePanelTag);

            if (pausePanel != null)
                SetPausePanel(false);

            if (defeatPanel == null)
                defeatPanel = FindInactiveByTag(defeatPanelTag);

            if (defeatPanel != null)
                SetDefeatPanel(false);

            if (victoryPanel == null)
                victoryPanel = FindInactiveByTag(victoryPanelTag);

            if (victoryPanel != null)
                SetVictoryPanel(false);

            isPaused = false;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private GameObject FindInactiveByTag(string tag)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.CompareTag(tag))
                return go;
        }
        return null;
    }

    private void SetPausePanel(bool show)
    {
        if (pausePanel == null) return;
        pausePanel.SetActive(show);

        if (show)
        {
            pausePanel.transform.SetAsLastSibling();
        }

        if (!pausePanel.TryGetComponent(out CanvasGroup cg))
            cg = pausePanel.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = show;
        cg.interactable = show;
    }

    private void SetDefeatPanel(bool show)
    {
        if (defeatPanel == null) return;
        defeatPanel.SetActive(show);

        if (show)
        {
            defeatPanel.transform.SetAsLastSibling();
        }

        if (!defeatPanel.TryGetComponent(out CanvasGroup cg))
            cg = defeatPanel.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = show;
        cg.interactable = show;
    }

    public void ShowDefeatScreen()
    {
        if (defeatPanel == null) return;

        cursorWasVisible = Cursor.visible;
        cursorWasLockState = Cursor.lockState;

        SetDefeatPanel(true);
        SetPausePanel(false);

        disabledPlayerInputs.Clear();
        foreach (var p in FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (p.enabled)
            {
                p.enabled = false;
                disabledPlayerInputs.Add(p);
            }
        }

        pausedAudioSources.Clear();
        foreach (var audio in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (audio.isPlaying)
            {
                audio.Pause();
                pausedAudioSources.Add(audio);
            }
        }


        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
        isPaused = true;
    }

    private void SetVictoryPanel(bool show)
    {
        if (victoryPanel == null) return;
        victoryPanel.SetActive(show);

        if (show)
        {
            victoryPanel.transform.SetAsLastSibling();
        }

        if (!victoryPanel.TryGetComponent(out CanvasGroup cg))
            cg = victoryPanel.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = show;
        cg.interactable = show;
    }

    public void ShowVictoryScreen(int score, int stars)
    {
        if (victoryPanel == null) return;


        if (victoryScoreText != null)
            victoryScoreText.text = $"Очки: {score}";
        Log.Debug("[NextLevel] Stars: {Stars}", stars);
        victoryStarsDisplay?.SetStars(stars);

        cursorWasVisible = Cursor.visible;
        cursorWasLockState = Cursor.lockState;

        SetVictoryPanel(true);
        SetPausePanel(false);
        SetDefeatPanel(false);


        disabledPlayerInputs.Clear();
        foreach (var p in FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (p.enabled)
            {
                p.enabled = false;
                disabledPlayerInputs.Add(p);
            }
        }

        pausedAudioSources.Clear();
        foreach (var audio in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (audio.isPlaying)
            {
                audio.Pause();
                pausedAudioSources.Add(audio);
            }
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ContinueGame() => ResumeGame();
    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void BackToMainMenu() => SceneManager.LoadScene("MainMenu");
    public void NextLevel()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (!currentScene.StartsWith("Level"))
        {
            Log.Error("[NextLevel] Current scene '{SceneName}' does not match format 'LevelN'", currentScene);
            return;
        }

        string numPart = currentScene[5..];
        if (!int.TryParse(numPart, out int currentLevel))
        {
            Log.Error("[NextLevel] Failed to parse level number from scene '{SceneName}'", currentScene);
            return;
        }

        int nextLevel = currentLevel + 1;
        string nextSceneName = $"Level{nextLevel}";

        SceneManager.LoadScene(nextSceneName);
    }

}
