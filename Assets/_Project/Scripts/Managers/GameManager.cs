using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public enum GameState
    {
        Start,
        Playing,
        Win,
        Lose
    }

    public class GameManager : MonoBehaviour
    {
        #region Variables

        [Header("Move Settings")]
        [SerializeField, Range(1, 99)] private int maxMoves = 20;

        [Header("Win Settings")]
        [SerializeField, Range(1, 999)] private int targetClearedTiles = 30;

        [Header("References")]
        [SerializeField] private BoardManager boardManager;

        // Cache
        private int currentMoves;
        private int clearedTiles;

        // State
        private GameState currentState;

        #endregion

        #region Properties

        public int MaxMoves => maxMoves;
        public int CurrentMoves => currentMoves;
        public int ClearedTiles => clearedTiles;
        public int TargetClearedTiles => targetClearedTiles;
        public GameState CurrentState => currentState;
        public bool IsPlaying => currentState == GameState.Playing;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            SetState(GameState.Start);
        }

        private void Start()
        {
            StartGame();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        #endregion

        #region Public Methods

        public void StartGame()
        {
            ResetGameData();
            SetState(GameState.Playing);
        }

        public void RestartGame()
        {
            ResetGameData();

            if (boardManager != null)
            {
                boardManager.RestartBoard();
            }
            else
            {
                Debug.LogWarning("GameManager: BoardManager reference is missing.");
            }

            SetState(GameState.Playing);
        }

        public void OnValidMoveUsed()
        {
            if (!IsPlaying)
            {
                return;
            }

            currentMoves = Mathf.Max(0, currentMoves - 1);
            Debug.Log($"Move used. Remaining moves: {currentMoves}");
        }

        public void OnTilesCleared(int amount)
        {
            if (!IsPlaying)
            {
                return;
            }

            if (amount <= 0)
            {
                return;
            }

            clearedTiles += amount;
            Debug.Log($"Tiles cleared: {clearedTiles}/{targetClearedTiles}");
        }

        public void EvaluateGameResult()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (clearedTiles >= targetClearedTiles)
            {
                SetState(GameState.Win);
                Debug.Log("Game Result: WIN");
                return;
            }

            if (currentMoves <= 0)
            {
                SetState(GameState.Lose);
                Debug.Log("Game Result: LOSE");
            }
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Private Methods

        private void ResetGameData()
        {
            currentMoves = maxMoves;
            clearedTiles = 0;
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"Game State: {currentState}");
        }

        #endregion
    }
}
