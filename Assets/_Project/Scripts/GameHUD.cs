using CrystalMind.MatchMancer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    #region Variables

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text movesText;
    [SerializeField] private TMP_Text clearedText;
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Button restartButton;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
    }

    private void Update()
    {
        Refresh();
    }

    #endregion

    #region Private Methods

    private void Refresh()
    {
        if (gameManager == null)
        {
            return;
        }

        SetText(movesText, $"Moves: {gameManager.CurrentMoves} / {gameManager.MaxMoves}");
        SetText(clearedText, $"Cleared: {gameManager.ClearedTiles} / {gameManager.TargetClearedTiles}");
        SetText(playerHpText, $"Player HP: {gameManager.PlayerCurrentHp} / {gameManager.PlayerMaxHp}");
        SetText(enemyHpText, $"Enemy HP: {gameManager.EnemyCurrentHp} / {gameManager.EnemyMaxHp}");
        SetText(stateText, $"State: {gameManager.CurrentState}");
    }

    private void OnRestartClicked()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    #endregion
}
