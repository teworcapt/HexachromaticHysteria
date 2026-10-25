
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerLives : NetworkBehaviour
{
    [Header("Lives Settings")]
    [SerializeField] private int startingLives = 5;

    [Header("UI")]
    [SerializeField] private TMP_Text livesText;
    private static bool gameOver;

    private NetworkVariable<int> currentLives = new NetworkVariable<int>(
        5,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int CurrentLives => currentLives.Value;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentLives.Value = startingLives;

        currentLives.OnValueChanged += OnLivesChanged;

            FindLivesText();

        UpdateLivesDisplay(CurrentLives);
    }

    public override void OnNetworkDespawn()
    {
        currentLives.OnValueChanged -= OnLivesChanged;
    }

    private void OnLivesChanged(int previousValue, int newValue)
    {
        UpdateLivesDisplay(newValue);
    }

    private void FindLivesText()
    {
        string textObjectName =
            OwnerClientId == NetworkManager.ServerClientId
                ? "LivesText_P1"
                : "LivesText_P2";

        GameObject textObject = GameObject.Find(textObjectName);

        if (textObject == null)
        {
            Debug.LogError($"Could not find {textObjectName}!");
            return;
        }

        livesText = textObject.GetComponent<TMPro.TMP_Text>();

        if (livesText == null)
        {
            Debug.LogError($"{textObjectName} has no TMP_Text component!");
            return;
        }

        UpdateLivesDisplay(CurrentLives);
    }

    public void LoseLife()
    {
        if (!IsServer || currentLives.Value <= 0)
            return;

        currentLives.Value--;

        Debug.Log(
            $"{gameObject.name} lost a life! Lives remaining: {currentLives.Value}"
        );

        if (currentLives.Value <= 0 && !gameOver)
        {
            gameOver = true;

            // Stop enemy spawning when the match ends.
            FindFirstObjectByType<EnemySpawner>()?.StopSpawning();

            ulong winnerClientId =
                OwnerClientId == NetworkManager.ServerClientId
                    ? 1UL
                    : NetworkManager.ServerClientId;

            ShowWinnerClientRpc(winnerClientId);
        }
    }

    private void UpdateLivesDisplay(int lives)
    {
        if (livesText != null)
            livesText.text = "LIVES: " + lives;
    }

    public void ResetLivesServer()
    {
        gameOver = false;
        if (!IsServer)
            return;

        currentLives.Value = startingLives;
    }

    [ClientRpc]
    private void ShowWinnerClientRpc(ulong winnerClientId)
    {
        GameOverUI ui = FindFirstObjectByType<GameOverUI>();

        if (ui != null)
            ui.ShowWinner(winnerClientId);
    }

}
