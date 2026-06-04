using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "SpecialTileVisualSet", menuName = "MatchMancer/Tiles/Special Tile Visual Set")]
    public class SpecialTileVisualSet : ScriptableObject
    {
        #region Variables

        [Header("Special Type")]
        [SerializeField] private SpecialTileType specialTileType = SpecialTileType.Bomb;

        [Header("Color Sprites")]
        [SerializeField] private Sprite redSprite;
        [SerializeField] private Sprite greenSprite;
        [SerializeField] private Sprite blueSprite;
        [SerializeField] private Sprite yellowSprite;
        [SerializeField] private Sprite purpleSprite;

        #endregion

        #region Properties

        public SpecialTileType SpecialTileType => specialTileType;
        public bool IsValid => specialTileType != SpecialTileType.None;

        #endregion

        #region Public Methods

        public Sprite GetSprite(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Red:
                    return redSprite;

                case TileType.Green:
                    return greenSprite;

                case TileType.Blue:
                    return blueSprite;

                case TileType.Yellow:
                    return yellowSprite;

                case TileType.Purple:
                    return purpleSprite;

                default:
                    return null;
            }
        }

        #endregion
    }
}
