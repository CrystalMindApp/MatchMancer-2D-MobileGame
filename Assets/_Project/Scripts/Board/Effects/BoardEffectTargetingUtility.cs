using System.Collections.Generic;

namespace CrystalMind.MatchMancer
{
    public static class BoardEffectTargetingUtility
    {
        #region Public Methods

        public static List<Tile> GetTilesOfType(BoardManager board, TileType tileType)
        {
            return board != null ? board.GetTilesOfType(tileType) : new List<Tile>();
        }

        public static List<Tile> GetTilesOfTypes(BoardManager board, TileType primaryTileType, TileType secondaryTileType)
        {
            if (board == null)
            {
                return new List<Tile>();
            }

            HashSet<Tile> uniqueTiles = new HashSet<Tile>();
            uniqueTiles.UnionWith(board.GetTilesOfType(primaryTileType));
            uniqueTiles.UnionWith(board.GetTilesOfType(secondaryTileType));
            return new List<Tile>(uniqueTiles);
        }

        public static List<Tile> GetRowTiles(BoardManager board, int row)
        {
            return board != null ? board.GetRowTiles(row) : new List<Tile>();
        }

        public static List<Tile> GetColumnTiles(BoardManager board, int col)
        {
            return board != null ? board.GetColumnTiles(col) : new List<Tile>();
        }

        public static List<Tile> GetAreaTiles(BoardManager board, int centerRow, int centerCol, int radius)
        {
            return board != null ? board.GetAreaTiles(centerRow, centerCol, radius) : new List<Tile>();
        }

        #endregion
    }
}
