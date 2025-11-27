using System.Collections;
using System.Collections.Generic;
using Serilog;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static WaitForSecondsRealtime _waitForSecondsRealtime2;

    public static GameManager Instance { get; private set; }
    public TankSpawner tankSpawner;

    public int score = 0;
    public int initialFriendlyTickets;
    public int initialEnemyTickets;
    public int friendlyTickets;
    public int enemyTickets;
    public System.Action<int, int> OnTicketsChanged;
    public System.Action<int, int> OnTankCountChanged;
    public System.Action<string> OnKillLogUpdated;
    private int aliveEnemyTanks = 0;
    private int aliveFriendlyTanks = 0;
    public bool IsLevelInitialized { get; private set; }
    private bool isGameFinished = false;
    private readonly List<string> killLog = new();
    public int maxKillLogEntries = 10;
    public int MaxPossibleScore { get; private set; }
    public bool debugLogs = false;

    private GameObject cachedKillLogObj;
    private LocalizeStringEvent cachedKillLogLse;
    private TextMeshProUGUI cachedKillLogTmp;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _waitForSecondsRealtime2 = new WaitForSecondsRealtime(2f);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        isGameFinished = false;
        killLog.Clear();

        cachedKillLogObj = null;
        cachedKillLogLse = null;
        cachedKillLogTmp = null;
    }

    public void InitializeLevel()
    {
        if (IsLevelInitialized)
        {
            Log.Warning("[GameManager] Level already initialized");
            return;
        }

        if (PlayerSelection.selectedTank != null && tankSpawner != null)
        {
            GameObject playerTank = tankSpawner.SpawnPlayerTank(PlayerSelection.selectedTank);
            if (playerTank != null)
            {
                playerTank.tag = "Player";

                var healthDisplay = FindObjectOfType<HealthDisplay>();
                if (healthDisplay != null)
                    healthDisplay.Initialize(playerTank.GetComponent<PlayerTankHealth>());

                var sniperView = playerTank.GetComponentInChildren<TankSniperView>();
                if (sniperView != null)
                    sniperView.InitializeDynamicReferences();

                var cameraSetup = playerTank.AddComponent<TankCameraSetup>();
                cameraSetup.playerTank = playerTank;
                cameraSetup.InitializeCameras();
            }
        }

        aliveEnemyTanks = 0;
        aliveFriendlyTanks = 0;
        friendlyTickets = 0;
        enemyTickets = 0;

        TankAI[] allTanks = FindObjectsByType<TankAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var tank in allTanks)
        {
            if (tank == null) continue;
            else if (tank.GetComponent<TeamComponent>() is TeamComponent tc)
            {
                int cost = GetTankCost(tank);
                if (tc.team == TeamEnum.Enemy)
                {
                    enemyTickets += cost;
                    aliveEnemyTanks++;
                }
                else if (tc.team == TeamEnum.Friendly)
                {
                    friendlyTickets += cost;
                    aliveFriendlyTanks++;
                }
            }
        }

        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            friendlyTickets += 200;
            aliveFriendlyTanks++;
        }

        OnTicketsChanged?.Invoke(friendlyTickets, enemyTickets);
        OnTankCountChanged?.Invoke(aliveFriendlyTanks, aliveEnemyTanks);

        MaxPossibleScore = friendlyTickets + enemyTickets;
        initialFriendlyTickets = friendlyTickets;
        initialEnemyTickets = enemyTickets;

        if (debugLogs) Log.Debug("[GameManager] Tickets: Friendly={FriendlyTickets} | Enemy={EnemyTickets}", friendlyTickets, enemyTickets);
        if (debugLogs) Log.Debug("[GameManager] Tanks: Friendly={AliveFriendlyTanks} | Enemy={AliveEnemyTanks}", aliveFriendlyTanks, aliveEnemyTanks);

        IsLevelInitialized = true;
    }

    int GetTankCost(TankAI tank)
    {
        return tank.CurrentTankClass switch
        {
            TankClass.Light => 100,
            TankClass.Medium => 200,
            TankClass.Heavy => 300,
            _ => 150
        };
    }


    public void OnTankDestroyed(TeamComponent victimTeamComponent, int ticketCost, string killerName = null, string victimName = null, bool killerIsPlayer = false)
    {
        if (isGameFinished || victimTeamComponent == null) return;

        if (victimTeamComponent.gameObject == null || !victimTeamComponent.gameObject.activeInHierarchy)
        {
            Log.Warning("[GameManager] Attempted to process death of already destroyed object: {VictimName}", victimName);
            return;
        }

        TeamEnum team = victimTeamComponent.team;
        if (team == TeamEnum.Neutral) return;

        if (victimTeamComponent.gameObject != null && victimTeamComponent.gameObject.activeInHierarchy)
        {
            CapturePoint[] allPoints = FindObjectsByType<CapturePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CapturePoint point in allPoints)
            {
                if (point != null)
                {
                    point.RemoveTeamComponent(victimTeamComponent);
                }
            }
        }

        string victimColor = team == TeamEnum.Friendly ? "#00FF00" : "#FF0000";
        string victimDisplay = victimName ?? (team == TeamEnum.Friendly ? LocalizationHelper.GetLocalizedString("ally") : LocalizationHelper.GetLocalizedString("enemy"));
        victimDisplay = $"<color={victimColor}>{victimDisplay}</color>";

        string entry;
        if (!string.IsNullOrEmpty(killerName))
        {
            TeamEnum killerTeam = team == TeamEnum.Friendly ? TeamEnum.Enemy : TeamEnum.Friendly;
            string killerColor = killerTeam == TeamEnum.Friendly ? "#00FF00" : "#FF0000";
            string killerDisplay = $"<color={killerColor}>{killerName}</color>";

            string youText = killerIsPlayer ? LocalizationHelper.GetLocalizedString("you_text") : "";

            entry = LocalizationHelper.GetLocalizedString("kill_message_with_player", killerDisplay, youText, victimDisplay);
        }
        else
        {
            entry = LocalizationHelper.GetLocalizedString("kill_message_without_player", victimDisplay);
        }


        AddKillLog(entry);

        if (team == TeamEnum.Friendly)
        {
            friendlyTickets = Mathf.Max(0, friendlyTickets - ticketCost);
            aliveFriendlyTanks = Mathf.Max(0, aliveFriendlyTanks - 1);
        }
        else if (team == TeamEnum.Enemy)
        {
            enemyTickets = Mathf.Max(0, enemyTickets - ticketCost);
            aliveEnemyTanks = Mathf.Max(0, aliveEnemyTanks - 1);
            score += ticketCost;
        }

        OnTicketsChanged?.Invoke(friendlyTickets, enemyTickets);
        OnTankCountChanged?.Invoke(aliveFriendlyTanks, aliveEnemyTanks);

        CheckVictory();
    }

    void CheckVictory()
    {
        if (isGameFinished) return;

        if (friendlyTickets <= 0)
        {
            Log.Information("[GameManager] DEFEAT! All friendly tanks destroyed");
            isGameFinished = true;
            StartCoroutine(ShowDefeatAfterDelay());
        }
        else if (enemyTickets <= 0 || aliveEnemyTanks <= 0)
        {
            Log.Information("[GameManager] VICTORY! All enemy tanks destroyed");
            isGameFinished = true;
            CompleteLevel();
        }
    }

    private IEnumerator ShowDefeatAfterDelay()
    {
        yield return _waitForSecondsRealtime2;
        GameUIManager.Instance?.ShowDefeatScreen();
    }

    void CompleteLevel()
    {
        int finalScore = score + friendlyTickets;
        int stars = CalculateStars(finalScore);
        string sceneName = SceneManager.GetActiveScene().name;

        int levelIndex = 0;
        if (sceneName.StartsWith("Level"))
        {
            string numPart = sceneName[5..];
            if (int.TryParse(numPart, out int idx)) levelIndex = idx;
        }

        int savedScore = PlayerPrefs.GetInt($"Level{levelIndex}_Score", 0);
        int savedStars = PlayerPrefs.GetInt($"Level{levelIndex}_Stars", 0);

        if (finalScore > savedScore)
            PlayerPrefs.SetInt($"Level{levelIndex}_Score", finalScore);

        if (stars > savedStars)
            PlayerPrefs.SetInt($"Level{levelIndex}_Stars", stars);

        PlayerPrefs.SetInt($"Level{levelIndex}_Completed", 1);
        PlayerPrefs.SetInt($"Level{levelIndex + 1}_Unlocked", 1);

        Log.Information("[GameManager] Level {LevelIndex} completed! Stars: {Stars}, Score: {Score}", levelIndex, stars, score);

        StartCoroutine(ShowVictoryAfterDelay(finalScore, stars));
        StartCoroutine(DelayedSave());
    }

    private IEnumerator ShowVictoryAfterDelay(int finalScore, int stars)
    {
        yield return _waitForSecondsRealtime2;
        GameUIManager.Instance?.ShowVictoryScreen(finalScore, stars);
    }

    IEnumerator DelayedSave()
    {
        yield return null;
        PlayerPrefs.Save();
    }

    int CalculateStars(int actualScore)
    {
        if (MaxPossibleScore <= 0)
            return 0;

        float ratio = (float)actualScore / MaxPossibleScore;

        if (ratio >= 0.99f) return 3;
        if (ratio >= 0.70f) return 2;
        if (ratio >= 0.10f) return 1;
        return 0;
    }

    public void OnPlayerTankDestroyed()
    {
        if (isGameFinished) return;

        Log.Information("[GameManager] Player destroyed. Game over");
        isGameFinished = true;
        StartCoroutine(ShowDefeatAfterDelay());
    }

    public void DrainTickets(TeamEnum team, int amount)
    {
        if (team == TeamEnum.Friendly)
            friendlyTickets = Mathf.Max(0, friendlyTickets - amount);
        else if (team == TeamEnum.Enemy)
            enemyTickets = Mathf.Max(0, enemyTickets - amount);

        OnTicketsChanged?.Invoke(friendlyTickets, enemyTickets);
        CheckVictory();
    }

    private void AddKillLog(string entry)
    {
        if (string.IsNullOrEmpty(entry)) return;

        killLog.Add(entry);

        if (killLog.Count > maxKillLogEntries)
            killLog.RemoveAt(0);

        if (cachedKillLogObj == null)
        {
            cachedKillLogObj = GameObject.Find("KillLogList");
            if (cachedKillLogObj != null)
            {
                cachedKillLogLse = cachedKillLogObj.GetComponent<LocalizeStringEvent>();
                cachedKillLogTmp = cachedKillLogObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                cachedKillLogLse = FindFirstObjectByType<LocalizeStringEvent>();
                if (cachedKillLogLse != null)
                    cachedKillLogObj = cachedKillLogLse.gameObject;
                else
                {
                    cachedKillLogTmp = FindFirstObjectByType<TextMeshProUGUI>();
                    if (cachedKillLogTmp != null)
                        cachedKillLogObj = cachedKillLogTmp.gameObject;
                }
            }
        }

        string fullLog = string.Join("\n", killLog);

        if (cachedKillLogLse != null)
        {
            LocalizationHelper.SetLocalizedText(cachedKillLogLse, "kill_log_text", fullLog);
        }
        else if (cachedKillLogTmp != null)
        {
            cachedKillLogTmp.text = fullLog;
        }

        OnKillLogUpdated?.Invoke(entry);
    }
}