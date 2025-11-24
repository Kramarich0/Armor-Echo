using System.Collections;
using System.Collections.Generic;
using Serilog;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class CapturePoint : MonoBehaviour
{
    [Header("Capture Settings")]
    public float captureTime = 10f;
    public float captureRatePerPlayer = 1f;
    public float neutralDecaySpeed = 1f;
    public float contestDecaySpeed = 4f;
    public int ticketsPerInterval = 10;
    public float ticketInterval = 1f;

    [Header("UI")]
    public Image captureBackgroundUI;
    public Image captureProgressUI;

    [Header("Audio / Setup")]
    public CapturePointEnum pointType = CapturePointEnum.Neutral;
    public TeamEnum startingTeam = TeamEnum.Neutral;

    private TeamEnum controllingTeam = TeamEnum.Neutral;
    public bool debugLogs = false;


    private float captureProgress = 0f;

    private Coroutine ticketCoroutine;

    private readonly Dictionary<TeamEnum, HashSet<string>> presentTankIds = new()
    {
        { TeamEnum.Friendly, new HashSet<string>() },
        { TeamEnum.Enemy,    new HashSet<string>() }
    };


    private const float EPS = 0.001f;

    void Start()
    {
        if (pointType == CapturePointEnum.Defense)
            controllingTeam = TeamEnum.Friendly;
        else if (pointType == CapturePointEnum.Attack)
            controllingTeam = TeamEnum.Enemy;
        else
            controllingTeam = startingTeam;

        if (controllingTeam == TeamEnum.Friendly) captureProgress = captureTime;
        else if (controllingTeam == TeamEnum.Enemy) captureProgress = -captureTime;
        else captureProgress = 0f;

        UpdateUI();
    }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            if (debugLogs) Log.Debug("[CapturePoint] Collider set to Trigger");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<TeamComponent>(out var tc))
            tc = other.GetComponentInParent<TeamComponent>();

        if (tc != null && tc.team != TeamEnum.Neutral)
        {
            if (presentTankIds[tc.team].Add(tc.tankId))
            {
                if (debugLogs) Log.Debug("[CapturePoint] Entered: {TankName} (ID: {TankId}), team={Team}. Count now: {TeamCount}",
                              tc.displayName ?? tc.gameObject.name, tc.tankId, tc.team, presentTankIds[tc.team].Count);
            }
        }
        else
        {
            if (debugLogs) Log.Debug("[CapturePoint] Entered by {OtherName}, but no TeamComponent (or Neutral)", other.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<TeamComponent>(out var tc))
            tc = other.GetComponentInParent<TeamComponent>();

        if (tc != null && tc.team != TeamEnum.Neutral)
        {
            if (presentTankIds[tc.team].Remove(tc.tankId))
            {
                if (debugLogs) Log.Debug("[CapturePoint] Exited: {TankName} (ID: {TankId}), team={Team}. Count now: {TeamCount}",
                              tc.displayName ?? tc.gameObject.name, tc.tankId, tc.team, presentTankIds[tc.team].Count);
            }
        }
    }
    void Update()
    {
        int friendlyCount = presentTankIds[TeamEnum.Friendly].Count;
        int enemyCount = presentTankIds[TeamEnum.Enemy].Count;
        int net = friendlyCount - enemyCount;

        if (controllingTeam != TeamEnum.Neutral)
        {
            if (friendlyCount == 0 && enemyCount == 0)
            {

            }

            else if (friendlyCount == enemyCount)
            {

                if ((controllingTeam == TeamEnum.Friendly && friendlyCount > 0) ||
                    (controllingTeam == TeamEnum.Enemy && enemyCount > 0))
                {
                    if (controllingTeam == TeamEnum.Friendly)
                    {
                        float speed = Mathf.Max(1, friendlyCount) * captureRatePerPlayer;
                        captureProgress = Mathf.MoveTowards(captureProgress, captureTime, Time.deltaTime * speed);
                    }
                    else
                    {
                        float speed = Mathf.Max(1, enemyCount) * captureRatePerPlayer;
                        captureProgress = Mathf.MoveTowards(captureProgress, -captureTime, Time.deltaTime * speed);
                    }
                }
                else
                {

                    captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime * neutralDecaySpeed);
                }
            }

            else if (net > 0)
            {
                float speed = Mathf.Max(1, net) * captureRatePerPlayer;
                captureProgress = Mathf.MoveTowards(captureProgress, captureTime, Time.deltaTime * speed);
            }
            else
            {
                float speed = Mathf.Max(1, -net) * captureRatePerPlayer;
                captureProgress = Mathf.MoveTowards(captureProgress, -captureTime, Time.deltaTime * speed);
            }
        }
        else
        {

            if (friendlyCount == 0 && enemyCount == 0)
            {
                captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime * neutralDecaySpeed);
            }
            else if (friendlyCount == enemyCount)
            {
                captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime * neutralDecaySpeed);
            }
            else if (net > 0)
            {
                float speed = Mathf.Max(1, net) * captureRatePerPlayer;
                captureProgress = Mathf.MoveTowards(captureProgress, captureTime, Time.deltaTime * speed);
            }
            else
            {
                float speed = Mathf.Max(1, -net) * captureRatePerPlayer;
                captureProgress = Mathf.MoveTowards(captureProgress, -captureTime, Time.deltaTime * speed);
            }
        }

        if (controllingTeam != TeamEnum.Neutral)
        {
            bool stillFullyOwned =
                (controllingTeam == TeamEnum.Friendly && captureProgress >= captureTime - EPS) ||
                (controllingTeam == TeamEnum.Enemy && captureProgress <= -captureTime + EPS);

            if (!stillFullyOwned)
            {

                LoseControl();
            }
        }

        if (captureProgress >= captureTime && controllingTeam != TeamEnum.Friendly)
        {
            SetControllingTeam(TeamEnum.Friendly);
        }
        else if (captureProgress <= -captureTime && controllingTeam != TeamEnum.Enemy)
        {
            SetControllingTeam(TeamEnum.Enemy);
        }

        captureProgress = Mathf.Clamp(captureProgress, -captureTime, captureTime);
        UpdateUI();
    }


    private void LoseControl()
    {
        if (ticketCoroutine != null)
        {
            StopCoroutine(ticketCoroutine);
            ticketCoroutine = null;
        }
        if (debugLogs) Log.Debug("[CapturePoint] Owner lost (progress {CaptureProgress}). Control removed.", captureProgress);
        controllingTeam = TeamEnum.Neutral;
    }

    private void UpdateUI()
    {
        if (captureProgressUI == null || captureBackgroundUI == null) return;

        float normalized = captureTime > 0f ? Mathf.Abs(captureProgress) / captureTime : 0f;
        captureProgressUI.fillAmount = normalized;

        if (captureProgress > 0.001f) captureProgressUI.color = Color.green;
        else if (captureProgress < -0.001f) captureProgressUI.color = Color.red;
        else captureProgressUI.color = Color.clear;

        captureBackgroundUI.fillAmount = 1f;
        captureBackgroundUI.color = Color.gray;
    }

    private void SetControllingTeam(TeamEnum team)
    {
        controllingTeam = team;
        captureProgress = (team == TeamEnum.Friendly) ? captureTime : -captureTime;


        if (ticketCoroutine != null) StopCoroutine(ticketCoroutine);
        ticketCoroutine = StartCoroutine(TicketDrainRoutine());

        Log.Information("[CapturePoint] Captured by team {Team}", team);

    }

    private IEnumerator TicketDrainRoutine()
    {

        while (controllingTeam != TeamEnum.Neutral)
        {
            yield return new WaitForSeconds(ticketInterval);


            bool fullyCaptured =
                (controllingTeam == TeamEnum.Friendly && captureProgress >= captureTime - EPS) ||
                (controllingTeam == TeamEnum.Enemy && captureProgress <= -captureTime + EPS);

            if (!fullyCaptured)
            {

                if (debugLogs) Log.Debug("[CapturePoint] TicketDrain: ownership no longer full — stopping drain.");
                yield break;
            }

            TeamEnum enemyTeam = controllingTeam == TeamEnum.Friendly ? TeamEnum.Enemy : TeamEnum.Friendly;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DrainTickets(enemyTeam, ticketsPerInterval);
                if (debugLogs) Log.Debug("[CapturePoint] Ticket drained from {EnemyTeam} by {TicketsPerInterval}", enemyTeam, ticketsPerInterval);
            }
        }
    }

    public void RemoveTeamComponent(TeamComponent tc)
    {
        if (tc == null || string.IsNullOrEmpty(tc.tankId))
        {
            Log.Warning("[CapturePoint] Attempted to remove invalid tank");
            return;
        }

        if (tc.team == TeamEnum.Neutral) return;

        if (presentTankIds.ContainsKey(tc.team))
        {
            if (presentTankIds[tc.team].Remove(tc.tankId))
            {
                if (debugLogs) Log.Debug("[CapturePoint] Removed tank ID: {TankId}, team={Team}", tc.tankId, tc.team);
            }
            else
            {
                if (debugLogs) Log.Debug("[CapturePoint] Tank with ID {TankId} not found in removal list", tc.tankId);
            }
        }
    }

    public TeamEnum GetControllingTeam() => controllingTeam;
    public float GetCaptureProgressNormalized() => captureTime > 0f ? Mathf.Abs(captureProgress) / captureTime : 0f;

    void OnDestroy()
    {
        presentTankIds[TeamEnum.Friendly].Clear();
        presentTankIds[TeamEnum.Enemy].Clear();
    }

    private void OnDisable()
    {
        if (ticketCoroutine != null)
        {
            StopCoroutine(ticketCoroutine);
            ticketCoroutine = null;
        }
    }

}
