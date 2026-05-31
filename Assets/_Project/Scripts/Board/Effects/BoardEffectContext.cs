using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public readonly struct BoardEffectContext
    {
        #region Variables

        // State
        private readonly TileType targetTileType;

        #endregion

        #region Properties

        public BoardManager Board { get; }
        public GameManager GameManager { get; }
        public Tile SourceTile { get; }
        public TileType SourceTileType { get; }
        public SpecialTileType SourceSpecialType { get; }
        public int SourceRow { get; }
        public int SourceCol { get; }
        public bool HasTargetTileType { get; }
        public TileType TargetTileType => HasTargetTileType ? targetTileType : SourceTileType;

        #endregion

        #region Public Methods

        public BoardEffectContext(
            BoardManager board,
            GameManager gameManager = null,
            Tile sourceTile = null,
            TileType? targetTileType = null)
        {
            Board = board;
            GameManager = gameManager;
            SourceTile = sourceTile;
            SourceTileType = sourceTile != null ? sourceTile.Type : targetTileType ?? TileType.Red;
            SourceSpecialType = sourceTile != null ? sourceTile.SpecialType : SpecialTileType.None;
            SourceRow = sourceTile != null ? sourceTile.Row : 0;
            SourceCol = sourceTile != null ? sourceTile.Col : 0;
            HasTargetTileType = targetTileType.HasValue;
            this.targetTileType = targetTileType ?? SourceTileType;
        }

        public BoardEffectContext(
            BoardManager board,
            GameManager gameManager,
            Tile sourceTile,
            TileType sourceTileType,
            SpecialTileType sourceSpecialType,
            int sourceRow,
            int sourceCol,
            TileType? targetTileType = null)
        {
            Board = board;
            GameManager = gameManager;
            SourceTile = sourceTile;
            SourceTileType = sourceTileType;
            SourceSpecialType = sourceSpecialType;
            SourceRow = sourceRow;
            SourceCol = sourceCol;
            HasTargetTileType = targetTileType.HasValue;
            this.targetTileType = targetTileType ?? sourceTileType;
        }

        public BoardEffectContext WithTargetTileType(TileType newTargetTileType)
        {
            return new BoardEffectContext(
                Board,
                GameManager,
                SourceTile,
                SourceTileType,
                SourceSpecialType,
                SourceRow,
                SourceCol,
                newTargetTileType);
        }

        #endregion
    }
}
