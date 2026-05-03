using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [RequireComponent(typeof(SpriteRenderer))]
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

            name = $"Tile_{row}_{col}_{type}";
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (spriteRenderer == null) return;

            switch (Type)
            {
                case TileType.Red:
                    spriteRenderer.color = Color.red;
                    break;
                case TileType.Blue:
                    spriteRenderer.color = Color.blue;
                    break;
                case TileType.Green:
                    spriteRenderer.color = Color.green;
                    break;
                case TileType.Yellow:
                    spriteRenderer.color = Color.yellow;
                    break;
                case TileType.Purple:
                    spriteRenderer.color = Color.magenta;
                    break;
            }
        }
    }
}