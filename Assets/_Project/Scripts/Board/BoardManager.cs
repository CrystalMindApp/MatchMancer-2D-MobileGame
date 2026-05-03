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

        private void Start()
        {
            GenerateBoard();
        }

        private void GenerateBoard()
        {
            boardTiles = new Tile[rows, cols];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    SpawnTile(row, col);
                }
            }

            DebugBoardState();
        }

        private void SpawnTile(int row, int col)
        {
            Vector3 spawnPosition = new Vector3(
                col * tileSpacing,
                -row * tileSpacing,
                0f
            );

            TileType randomType = GetRandomTileType();

            Tile tile = Instantiate(tilePrefab, spawnPosition, Quaternion.identity, transform);
            tile.Init(row, col, randomType);

            boardTiles[row, col] = tile;
        }

        private TileType GetRandomTileType()
        {
            int typeCount = System.Enum.GetValues(typeof(TileType)).Length;
            int randomIndex = Random.Range(0, typeCount);
            return (TileType)randomIndex;
        }

        public Tile GetTile(int row, int col)
        {
            if (row < 0 || row >= rows || col < 0 || col >= cols)
            {
                return null;
            }

            return boardTiles[row, col];
        }

        private void DebugBoardState()
        {
            Debug.Log("Board generated successfully.");

            for (int row = 0; row < rows; row++)
            {
                string line = "";

                for (int col = 0; col < cols; col++)
                {
                    line += boardTiles[row, col].Type.ToString()[0] + " ";
                }

                Debug.Log(line);
            }
        }
    }
}