using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class BoardManager : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private int rows = 8;
        [SerializeField] private int cols = 8;
        [SerializeField] private float tileSpacing = 1.1f;

        [Header("References")]
        [SerializeField] private Tile tilePrefab;

        private Tile[,] boardTiles;
        public int Rows { get { return rows; } }
        public int Cols { get { return cols; } }

        private void Start()
        {
            GenerateBoard();

            MatchFinder finder = new MatchFinder(this);
            var matches = finder.FindAllMatches();

            Debug.Log($"Matches found: {matches.Count}");
        }

        private void GenerateBoard()
        {
            if (tilePrefab == null)
            {
                Debug.LogError("BoardManager: Tile Prefab is missing.");
                return;
            }

            boardTiles = new Tile[rows, cols];

            Vector2 boardOffset = GetBoardCenterOffset();

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    SpawnTile(row, col, boardOffset);
                }
            }

            PrintBoard();
            ValidateBoardData();    
        }

        private void SpawnTile(int row, int col, Vector2 boardOffset)
        {
            Vector3 spawnPosition = new Vector3(
                col * tileSpacing + boardOffset.x,
                -row * tileSpacing + boardOffset.y,
                0f
            );

            TileType randomType = GetRandomTileType();

            Tile tile = Instantiate(tilePrefab, spawnPosition, Quaternion.identity, transform);
            tile.Init(row, col, randomType);

            boardTiles[row, col] = tile;
        }

        private Vector2 GetBoardCenterOffset()
        {
            float boardWidth = (cols - 1) * tileSpacing;
            float boardHeight = (rows - 1) * tileSpacing;

            return new Vector2(
                -boardWidth / 2f,
                boardHeight / 2f
            );
        }

        private TileType GetRandomTileType()
        {
            int typeCount = System.Enum.GetValues(typeof(TileType)).Length;
            int randomIndex = Random.Range(0, typeCount);

            return (TileType)randomIndex;
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

        public void PrintBoard()
        {
            Debug.Log("=== Board State ===");

            for (int row = 0; row < rows; row++)
            {
                string line = "";

                for (int col = 0; col < cols; col++)
                {
                    Tile tile = GetTile(row, col);
                    line += tile != null ? tile.Type.ToString()[0] + " " : "? ";
                }

                Debug.Log(line);
            }
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
                        Debug.LogError($"Board validation failed: Tile coordinate mismatch at [{row}, {col}]");
                        return false;
                    }
                }
            }

            Debug.Log("Board validation passed.");
            return true;
        }
    }
}