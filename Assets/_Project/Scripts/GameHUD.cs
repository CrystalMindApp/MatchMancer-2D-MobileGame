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
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Button restartButton;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void OnDestroy()
    {
        restartButton.onClick.RemoveListener(OnRestartClicked);
    }

    private void Update()
    {
        Refresh();
    }

    #endregion

    #region Private Methods

    private void Refresh()
    {
        movesText.text = $"Moves: {gameManager.CurrentMoves} / {gameManager.MaxMoves}";
        clearedText.text = $"Cleared: {gameManager.ClearedTiles} / {gameManager.TargetClearedTiles}";
        stateText.text = $"State: {gameManager.CurrentState}";
    }

    private void OnRestartClicked()
    {
        gameManager.RestartGame();
    }

    #endregion
}