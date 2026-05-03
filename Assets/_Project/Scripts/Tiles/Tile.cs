using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class Tile : MonoBehaviour
    {
        #region Variables

        [Header("Selection Settings")]
        [SerializeField, Range(1f, 1.5f)] private float selectedScaleMultiplier = 1.15f;

        // Cache
        private SpriteRenderer spriteRenderer;
        private BoardManager boardManager;
        private Vector3 defaultScale;

        // State
        private int row;
        private int col;
        private TileType type;

        #endregion

        #region Properties

        public int Row => row;
        public int Col => col;
        public TileType Type => type;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            defaultScale = transform.localScale;
        }

        private void Start()
        {
        }

        private void Update()
        {
        }

        #endregion

        #region Public Methods

        public void Init(int newRow, int newCol, TileType newType)
        {
            row = newRow;
            col = newCol;
            type = newType;

            gameObject.name = $"Tile_{row}_{col}_{type}";

            ApplyVisual();
            SetSelected(false);
        }

        public void SetBoardManager(BoardManager manager)
        {
            boardManager = manager;
        }

        public void SetCoordinate(int newRow, int newCol)
        {
            row = newRow;
            col = newCol;
            gameObject.name = $"Tile_{row}_{col}_{type}";
        }

        public void SetSelected(bool isSelected)
        {
            transform.localScale = isSelected ? defaultScale * selectedScaleMultiplier : defaultScale;
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Private Methods

        private void ApplyVisual()
        {
            if (spriteRenderer == null)
            {
                Debug.LogError($"{name}: SpriteRenderer is missing.");
                return;
            }

            spriteRenderer.color = GetColorByType(type);
        }

        private Color GetColorByType(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Red:
                    return Color.red;

                case TileType.Blue:
                    return Color.blue;

                case TileType.Green:
                    return Color.green;

                case TileType.Yellow:
                    return Color.yellow;

                case TileType.Purple:
                    return Color.magenta;

                default:
                    return Color.white;
            }
        }

        #endregion
    }
}
