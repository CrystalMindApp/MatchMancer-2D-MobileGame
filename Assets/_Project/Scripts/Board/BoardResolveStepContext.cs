using System.Collections.Generic;

namespace CrystalMind.MatchMancer
{
    public readonly struct BoardResolveStepContext
    {
        #region Variables

        // State
        private readonly IReadOnlyCollection<TileType> matchedTileTypes;

        #endregion

        #region Properties

        public int ComboIndex { get; }
        public IReadOnlyList<MatchGroup> MatchGroups { get; }
        public IReadOnlyCollection<TileType> MatchedTileTypes => matchedTileTypes;
        public int MatchedTileTypeCount => matchedTileTypes != null ? matchedTileTypes.Count : 0;
        public bool HasMultipleMatchedTileTypes => MatchedTileTypeCount >= 2;

        #endregion

        #region Public Methods

        public BoardResolveStepContext(int comboIndex, IReadOnlyList<MatchGroup> matchGroups, IReadOnlyCollection<TileType> matchedTileTypes)
        {
            ComboIndex = comboIndex;
            MatchGroups = matchGroups;
            this.matchedTileTypes = matchedTileTypes ?? new List<TileType>();
        }

        #endregion
    }
}
