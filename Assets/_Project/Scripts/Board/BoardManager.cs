using System.Collections;
using System.Collections.Generic;
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

        [Header("References")]
        [SerializeField] private Tile tilePrefab;
        [SerializeField] private Camera inputCamera;

        // Cache
        private Tile[,] boardTiles;
        private Tile selectedTile;
        private MatchFinder matchFinder;
        private readonly Stack<Tile> tilePool = new Stack<Tile>();

        // State
        private bool isResolving;

        #endregion

        #region Properties

        public int Rows => rows;
        public int Cols => cols;
        public bool IsResolving => isResolving;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            matchFinder = new MatchFinder(this);
        }

        private void Start()
        {
            GenerateBoard();
            ValidateBoardData();

            StartCoroutine(InitialResolveRoutine());
        }

        private void Update()
        {
            HandleInput();
        }

        #endregion

        #region Public Methods

        public void HandleTileClicked(Tile clickedTile)
        {
            if (isResolving)
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

        #endregion

        #region Protected Methods

        #endregion

        #region Private Methods

        private void HandleInput()
        {
            if (isResolving)
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
            if (tilePrefab == null)
            {
                Debug.LogError("BoardManager: Tile prefab is missing.");
                return;
            }

            boardTiles = new Tile[rows, cols];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    TileType randomType = GetRandomTileType();
                    Tile tile = GetTileFromPool(row, col, randomType);
                    boardTiles[row, col] = tile;
                }
            }
        }

        private IEnumerator InitialResolveRoutine()
        {
            isResolving = true;
            yield return StartCoroutine(ResolveBoardRoutine());
            isResolving = false;
        }

        private IEnumerator TrySwapRoutine(Tile firstTile, Tile secondTile)
        {
            isResolving = true;
            ClearSelection();

            SwapTiles(firstTile, secondTile);
            yield return new WaitForSeconds(resolveStepDelay);

            List<Tile> matches = matchFinder.FindAllMatches();
            if (matches.Count == 0)
            {
                SwapTiles(firstTile, secondTile);
                yield return new WaitForSeconds(resolveStepDelay);

                isResolving = false;
                yield break;
            }

            yield return StartCoroutine(ResolveBoardRoutine());
            isResolving = false;
        }

        private IEnumerator ResolveBoardRoutine()
        {
            for (int loopCount = 0; loopCount < maxResolveLoops; loopCount++)
            {
                List<Tile> matches = matchFinder.FindAllMatches();

                if (matches.Count == 0)
                {
                    ValidateBoardData();
                    yield break;
                }

                ClearTiles(matches);
                yield return new WaitForSeconds(resolveStepDelay);

                ApplyGravity();
                yield return new WaitForSeconds(resolveStepDelay);

                RefillBoard();
                yield return new WaitForSeconds(resolveStepDelay);
            }

            Debug.LogWarning($"BoardManager: Resolve loop stopped by max limit ({maxResolveLoops}).");
            ValidateBoardData();
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

        private void SwapTiles(Tile firstTile, Tile secondTile)
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

            firstTile.transform.position = GetTileWorldPosition(secondRow, secondCol);
            secondTile.transform.position = GetTileWorldPosition(firstRow, firstCol);
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

        private void ClearTiles(List<Tile> tilesToClear)
        {
            if (tilesToClear == null || tilesToClear.Count == 0)
            {
                return;
            }

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
                ReleaseTileToPool(tile);
            }
        }

        private void ApplyGravity()
        {
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
                    tile.transform.position = GetTileWorldPosition(emptyRow, col);

                    emptyRow--;
                }
            }
        }

        private void RefillBoard()
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (boardTiles[row, col] != null)
                    {
                        continue;
                    }

                    TileType randomType = GetRandomTileType();
                    Tile tile = GetTileFromPool(row, col, randomType);
                    boardTiles[row, col] = tile;
                }
            }
        }

        private Tile GetTileFromPool(int row, int col, TileType type)
        {
            Tile tile;

            if (tilePool.Count > 0)
            {
                tile = tilePool.Pop();
                tile.transform.SetParent(transform);
                tile.transform.position = GetTileWorldPosition(row, col);
                tile.gameObject.SetActive(true);
            }
            else
            {
                tile = Instantiate(tilePrefab, GetTileWorldPosition(row, col), Quaternion.identity, transform);
                tile.SetBoardManager(this);
            }

            tile.Init(row, col, type);
            return tile;
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
            int randomIndex = Random.Range(0, typeCount);

            return (TileType)randomIndex;
        }

        private Vector3 GetTileWorldPosition(int row, int col)
        {
            Vector2 boardOffset = GetBoardCenterOffset();

            float x = col * tileSpacing + boardOffset.x;
            float y = -row * tileSpacing + boardOffset.y;

            return new Vector3(x, y, 0f);
        }

        private Vector2 GetBoardCenterOffset()
        {
            float boardWidth = (cols - 1) * tileSpacing;
            float boardHeight = (rows - 1) * tileSpacing;

            return new Vector2(-boardWidth / 2f, boardHeight / 2f);
        }

        #endregion
    }
}
