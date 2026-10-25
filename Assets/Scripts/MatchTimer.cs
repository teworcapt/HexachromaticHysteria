
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class MatchTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float matchDuration = 120f;

    [Header("Timer Displays")]
    [SerializeField] private TMP_Text MatchTimer_1;
    [SerializeField] private TMP_Text MatchTimer_2;

    private NetworkVariable<float> timeRemaining =
        new NetworkVariable<float>(
            120f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<bool> timerStarted =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private EnemySpawner enemySpawner;


    public override void OnNetworkSpawn()
    {
        Debug.Log($"[MatchTimer] Spawned! IsServer={IsServer}, IsClient={IsClient}");

        if (IsServer)
            timeRemaining.Value = matchDuration;

        GameObject p1Canvas = GameObject.Find("Player1Canvas");
        GameObject p2Canvas = GameObject.Find("Player2Canvas");

        Debug.Log($"[MatchTimer] P1 Canvas found: {p1Canvas != null}");
        Debug.Log($"[MatchTimer] P2 Canvas found: {p2Canvas != null}");

        if (p1Canvas != null)
        {
            Transform timer = p1Canvas.transform.Find("MatchTimer_1");
            Debug.Log($"[MatchTimer] P1 timer found: {timer != null}");

            if (timer != null)
                MatchTimer_1 = timer.GetComponentInChildren<TMP_Text>();
        }

        if (p2Canvas != null)
        {
            Transform timer = p2Canvas.transform.Find("MatchTimer_2");
            Debug.Log($"[MatchTimer] P2 timer found: {timer != null}");

            if (timer != null)
                MatchTimer_2 = timer.GetComponentInChildren<TMP_Text>();
        }

        timeRemaining.OnValueChanged += OnTimeChanged;
        timerStarted.OnValueChanged += OnTimerStartedChanged;

        enemySpawner = FindFirstObjectByType<EnemySpawner>();

        Debug.Log($"[MatchTimer] EnemySpawner found: {enemySpawner != null}");

        UpdateTimerDisplay(timeRemaining.Value);
    }


    public override void OnNetworkDespawn()
    {
        timeRemaining.OnValueChanged -= OnTimeChanged;
        timerStarted.OnValueChanged -= OnTimerStartedChanged;
    }

    private void Update()
    {
        if (!IsServer || enemySpawner == null)
            return;

        if (!timerStarted.Value && enemySpawner.MatchStarted)
            timerStarted.Value = true;

        if (!timerStarted.Value || timeRemaining.Value <= 0f)
            return;

        timeRemaining.Value = Mathf.Max(
            0f,
            timeRemaining.Value - Time.deltaTime
        );
    }

    private void OnTimeChanged(float oldValue, float newValue)
    {
        UpdateTimerDisplay(newValue);
    }

    private void OnTimerStartedChanged(bool oldValue, bool newValue)
    {
        UpdateTimerDisplay(timeRemaining.Value);
    }

    private void UpdateTimerDisplay(float time)
    {
        int totalSeconds = Mathf.CeilToInt(time);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        string timeText = $"{minutes:00}:{seconds:00}";

        if (MatchTimer_1 != null)
            MatchTimer_1.text = timeText;

        if (MatchTimer_2 != null)
            MatchTimer_2.text = timeText;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestRestartServerRpc(
        RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        // Only allow one restart request at a time.
        if (restartInProgress)
            return;

        restartInProgress = true;

        EnemySpawner spawner =
            FindFirstObjectByType<EnemySpawner>();

        if (spawner == null)
        {
            Debug.LogError("Cannot restart: EnemySpawner not found.");
            restartInProgress = false;
            return;
        }

        // Restore lives for both players.
        PlayerLives[] players =
            FindObjectsByType<PlayerLives>(
                FindObjectsSortMode.None
            );

        foreach (PlayerLives player in players)
            player.ResetLivesServer();

        // Reset the timer before starting the new round.
        timerStarted.Value = false;
        timeRemaining.Value = matchDuration;

        // Reset enemies and begin the next round.
        spawner.RestartMatch();

        // Hide winner panels on all clients.
        HideWinnerClientRpc();

        restartInProgress = false;
    }

    private bool restartInProgress;

    [ClientRpc]
    private void HideWinnerClientRpc()
    {
        GameOverUI ui = FindFirstObjectByType<GameOverUI>();

        if (ui != null)
            ui.HideWinner();
    }

}
