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

        [Header("Combat Settings")]
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private bool enableCombatDebugLogs = true;
        [SerializeField, Min(1)] private int playerMaxHp = 100;
        [SerializeField] private int playerCurrentHp;
        [SerializeField, Min(1)] private int enemyMaxHp = 100;
        [SerializeField] private int enemyCurrentHp;
        [SerializeField, Min(0)] private int enemyAttackDamage = 10;
        [SerializeField, Min(0)] private int baseDamagePerTile = 2;

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
        public int PlayerMaxHp => GetPlayerMaxHp();
        public int PlayerCurrentHp => Mathf.Max(0, playerCurrentHp);
        public int EnemyMaxHp => GetEnemyMaxHp();
        public int EnemyCurrentHp => Mathf.Max(0, enemyCurrentHp);
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

        public void OnPlayerMoveResolved(int clearedTileCount)
        {
            if (!IsPlaying)
            {
                return;
            }

            LogCombat($"Cleared tile count this move: {clearedTileCount}");
            ApplyPlayerDamage(clearedTileCount);

            if (enemyCurrentHp <= 0)
            {
                SetState(GameState.Win);
                LogCombat("Game Result: WIN - Enemy defeated.");
                return;
            }

            EnemyAttack();

            if (playerCurrentHp <= 0)
            {
                SetState(GameState.Lose);
                LogCombat("Game Result: LOSE - Player defeated.");
                return;
            }

            if (currentMoves <= 0)
            {
                SetState(GameState.Lose);
                LogCombat("Game Result: LOSE - No moves remaining.");
            }
        }

        public void EvaluateGameResult()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (enemyCurrentHp <= 0)
            {
                SetState(GameState.Win);
                LogCombat("Game Result: WIN - Enemy defeated.");
                return;
            }

            if (playerCurrentHp <= 0)
            {
                SetState(GameState.Lose);
                LogCombat("Game Result: LOSE - Player defeated.");
                return;
            }

            if (currentMoves <= 0 && enemyCurrentHp > 0)
            {
                SetState(GameState.Lose);
                LogCombat("Game Result: LOSE - No moves remaining.");
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
            playerCurrentHp = GetPlayerMaxHp();
            enemyCurrentHp = GetEnemyMaxHp();
        }

        private void ApplyPlayerDamage(int clearedTileCount)
        {
            int damage = clearedTileCount * GetBaseDamagePerTile();
            enemyCurrentHp = Mathf.Max(0, enemyCurrentHp - damage);

            LogCombat($"Player deals {damage} damage. Enemy HP: {enemyCurrentHp}");
        }

        private void EnemyAttack()
        {
            int damage = GetEnemyAttackDamage();
            playerCurrentHp = Mathf.Max(0, playerCurrentHp - damage);

            LogCombat($"Enemy attacks for {damage}. Player HP: {playerCurrentHp}");
        }

        private int GetPlayerMaxHp()
        {
            return combatConfig != null ? combatConfig.PlayerMaxHp : playerMaxHp;
        }

        private int GetEnemyMaxHp()
        {
            return combatConfig != null ? combatConfig.EnemyMaxHp : enemyMaxHp;
        }

        private int GetEnemyAttackDamage()
        {
            return combatConfig != null ? combatConfig.EnemyAttackDamage : enemyAttackDamage;
        }

        private int GetBaseDamagePerTile()
        {
            return combatConfig != null ? combatConfig.BaseDamagePerTile : baseDamagePerTile;
        }

        private void LogCombat(string message)
        {
            if (enableCombatDebugLogs)
            {
                Debug.Log(message);
            }
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"Game State: {currentState}");
        }

        #endregion
    }
}
