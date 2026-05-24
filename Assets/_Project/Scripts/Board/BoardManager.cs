using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class BoardManager : MonoBehaviour
    {
        #region Variables

        [Header("Board Settings")]
        [SerializeField, Range(3, 12)] private int rows = 8;
        [SerializeField, Range(3, 12)] private int cols = 8;
        [SerializeField, Range(0.5f, 2f)] private float tileSpacing = 1.1f;

        [Header("Resolve Settings")]
        [SerializeField, Range(1, 30)] private int maxResolveLoops = 10;
        [SerializeField, Range(0f, 1f)] private float resolveStepDelay = 0.3f;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float tileSwapDuration = 0.15f;
        [SerializeField, Min(0f)] private float tileFallDuration = 0.18f;
        [SerializeField, Min(0f)] private float tileFallSpeed = 8f;
        [SerializeField, Min(0f)] private float minTileFallDuration = 0.08f;
        [SerializeField, Min(0f)] private float maxTileFallDuration = 0.35f;
        [SerializeField, Min(0f)] private float tileRefillSpawnOffset = 1f;
        [SerializeField] private AnimationCurve tileSwapCurve;
        [SerializeField] private AnimationCurve tileFallCurve;

        [Header("Board Intro Animation")]
        [SerializeField] private bool enableBoardIntroAnimation = true;
        [SerializeField, Min(0f)] private float boardIntroDuration = 0.2f;
        [SerializeField, Range(0f, 1f)] private float boardIntroStartScale = 0.2f;
        [SerializeField, Min(0f)] private float boardIntroEndScale = 1f;
        [SerializeField, Min(1f)] private float boardIntroOvershootScale = 1.08f;
        [SerializeField, Min(0f)] private float boardIntroStaggerDelay = 0.004f;
        [SerializeField] private bool boardIntroUseUnscaledTime;

        [Header("Combo Polish")]
        [SerializeField, Min(1)] private int maxComboImpactLevel = 5;
        [SerializeField, Min(0f)] private float maxComboShakeForce = 0.08f;

        [Header("Tile Effects")]
        [SerializeField] private GameObject tileDestroyEffectPrefab;
        [SerializeField] private GameObject bombTileDestroyEffectPrefab;
        [SerializeField] private GameObject horizontalTileDestroyEffectPrefab;
        [SerializeField] private GameObject verticalTileDestroyEffectPrefab;
        [SerializeField, Min(0f)] private float tileDestroyEffectLifetime = 1f;
        [SerializeField] private Transform tileDestroyEffectParent;
        [SerializeField, Min(0f)] private float specialWaveStepDelay = 0.035f;
        [SerializeField, Min(0f)] private float bombWaveStepDelay = 0.045f;
        [SerializeField, Min(0f)] private float lineWaveStepDelay = 0.035f;

        [Header("Generation Settings")]
        [SerializeField, Range(1, 100)] private int maxInitialBoardGenerationAttempts = 25;

        [Header("References")]
        [SerializeField] private Tile tilePrefab;
        [SerializeField] private Transform boardRoot;
        [SerializeField] private Camera inputCamera;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BoardComboTextController comboTextController;
        [SerializeField] private SceneAudioLibrary sceneAudioLibrary;
        [SerializeField] private CinemachineTinyImpulse tinyImpulse;

        // Cache
        private Tile[,] boardTiles;
        private Tile selectedTile;
        private MatchFinder matchFinder;
        private Tile recentSwapFirstTile;
        private Tile recentSwapSecondTile;
        private readonly Stack<Tile> tilePool = new Stack<Tile>();

        // State
        private bool isResolving;
        private bool isSwapping;
        private bool isInputBlocked;
        private bool isExternalInputBlocked;
        private bool spawnBoardTilesAtIntroScale;
        private bool hasPlayedMaxComboImpact;
        private int lastResolveClearedTileCount;
        private int currentResolveComboGroupCount;

        private struct TileMoveAnimation
        {
            public Tile Tile;
            public Vector3 StartPosition;
            public Vector3 TargetPosition;
            public float Duration;

            public TileMoveAnimation(Tile tile, Vector3 startPosition, Vector3 targetPosition, float duration)
            {
                Tile = tile;
                StartPosition = startPosition;
                TargetPosition = targetPosition;
                Duration = duration;
            }
        }

        #endregion

        #region Properties

        public int Rows => rows;
        public int Cols => cols;
        public bool IsResolving => isResolving;
        public bool CanReceiveInput => !isResolving &&
            !isSwapping &&
            !isInputBlocked &&
            !isExternalInputBlocked &&
            (gameManager == null || gameManager.IsPlaying);

        #endregion

        #region Unity Methods

        private void Awake()
        {
            matchFinder = new MatchFinder(this);
        }

        private void Start()
        {
            StartCoroutine(InitializeBoardRoutine());
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDrawGizmosSelected()
        {
            DrawBoardGizmo();
        }

        #endregion

        #region Public Methods

        public void HandleTileClicked(Tile clickedTile)
        {
            if (!CanReceiveInput)
            {
                return;
            }

            if (clickedTile == null)
            {
                return;
            }

            if (selectedTile == null)
            {
                SelectTile(clickedTile);
                return;
            }

            if (selectedTile == clickedTile)
            {
                ClearSelection();
                return;
            }

            if (!AreAdjacent(selectedTile, clickedTile))
            {
                SelectTile(clickedTile);
                return;
            }

            TrySwapSelectedTile(clickedTile);
        }

        public Tile GetTile(int row, int col)
        {
            if (!IsValidCoordinate(row, col))
            {
                return null;
            }

            return boardTiles[row, col];
        }

        public bool IsValidCoordinate(int row, int col)
        {
            return row >= 0 && row < rows && col >= 0 && col < cols;
        }

        public void RestartBoard()
        {
            StopAllCoroutines();
            StartCoroutine(RestartBoardRoutine());
        }

        public bool ValidateBoardData()
        {
            if (boardTiles == null)
            {
                Debug.LogError("Board validation failed: boardTiles is null.");
                return false;
            }

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Tile tile = boardTiles[row, col];

                    if (tile == null)
                    {
                        Debug.LogError($"Board validation failed: Missing tile at [{row}, {col}]");
                        return false;
                    }

                    if (tile.Row != row || tile.Col != col)
                    {
                        Debug.LogError($"Board validation failed: Tile coordinate mismatch at [{row}, {col}]. Tile has [{tile.Row}, {tile.Col}].");
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryActivateColorClearSkill(TileType targetType)
        {
            if (isResolving || isSwapping || (gameManager != null && !gameManager.IsPlaying))
            {
                return false;
            }

            if (!HasTileOfType(targetType))
            {
                return false;
            }

            StartCoroutine(ColorClearSkillRoutine(targetType));
            return true;
        }

        public bool HasTileOfType(TileType targetType)
        {
            return GetActiveTiles()
                .Any(tile => tile != null && tile.Type == targetType);
        }

        public void SetInputBlocked(bool blocked)
        {
            isExternalInputBlocked = blocked;

            if (blocked)
            {
                ClearSelection();
            }
        }

        public bool TryCreateRandomBomb()
        {
            List<Tile> normalTiles = GetActiveTiles()
                .Where(tile => tile != null && !tile.IsSpecial)
                .ToList();

            if (normalTiles.Count == 0)
            {
                return false;
            }

            Tile targetTile = normalTiles[UnityEngine.Random.Range(0, normalTiles.Count)];
            SetTileSpecialType(targetTile, SpecialTileType.Bomb, true);
            Debug.Log($"BoardManager: Passive created Bomb at [{targetTile.Row}, {targetTile.Col}].");
            return true;
        }

        public bool TryRemoveRandomSpecialTile()
        {
            List<Tile> specialTiles = GetActiveTiles()
                .Where(tile => tile != null && tile.IsSpecial)
                .ToList();

            if (specialTiles.Count == 0)
            {
                return false;
            }

            Tile targetTile = specialTiles[UnityEngine.Random.Range(0, specialTiles.Count)];
            targetTile.SetSpecialType(SpecialTileType.None);
            Debug.Log($"BoardManager: Enemy disruption removed special tile at [{targetTile.Row}, {targetTile.Col}].");
            return true;
        }

        private struct TileClearVisual
        {
            public Tile Tile;
            public TileDestroyContext DestroyContext;
            public float Delay;

            public TileClearVisual(Tile tile, TileDestroyContext destroyContext, float delay)
            {
                Tile = tile;
                DestroyContext = destroyContext;
                Delay = delay;
            }
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Private Methods

        private void HandleInput()
        {
            if (!CanReceiveInput)
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("BoardManager: Input camera is missing.");
                return;
            }

            Vector3 worldPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 clickPoint = new Vector2(worldPosition.x, worldPosition.y);

            RaycastHit2D hit = Physics2D.Raycast(clickPoint, Vector2.zero);
            if (hit.collider == null)
            {
                return;
            }

            Tile clickedTile = hit.collider.GetComponent<Tile>();
            if (clickedTile == null)
            {
                return;
            }

            HandleTileClicked(clickedTile);
        }

        private void GenerateBoard()
        {
            ClearActiveBoardTiles();

            if (tilePrefab == null)
            {
                Debug.LogError("BoardManager: Tile prefab is missing.");
                return;
            }

            boardTiles = new Tile[rows, cols];

            for (int attempt = 0; attempt < maxInitialBoardGenerationAttempts; attempt++)
            {
                ClearActiveBoardTiles();
                boardTiles = new Tile[rows, cols];
                spawnBoardTilesAtIntroScale = ShouldPlayBoardIntroAnimation();
                FillInitialBoardWithoutMatches();
                spawnBoardTilesAtIntroScale = false;

                if (HasAnyValidMove())
                {
                    return;
                }
            }

            spawnBoardTilesAtIntroScale = false;
            Debug.LogWarning($"BoardManager: Failed to generate a starting board with a valid move after {maxInitialBoardGenerationAttempts} attempts. Keeping latest board.");
        }

        private void FillInitialBoardWithoutMatches()
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    TileType tileType = GetRandomInitialTileType(row, col);
                    Tile tile = GetTileFromPool(row, col, tileType);
                    boardTiles[row, col] = tile;
                }
            }
        }

        private TileType GetRandomInitialTileType(int row, int col)
        {
            List<TileType> availableTypes = new List<TileType>();

            foreach (TileType tileType in System.Enum.GetValues(typeof(TileType)))
            {
                if (WouldCreateInitialMatch(row, col, tileType))
                {
                    continue;
                }

                availableTypes.Add(tileType);
            }

            if (availableTypes.Count == 0)
            {
                return GetRandomTileType();
            }

            return availableTypes[UnityEngine.Random.Range(0, availableTypes.Count)];
        }

        private bool WouldCreateInitialMatch(int row, int col, TileType tileType)
        {
            bool createsHorizontalMatch = col >= 2 &&
                boardTiles[row, col - 1] != null &&
                boardTiles[row, col - 2] != null &&
                boardTiles[row, col - 1].Type == tileType &&
                boardTiles[row, col - 2].Type == tileType;

            bool createsVerticalMatch = row >= 2 &&
                boardTiles[row - 1, col] != null &&
                boardTiles[row - 2, col] != null &&
                boardTiles[row - 1, col].Type == tileType &&
                boardTiles[row - 2, col].Type == tileType;

            return createsHorizontalMatch || createsVerticalMatch;
        }

        private bool HasAnyValidMove()
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (WouldSwapCreateMatch(row, col, row, col + 1) ||
                        WouldSwapCreateMatch(row, col, row + 1, col))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool WouldSwapCreateMatch(int firstRow, int firstCol, int secondRow, int secondCol)
        {
            if (!IsValidCoordinate(firstRow, firstCol) || !IsValidCoordinate(secondRow, secondCol))
            {
                return false;
            }

            Tile firstTile = boardTiles[firstRow, firstCol];
            Tile secondTile = boardTiles[secondRow, secondCol];

            if (firstTile == null || secondTile == null || firstTile.Type == secondTile.Type)
            {
                return false;
            }

            TileType firstType = firstTile.Type;
            TileType secondType = secondTile.Type;

            return WouldTileHaveMatchAt(firstRow, firstCol, secondType, secondRow, secondCol) ||
                WouldTileHaveMatchAt(secondRow, secondCol, firstType, firstRow, firstCol);
        }

        private bool WouldTileHaveMatchAt(int row, int col, TileType tileType, int swappedRow, int swappedCol)
        {
            int horizontalCount = 1 +
                CountVirtualMatches(row, col, 0, -1, tileType, swappedRow, swappedCol) +
                CountVirtualMatches(row, col, 0, 1, tileType, swappedRow, swappedCol);

            if (horizontalCount >= 3)
            {
                return true;
            }

            int verticalCount = 1 +
                CountVirtualMatches(row, col, -1, 0, tileType, swappedRow, swappedCol) +
                CountVirtualMatches(row, col, 1, 0, tileType, swappedRow, swappedCol);

            return verticalCount >= 3;
        }

        private int CountVirtualMatches(int row, int col, int rowDirection, int colDirection, TileType tileType, int swappedRow, int swappedCol)
        {
            int count = 0;
            int currentRow = row + rowDirection;
            int currentCol = col + colDirection;

            while (IsValidCoordinate(currentRow, currentCol))
            {
                if (currentRow == swappedRow && currentCol == swappedCol)
                {
                    break;
                }

                Tile tile = boardTiles[currentRow, currentCol];

                if (tile == null || tile.Type != tileType)
                {
                    break;
                }

                count++;
                currentRow += rowDirection;
                currentCol += colDirection;
            }

            return count;
        }

        private IEnumerator RestartBoardRoutine()
        {
            isResolving = true;
            isSwapping = false;
            ClearSelection();
            GenerateBoard();
            ValidateBoardData();
            yield return StartCoroutine(PlayBoardIntroAnimationRoutine());
            isResolving = false;
        }

        private IEnumerator InitializeBoardRoutine()
        {
            isInputBlocked = true;
            GenerateBoard();
            ValidateBoardData();
            yield return StartCoroutine(PlayBoardIntroAnimationRoutine());
            isInputBlocked = false;
        }

        private IEnumerator TrySwapRoutine(Tile firstTile, Tile secondTile)
        {
            isResolving = true;
            isSwapping = true;
            ClearSelection();

            SwapTiles(firstTile, secondTile, false);
            yield return StartCoroutine(AnimateTileSwap(firstTile, secondTile));

            List<MatchGroup> matchGroups = matchFinder.FindMatchGroups();
            if (matchGroups.Count == 0)
            {
                PlayFailedSwapSfx();
                SwapTiles(firstTile, secondTile, false);
                yield return StartCoroutine(AnimateTileSwap(firstTile, secondTile));

                isSwapping = false;
                isResolving = false;
                yield break;
            }

            isSwapping = false;
            recentSwapFirstTile = firstTile;
            recentSwapSecondTile = secondTile;

            gameManager?.OnValidMoveUsed();

            yield return StartCoroutine(ResolveBoardRoutine(true));

            recentSwapFirstTile = null;
            recentSwapSecondTile = null;
            isSwapping = false;
            isResolving = false;

            gameManager?.OnPlayerMoveResolved(lastResolveClearedTileCount);
        }

        private IEnumerator ResolveBoardRoutine(bool countClearedTiles, bool resetClearedCount = true, int comboOffset = 0)
        {
            if (resetClearedCount)
            {
                lastResolveClearedTileCount = 0;
                ResetComboFeedback();
            }

            for (int loopCount = 0; loopCount < maxResolveLoops; loopCount++)
            {
                List<MatchGroup> matchGroups = matchFinder.FindMatchGroups();

                if (matchGroups.Count == 0)
                {
                    HideComboFeedback();
                    ValidateBoardData();
                    yield break;
                }

                int comboLevel = comboOffset + loopCount + 1;
                UpdateComboFeedback(matchGroups.Count);
                PlayComboSfx(comboLevel);
                TryPlayMaxComboImpact(comboLevel);

                ClearStepResult clearResult = new ClearStepResult();

                if (countClearedTiles)
                {
                    yield return StartCoroutine(ProcessMatchGroupsRoutine(matchGroups, result => clearResult = result));
                }
                else
                {
                    yield return StartCoroutine(ClearTilesAndGetResultRoutine(GetTilesFromMatchGroups(matchGroups), result => clearResult = result));
                }

                if (countClearedTiles)
                {
                    int comboCount = comboOffset + loopCount + 1;
                    lastResolveClearedTileCount += clearResult.ClearedCount;
                    gameManager?.OnTilesCleared(clearResult.ClearedCount);
                    gameManager?.OnTileColorsCleared(clearResult.ColorCounts, comboCount);
                }

                yield return new WaitForSeconds(resolveStepDelay);

                yield return StartCoroutine(ApplyGravityRoutine());

                yield return StartCoroutine(RefillBoardRoutine());
                yield return new WaitForSeconds(resolveStepDelay);
            }

            Debug.LogWarning($"BoardManager: Resolve loop stopped by max limit ({maxResolveLoops}).");
            HideComboFeedback();
            ValidateBoardData();
        }

        private IEnumerator ColorClearSkillRoutine(TileType targetType)
        {
            isResolving = true;
            ClearSelection();
            ResetComboFeedback();

            List<Tile> tilesToClear = GetActiveTiles()
                .Where(tile => tile != null && tile.Type == targetType)
                .ToList();

            ClearStepResult clearResult = new ClearStepResult();
            yield return StartCoroutine(ClearTilesAndGetResultRoutine(tilesToClear, result => clearResult = result));
            lastResolveClearedTileCount = clearResult.ClearedCount;

            if (clearResult.ClearedCount > 0)
            {
                PlayComboSfx(1);
                TryPlayMaxComboImpact(1);
            }

            gameManager?.OnTilesCleared(clearResult.ClearedCount);
            gameManager?.OnTileColorsCleared(clearResult.ColorCounts, 1);

            yield return new WaitForSeconds(resolveStepDelay);

            yield return StartCoroutine(ApplyGravityRoutine());

            yield return StartCoroutine(RefillBoardRoutine());
            yield return new WaitForSeconds(resolveStepDelay);

            yield return StartCoroutine(ResolveBoardRoutine(true, false, 1));

            HideComboFeedback();
            isResolving = false;
            gameManager?.OnPlayerMoveResolved(lastResolveClearedTileCount);
        }

        private void TrySwapSelectedTile(Tile targetTile)
        {
            if (selectedTile == null || targetTile == null)
            {
                ClearSelection();
                return;
            }

            StartCoroutine(TrySwapRoutine(selectedTile, targetTile));
        }

        private void SwapTiles(Tile firstTile, Tile secondTile, bool updatePositions = true)
        {
            if (firstTile == null || secondTile == null)
            {
                return;
            }

            int firstRow = firstTile.Row;
            int firstCol = firstTile.Col;
            int secondRow = secondTile.Row;
            int secondCol = secondTile.Col;

            boardTiles[firstRow, firstCol] = secondTile;
            boardTiles[secondRow, secondCol] = firstTile;

            firstTile.SetCoordinate(secondRow, secondCol);
            secondTile.SetCoordinate(firstRow, firstCol);

            if (updatePositions)
            {
                firstTile.transform.localPosition = GetTileLocalPosition(secondRow, secondCol);
                secondTile.transform.localPosition = GetTileLocalPosition(firstRow, firstCol);
            }
        }

        private IEnumerator AnimateTileSwap(Tile firstTile, Tile secondTile)
        {
            if (firstTile == null || secondTile == null)
            {
                yield break;
            }

            Vector3 firstTarget = GetTileLocalPosition(firstTile.Row, firstTile.Col);
            Vector3 secondTarget = GetTileLocalPosition(secondTile.Row, secondTile.Col);

            yield return StartCoroutine(AnimateTilesToPositions(firstTile, firstTarget, secondTile, secondTarget, tileSwapDuration));
        }

        private IEnumerator AnimateTilesToPositions(Tile firstTile, Vector3 firstTarget, Tile secondTile, Vector3 secondTarget, float duration)
        {
            if (firstTile == null || secondTile == null)
            {
                yield break;
            }

            Vector3 firstStart = firstTile.transform.localPosition;
            Vector3 secondStart = secondTile.transform.localPosition;
            float safeDuration = Mathf.Max(0f, duration);

            if (safeDuration <= 0f)
            {
                firstTile.transform.localPosition = firstTarget;
                secondTile.transform.localPosition = secondTarget;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                float easedTime = EvaluateSwapCurve(time);

                firstTile.transform.localPosition = Vector3.LerpUnclamped(firstStart, firstTarget, easedTime);
                secondTile.transform.localPosition = Vector3.LerpUnclamped(secondStart, secondTarget, easedTime);

                yield return null;
            }

            firstTile.transform.localPosition = firstTarget;
            secondTile.transform.localPosition = secondTarget;
        }

        private float EvaluateSwapCurve(float time)
        {
            return EvaluateAnimationCurve(tileSwapCurve, time);
        }

        private void SelectTile(Tile tile)
        {
            ClearSelection();

            selectedTile = tile;
            selectedTile.SetSelected(true);
        }

        private void ClearSelection()
        {
            if (selectedTile != null)
            {
                selectedTile.SetSelected(false);
            }

            selectedTile = null;
        }

        private void ResetComboFeedback()
        {
            currentResolveComboGroupCount = 0;
            hasPlayedMaxComboImpact = false;
            comboTextController?.HideImmediate();
        }

        private void UpdateComboFeedback(int matchGroupCount)
        {
            if (matchGroupCount <= 0)
            {
                return;
            }

            currentResolveComboGroupCount += matchGroupCount;

            if (currentResolveComboGroupCount >= 2)
            {
                comboTextController?.ShowCombo(currentResolveComboGroupCount);
            }
        }

        private void HideComboFeedback()
        {
            comboTextController?.HideCombo();
            currentResolveComboGroupCount = 0;
        }

        private bool AreAdjacent(Tile firstTile, Tile secondTile)
        {
            if (firstTile == null || secondTile == null)
            {
                return false;
            }

            int rowDistance = Mathf.Abs(firstTile.Row - secondTile.Row);
            int colDistance = Mathf.Abs(firstTile.Col - secondTile.Col);

            return rowDistance + colDistance == 1;
        }

        private IEnumerator ProcessMatchGroupsRoutine(List<MatchGroup> matchGroups, Action<ClearStepResult> onComplete)
        {
            HashSet<Tile> tilesToClear = new HashSet<Tile>();
            HashSet<Tile> activatedSpecialTiles = new HashSet<Tile>();
            Dictionary<Tile, TileDestroyContext> destroyContexts = new Dictionary<Tile, TileDestroyContext>();

            foreach (MatchGroup group in matchGroups)
            {
                bool groupHasExistingSpecial = HasExistingSpecialTile(group);

                foreach (Tile tile in group.Tiles)
                {
                    if (tile != null)
                    {
                        tilesToClear.Add(tile);
                    }
                }

                foreach (Tile tile in group.Tiles)
                {
                    ActivateSpecialTileIfNeeded(tile, tilesToClear, activatedSpecialTiles, destroyContexts);
                }

                if (groupHasExistingSpecial || !ShouldCreateSpecialTile(group))
                {
                    continue;
                }

                Tile specialTile = SelectSpecialTileForGroup(group);
                if (specialTile == null)
                {
                    continue;
                }

                SpecialTileType specialType = ClassifySpecialTileType(group);
                SetTileSpecialType(specialTile, specialType, true);
                tilesToClear.Remove(specialTile);
            }

            yield return StartCoroutine(ClearTilesAndGetResultRoutine(new List<Tile>(tilesToClear), onComplete, destroyContexts));
        }

        private List<Tile> GetTilesFromMatchGroups(List<MatchGroup> matchGroups)
        {
            HashSet<Tile> uniqueTiles = new HashSet<Tile>();

            foreach (MatchGroup group in matchGroups)
            {
                uniqueTiles.UnionWith(group.Tiles);
            }

            return new List<Tile>(uniqueTiles);
        }

        private bool HasExistingSpecialTile(MatchGroup group)
        {
            foreach (Tile tile in group.Tiles)
            {
                if (tile != null && tile.IsSpecial)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldCreateSpecialTile(MatchGroup group)
        {
            if (group == null)
            {
                return false;
            }

            return group.Count >= 4 || group.IsCornerOrCrossShape;
        }

        private SpecialTileType ClassifySpecialTileType(MatchGroup group)
        {
            if (group.IsCornerOrCrossShape || group.Count >= 5)
            {
                return SpecialTileType.Bomb;
            }

            if (group.LongestHorizontalLength >= 4)
            {
                return SpecialTileType.LineHorizontal;
            }

            if (group.LongestVerticalLength >= 4)
            {
                return SpecialTileType.LineVertical;
            }

            return SpecialTileType.None;
        }

        private Tile SelectSpecialTileForGroup(MatchGroup group)
        {
            Tile swappedTile = GetPreferredSwappedTile(group);
            if (swappedTile != null)
            {
                return swappedTile;
            }

            return GetClosestTileToGroupCenter(group);
        }

        private Tile GetPreferredSwappedTile(MatchGroup group)
        {
            if (recentSwapSecondTile != null && group.Tiles.Contains(recentSwapSecondTile) && !recentSwapSecondTile.IsSpecial)
            {
                return recentSwapSecondTile;
            }

            if (recentSwapFirstTile != null && group.Tiles.Contains(recentSwapFirstTile) && !recentSwapFirstTile.IsSpecial)
            {
                return recentSwapFirstTile;
            }

            return null;
        }

        private Tile GetClosestTileToGroupCenter(MatchGroup group)
        {
            Tile closestTile = null;
            float closestDistance = float.MaxValue;
            float centerRow = 0f;
            float centerCol = 0f;
            int tileCount = 0;

            foreach (Tile tile in group.Tiles)
            {
                if (tile == null || tile.IsSpecial)
                {
                    continue;
                }

                centerRow += tile.Row;
                centerCol += tile.Col;
                tileCount++;
            }

            if (tileCount == 0)
            {
                return null;
            }

            centerRow /= tileCount;
            centerCol /= tileCount;

            foreach (Tile tile in group.Tiles)
            {
                if (tile == null || tile.IsSpecial)
                {
                    continue;
                }

                float rowDistance = tile.Row - centerRow;
                float colDistance = tile.Col - centerCol;
                float distance = rowDistance * rowDistance + colDistance * colDistance;

                if (distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                closestTile = tile;
            }

            return closestTile;
        }

        private void ActivateSpecialTileIfNeeded(
            Tile tile,
            HashSet<Tile> tilesToClear,
            HashSet<Tile> activatedSpecialTiles,
            Dictionary<Tile, TileDestroyContext> destroyContexts)
        {
            if (tile == null || !tile.IsSpecial || activatedSpecialTiles.Contains(tile))
            {
                return;
            }

            activatedSpecialTiles.Add(tile);
            TileDestroyContext sourceContext = new TileDestroyContext(tile.SpecialType, tile.Type, tile.Row, tile.Col);
            SetDestroyContext(tile, sourceContext, destroyContexts);

            switch (tile.SpecialType)
            {
                case SpecialTileType.LineHorizontal:
                    AddRowToClear(tile.Row, tilesToClear, activatedSpecialTiles, destroyContexts, sourceContext);
                    break;

                case SpecialTileType.LineVertical:
                    AddColumnToClear(tile.Col, tilesToClear, activatedSpecialTiles, destroyContexts, sourceContext);
                    break;

                case SpecialTileType.Bomb:
                    AddAreaToClear(tile.Row, tile.Col, 1, tilesToClear, activatedSpecialTiles, destroyContexts, sourceContext);
                    break;
            }
        }

        private void AddRowToClear(
            int row,
            HashSet<Tile> tilesToClear,
            HashSet<Tile> activatedSpecialTiles,
            Dictionary<Tile, TileDestroyContext> destroyContexts,
            TileDestroyContext sourceContext)
        {
            for (int col = 0; col < cols; col++)
            {
                AddTileToClear(row, col, tilesToClear, activatedSpecialTiles, destroyContexts, sourceContext);
            }
        }

        private void AddColumnToClear(
            int col,
            HashSet<Tile> tilesToClear,
            HashSet<Tile> activatedSpecialTiles,
            Dictionary<Tile, TileDestroyContext> destroyContexts,
            TileDestroyContext sourceContext)
        {
            for (int row = 0; row < rows; row++)
            {
                AddTileToClear(row, col, tilesToClear, activatedSpecialTiles, destroyContexts, sourceContext);
            }
        }

        private void AddAreaToClear(
            int centerRow,
            int centerCol,
            int radius,
            HashSet<Tile> tilesToClear,
            HashSet<Tile> activatedSpecialTiles,
            Dictionary<Tile, TileDestroyContext> destroyContexts,
            TileDestroyContext sourceContext)
        {
            for (int row = centerRow - radius; row <= centerRow + radius; row++)
            {
                for (int col = centerCol - radius; col <= centerCol + radius; col++)
                {
                    AddTileToClear(row, col, tilesToClear, activatedSpecialTiles, destroyContexts, sourceContext);
                }
            }
        }

        private void AddTileToClear(
            int row,
            int col,
            HashSet<Tile> tilesToClear,
            HashSet<Tile> activatedSpecialTiles,
            Dictionary<Tile, TileDestroyContext> destroyContexts,
            TileDestroyContext sourceContext)
        {
            Tile tile = GetTile(row, col);
            if (tile == null)
            {
                return;
            }

            tilesToClear.Add(tile);
            SetDestroyContext(tile, sourceContext, destroyContexts);
            ActivateSpecialTileIfNeeded(tile, tilesToClear, activatedSpecialTiles, destroyContexts);
        }

        private void SetDestroyContext(
            Tile tile,
            TileDestroyContext sourceContext,
            Dictionary<Tile, TileDestroyContext> destroyContexts)
        {
            if (tile == null || destroyContexts == null || destroyContexts.ContainsKey(tile))
            {
                return;
            }

            destroyContexts[tile] = sourceContext;
        }

        private IEnumerator ClearTilesAndGetResultRoutine(
            List<Tile> tilesToClear,
            Action<ClearStepResult> onComplete,
            Dictionary<Tile, TileDestroyContext> destroyContexts = null)
        {
            if (tilesToClear == null || tilesToClear.Count == 0)
            {
                onComplete?.Invoke(new ClearStepResult());
                yield break;
            }

            ClearStepResult result = new ClearStepResult();
            List<Tile> validTilesToRelease = new List<Tile>();

            foreach (Tile tile in tilesToClear)
            {
                if (tile == null)
                {
                    continue;
                }

                int row = tile.Row;
                int col = tile.Col;

                if (!IsValidCoordinate(row, col))
                {
                    continue;
                }

                if (boardTiles[row, col] != tile)
                {
                    continue;
                }

                boardTiles[row, col] = null;
                result.Add(tile.Type);
                validTilesToRelease.Add(tile);
            }

            onComplete?.Invoke(result);
            yield return StartCoroutine(PlayClearVisualsRoutine(validTilesToRelease, destroyContexts));

            foreach (Tile tile in validTilesToRelease)
            {
                if (tile != null)
                {
                    ReleaseTileToPool(tile);
                }
            }
        }

        private IEnumerator PlayClearVisualsRoutine(List<Tile> tilesToClear, Dictionary<Tile, TileDestroyContext> destroyContexts)
        {
            if (tilesToClear == null || tilesToClear.Count == 0)
            {
                yield break;
            }

            List<TileClearVisual> clearVisuals = new List<TileClearVisual>();
            List<Coroutine> runningAnimations = new List<Coroutine>();

            foreach (Tile tile in tilesToClear)
            {
                if (tile != null)
                {
                    TileDestroyContext destroyContext = GetDestroyContext(tile, destroyContexts);
                    clearVisuals.Add(new TileClearVisual(tile, destroyContext, GetSpecialClearWaveDelay(tile, destroyContext)));
                }
            }

            clearVisuals.Sort((first, second) => first.Delay.CompareTo(second.Delay));

            float elapsedDelay = 0f;
            int index = 0;

            while (index < clearVisuals.Count)
            {
                float nextDelay = clearVisuals[index].Delay;
                float waitTime = Mathf.Max(0f, nextDelay - elapsedDelay);

                if (waitTime > 0f)
                {
                    yield return new WaitForSeconds(waitTime);
                    elapsedDelay = nextDelay;
                }

                while (index < clearVisuals.Count && Mathf.Approximately(clearVisuals[index].Delay, nextDelay))
                {
                    TileClearVisual clearVisual = clearVisuals[index];
                    if (clearVisual.Tile != null)
                    {
                        SpawnTileDestroyEffect(clearVisual.Tile, clearVisual.DestroyContext);
                        runningAnimations.Add(StartCoroutine(clearVisual.Tile.PlayDestroyVisual()));
                    }

                    index++;
                }
            }

            foreach (Coroutine animation in runningAnimations)
            {
                yield return animation;
            }
        }

        private void ClearActiveBoardTiles()
        {
            if (boardTiles == null)
            {
                return;
            }

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Tile tile = boardTiles[row, col];

                    if (tile == null)
                    {
                        continue;
                    }

                    boardTiles[row, col] = null;
                    ReleaseTileToPool(tile);
                }
            }
        }

        private void PlayComboSfx(int comboLevel)
        {
            if (comboLevel <= 0)
            {
                return;
            }

            GetSceneAudioLibrary()?.PlayComboLevel(comboLevel);
        }

        private void TryPlayMaxComboImpact(int comboLevel)
        {
            if (hasPlayedMaxComboImpact || comboLevel < maxComboImpactLevel)
            {
                return;
            }

            hasPlayedMaxComboImpact = true;
            tinyImpulse?.Shake(maxComboShakeForce);
        }

        private TileDestroyContext GetDestroyContext(Tile tile, Dictionary<Tile, TileDestroyContext> destroyContexts)
        {
            if (tile != null && destroyContexts != null && destroyContexts.TryGetValue(tile, out TileDestroyContext destroyContext))
            {
                return destroyContext;
            }

            return new TileDestroyContext(
                tile != null ? tile.SpecialType : SpecialTileType.None,
                tile != null ? tile.Type : TileType.Red,
                tile != null ? tile.Row : 0,
                tile != null ? tile.Col : 0);
        }

        private float GetSpecialClearWaveDelay(Tile tile, TileDestroyContext destroyContext)
        {
            if (tile == null)
            {
                return 0f;
            }

            switch (destroyContext.SpecialType)
            {
                case SpecialTileType.Bomb:
                    int rowDistance = Mathf.Abs(tile.Row - destroyContext.SourceRow);
                    int colDistance = Mathf.Abs(tile.Col - destroyContext.SourceCol);
                    return Mathf.Max(rowDistance, colDistance) * GetSpecialWaveDelay(bombWaveStepDelay);

                case SpecialTileType.LineHorizontal:
                    return Mathf.Abs(tile.Col - destroyContext.SourceCol) * GetSpecialWaveDelay(lineWaveStepDelay);

                case SpecialTileType.LineVertical:
                    return Mathf.Abs(tile.Row - destroyContext.SourceRow) * GetSpecialWaveDelay(lineWaveStepDelay);

                default:
                    return 0f;
            }
        }

        private float GetSpecialWaveDelay(float specificDelay)
        {
            return specificDelay > 0f ? specificDelay : specialWaveStepDelay;
        }

        private void SpawnTileDestroyEffect(Tile tile, TileDestroyContext destroyContext)
        {
            if (tile == null)
            {
                return;
            }

            GameObject effectPrefab = GetTileDestroyEffectPrefab(destroyContext.SpecialType);
            if (effectPrefab == null)
            {
                return;
            }

            GameObject effectInstance = Instantiate(effectPrefab, tile.transform.position, Quaternion.identity, tileDestroyEffectParent);
            TintTileDestroyEffect(effectInstance, GetTileEffectColor(destroyContext.TileType));

            if (tileDestroyEffectLifetime > 0f)
            {
                Destroy(effectInstance, tileDestroyEffectLifetime);
            }
        }

        private GameObject GetTileDestroyEffectPrefab(SpecialTileType specialType)
        {
            switch (specialType)
            {
                case SpecialTileType.Bomb:
                    return bombTileDestroyEffectPrefab != null ? bombTileDestroyEffectPrefab : tileDestroyEffectPrefab;

                case SpecialTileType.LineHorizontal:
                    return horizontalTileDestroyEffectPrefab != null ? horizontalTileDestroyEffectPrefab : tileDestroyEffectPrefab;

                case SpecialTileType.LineVertical:
                    return verticalTileDestroyEffectPrefab != null ? verticalTileDestroyEffectPrefab : tileDestroyEffectPrefab;

                default:
                    return tileDestroyEffectPrefab;
            }
        }

        private void TintTileDestroyEffect(GameObject effectInstance, Color tintColor)
        {
            if (effectInstance == null)
            {
                return;
            }

            VfxMaterialTintApplier[] materialTintAppliers = effectInstance.GetComponentsInChildren<VfxMaterialTintApplier>(true);

            if (materialTintAppliers.Length > 0)
            {
                foreach (VfxMaterialTintApplier materialTintApplier in materialTintAppliers)
                {
                    if (materialTintApplier != null)
                    {
                        materialTintApplier.ApplyTint(tintColor);
                    }
                }

                return;
            }

            SpriteRenderer[] spriteRenderers = effectInstance.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color color = spriteRenderer.color;
                color.r *= tintColor.r;
                color.g *= tintColor.g;
                color.b *= tintColor.b;
                spriteRenderer.color = color;
            }
        }

        private Color GetTileEffectColor(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Red:
                    return Color.red;

                case TileType.Green:
                    return Color.green;

                case TileType.Blue:
                    return Color.blue;

                case TileType.Yellow:
                    return Color.yellow;

                case TileType.Purple:
                    return Color.magenta;

                default:
                    return Color.white;
            }
        }

        private void PlayFailedSwapSfx()
        {
            GetSceneAudioLibrary()?.PlayFailedSwap();
        }

        private void PlaySpecialTileSpawnSfx()
        {
            GetSceneAudioLibrary()?.PlaySpecialTileSpawn();
        }

        private bool SetTileSpecialType(Tile tile, SpecialTileType specialType, bool playSpawnSfx)
        {
            if (tile == null)
            {
                return false;
            }

            bool createsSpecial = !tile.IsSpecial && specialType != SpecialTileType.None;
            tile.SetSpecialType(specialType);

            if (createsSpecial && playSpawnSfx)
            {
                PlaySpecialTileSpawnSfx();
            }

            return createsSpecial;
        }

        private SceneAudioLibrary GetSceneAudioLibrary()
        {
            return sceneAudioLibrary != null ? sceneAudioLibrary : SceneAudioLibrary.Current;
        }

        private IEnumerator ApplyGravityRoutine()
        {
            List<TileMoveAnimation> moves = new List<TileMoveAnimation>();

            for (int col = 0; col < cols; col++)
            {
                int emptyRow = -1;

                for (int row = rows - 1; row >= 0; row--)
                {
                    Tile tile = boardTiles[row, col];

                    if (tile == null)
                    {
                        if (emptyRow == -1)
                        {
                            emptyRow = row;
                        }

                        continue;
                    }

                    if (emptyRow == -1)
                    {
                        continue;
                    }

                    boardTiles[emptyRow, col] = tile;
                    boardTiles[row, col] = null;

                    tile.SetCoordinate(emptyRow, col);
                    Vector3 targetPosition = GetTileLocalPosition(emptyRow, col);
                    Vector3 startPosition = tile.transform.localPosition;
                    moves.Add(new TileMoveAnimation(tile, startPosition, targetPosition, CalculateTileFallDuration(startPosition, targetPosition)));

                    emptyRow--;
                }
            }

            yield return StartCoroutine(AnimateTileMoves(moves, tileFallCurve));
        }

        private IEnumerator RefillBoardRoutine()
        {
            List<TileMoveAnimation> moves = new List<TileMoveAnimation>();

            for (int col = 0; col < cols; col++)
            {
                int refillSpawnIndex = 0;

                for (int row = rows - 1; row >= 0; row--)
                {
                    if (boardTiles[row, col] != null)
                    {
                        continue;
                    }

                    TileType randomType = GetRandomTileType();
                    Vector3 targetPosition = GetTileLocalPosition(row, col);
                    Vector3 spawnPosition = GetRefillSpawnPosition(col, refillSpawnIndex);
                    Tile tile = GetTileFromPool(row, col, randomType, spawnPosition);
                    boardTiles[row, col] = tile;
                    moves.Add(new TileMoveAnimation(tile, spawnPosition, targetPosition, CalculateTileFallDuration(spawnPosition, targetPosition)));
                    refillSpawnIndex++;
                }
            }

            yield return StartCoroutine(AnimateTileMoves(moves, tileFallCurve));
        }

        private IEnumerator PlayBoardIntroAnimationRoutine()
        {
            if (!ShouldPlayBoardIntroAnimation())
            {
                yield break;
            }

            List<Tile> activeTiles = GetActiveTiles();
            if (activeTiles.Count == 0)
            {
                yield break;
            }

            bool wasInputBlocked = isInputBlocked;
            isInputBlocked = true;

            List<Coroutine> runningAnimations = new List<Coroutine>();

            for (int i = 0; i < activeTiles.Count; i++)
            {
                Tile tile = activeTiles[i];
                if (tile == null)
                {
                    continue;
                }

                float delay = boardIntroStaggerDelay * i;
                runningAnimations.Add(StartCoroutine(tile.PlayIntroVisual(
                    boardIntroDuration,
                    boardIntroStartScale,
                    boardIntroOvershootScale,
                    boardIntroEndScale,
                    delay,
                    boardIntroUseUnscaledTime)));
            }

            foreach (Coroutine animation in runningAnimations)
            {
                yield return animation;
            }

            isInputBlocked = wasInputBlocked;
        }

        private Tile GetTileFromPool(int row, int col, TileType type)
        {
            return GetTileFromPool(row, col, type, GetTileLocalPosition(row, col));
        }

        private Tile GetTileFromPool(int row, int col, TileType type, Vector3 startLocalPosition)
        {
            Tile tile;

            if (tilePool.Count > 0)
            {
                tile = tilePool.Pop();
                tile.transform.SetParent(GetTileParent(), false);
                tile.transform.localPosition = startLocalPosition;
                tile.gameObject.SetActive(true);
            }
            else
            {
                tile = Instantiate(tilePrefab, GetTileParent());
                tile.transform.localPosition = startLocalPosition;
                tile.transform.localRotation = Quaternion.identity;
                tile.SetBoardManager(this);
            }

            tile.Init(row, col, type);

            if (spawnBoardTilesAtIntroScale)
            {
                tile.SetVisualScaleMultiplier(boardIntroStartScale);
            }

            return tile;
        }

        private bool ShouldPlayBoardIntroAnimation()
        {
            return enableBoardIntroAnimation && boardIntroDuration > 0f;
        }

        private void ReleaseTileToPool(Tile tile)
        {
            tile.SetSelected(false);
            tile.gameObject.SetActive(false);
            tilePool.Push(tile);
        }

        private TileType GetRandomTileType()
        {
            int typeCount = System.Enum.GetValues(typeof(TileType)).Length;
            int randomIndex = UnityEngine.Random.Range(0, typeCount);

            return (TileType)randomIndex;
        }

        private IEnumerator AnimateTileMoves(List<TileMoveAnimation> moves, AnimationCurve curve)
        {
            if (moves == null || moves.Count == 0)
            {
                yield break;
            }

            float maxDuration = GetMaxTileMoveDuration(moves);

            if (maxDuration <= 0f)
            {
                SnapTileMoves(moves);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < moves.Count; i++)
                {
                    TileMoveAnimation move = moves[i];
                    if (move.Tile != null)
                    {
                        float moveDuration = Mathf.Max(0f, move.Duration);
                        float time = moveDuration > 0f ? Mathf.Clamp01(elapsed / moveDuration) : 1f;
                        float easedTime = EvaluateAnimationCurve(curve, time);
                        move.Tile.transform.localPosition = Vector3.LerpUnclamped(move.StartPosition, move.TargetPosition, easedTime);
                    }
                }

                yield return null;
            }

            SnapTileMoves(moves);
        }

        private float GetMaxTileMoveDuration(List<TileMoveAnimation> moves)
        {
            float maxDuration = 0f;

            for (int i = 0; i < moves.Count; i++)
            {
                maxDuration = Mathf.Max(maxDuration, moves[i].Duration);
            }

            return maxDuration;
        }

        private void SnapTileMoves(List<TileMoveAnimation> moves)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                TileMoveAnimation move = moves[i];
                if (move.Tile != null)
                {
                    move.Tile.transform.localPosition = move.TargetPosition;
                }
            }
        }

        private Vector3 GetTileLocalPosition(int row, int col)
        {
            Vector2 boardOffset = GetBoardCenterOffset();

            float x = col * tileSpacing + boardOffset.x;
            float y = -row * tileSpacing + boardOffset.y;

            return new Vector3(x, y, 0f);
        }

        private Vector3 GetRefillSpawnPosition(int col, int spawnIndex)
        {
            Vector3 topCellPosition = GetTileLocalPosition(0, col);
            float rowOffset = tileRefillSpawnOffset + spawnIndex + 1f;
            return new Vector3(topCellPosition.x, topCellPosition.y + rowOffset * tileSpacing, topCellPosition.z);
        }

        private float CalculateTileFallDuration(Vector3 startPosition, Vector3 targetPosition)
        {
            if (tileFallSpeed <= 0f)
            {
                return Mathf.Max(0f, tileFallDuration);
            }

            float distance = Vector3.Distance(startPosition, targetPosition);
            float duration = distance / tileFallSpeed;
            float minDuration = Mathf.Max(0f, minTileFallDuration);
            float maxDuration = Mathf.Max(minDuration, maxTileFallDuration);

            return Mathf.Clamp(duration, minDuration, maxDuration);
        }

        private float EvaluateAnimationCurve(AnimationCurve curve, float time)
        {
            if (curve == null || curve.length == 0)
            {
                return time;
            }

            return curve.Evaluate(time);
        }

        private Transform GetTileParent()
        {
            return boardRoot != null ? boardRoot : transform;
        }

        private void DrawBoardGizmo()
        {
            Transform origin = GetTileParent();
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;

            Gizmos.matrix = origin.localToWorldMatrix;
            Gizmos.color = Color.yellow;

            Vector3 size = new Vector3(cols * tileSpacing, rows * tileSpacing, 0f);
            Gizmos.DrawWireCube(Vector3.zero, size);

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private Vector2 GetBoardCenterOffset()
        {
            float boardWidth = (cols - 1) * tileSpacing;
            float boardHeight = (rows - 1) * tileSpacing;

            return new Vector2(-boardWidth / 2f, boardHeight / 2f);
        }

        private List<Tile> GetActiveTiles()
        {
            List<Tile> tiles = new List<Tile>();

            if (boardTiles == null)
            {
                return tiles;
            }

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Tile tile = boardTiles[row, col];

                    if (tile != null)
                    {
                        tiles.Add(tile);
                    }
                }
            }

            return tiles;
        }

        private struct ClearStepResult
        {
            public int ClearedCount { get; private set; }
            public Dictionary<TileType, int> ColorCounts { get; private set; }

            public void Add(TileType tileType)
            {
                if (ColorCounts == null)
                {
                    ColorCounts = new Dictionary<TileType, int>();
                }

                ClearedCount++;

                if (!ColorCounts.ContainsKey(tileType))
                {
                    ColorCounts[tileType] = 0;
                }

                ColorCounts[tileType]++;
            }
        }

        private readonly struct TileDestroyContext
        {
            public TileDestroyContext(SpecialTileType specialType, TileType tileType, int sourceRow, int sourceCol)
            {
                SpecialType = specialType;
                TileType = tileType;
                SourceRow = sourceRow;
                SourceCol = sourceCol;
            }

            public SpecialTileType SpecialType { get; }
            public TileType TileType { get; }
            public int SourceRow { get; }
            public int SourceCol { get; }
        }

        #endregion
    }
}
