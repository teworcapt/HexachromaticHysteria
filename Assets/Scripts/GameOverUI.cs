
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("Player 1 UI")]
    [SerializeField] private GameObject player1WinnerPanel;
    [SerializeField] private TMP_Text player1WinnerText;
    [SerializeField] private Button restartButtonP1;

    [Header("Player 2 UI")]
    [SerializeField] private GameObject player2WinnerPanel;
    [SerializeField] private TMP_Text player2WinnerText;
    [SerializeField] private Button restartButtonP2;

    private void Start()
    {
        HideWinner();

        if (restartButtonP1 != null)
            restartButtonP1.onClick.AddListener(RestartGame);

        if (restartButtonP2 != null)
            restartButtonP2.onClick.AddListener(RestartGame);
    }

    private void OnDestroy()
    {
        if (restartButtonP1 != null)
            restartButtonP1.onClick.RemoveListener(RestartGame);

        if (restartButtonP2 != null)
            restartButtonP2.onClick.RemoveListener(RestartGame);
    }

    public void ShowWinner(ulong winnerClientId)
    {
        string message = winnerClientId == 0
            ? "PLAYER 1 WINS!"
            : "PLAYER 2 WINS!";

        if (player1WinnerText != null)
            player1WinnerText.text = message;

        if (player2WinnerText != null)
            player2WinnerText.text = message;

        if (player1WinnerPanel != null)
            player1WinnerPanel.SetActive(true);

        if (player2WinnerPanel != null)
            player2WinnerPanel.SetActive(true);
    }

    public void HideWinner()
    {
        if (player1WinnerPanel != null)
            player1WinnerPanel.SetActive(false);

        if (player2WinnerPanel != null)
            player2WinnerPanel.SetActive(false);
    }

    public void RestartGame()
    {
        MatchTimer timer =
            FindFirstObjectByType<MatchTimer>();

        if (timer != null)
            timer.RequestRestartServerRpc();
        else
            Debug.LogError("Cannot restart: MatchTimer was not found.");
    }
}
