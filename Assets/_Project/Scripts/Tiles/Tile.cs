using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class Tile : MonoBehaviour
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public TileType Type { get; private set; }

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(int row, int col, TileType type)
        {
            Row = row;
            Col = col;
            Type = type;

            gameObject.name = $"Tile_{row}_{col}_{type}";

            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (spriteRenderer == null)
            {
                Debug.LogError($"{name}: SpriteRenderer missing.");
                return;
            }

            spriteRenderer.color = GetColorByType(Type);
        }

        private Color GetColorByType(TileType type)
        {
            switch (type)
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
    }
}