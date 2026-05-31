using System.Collections.Generic;

namespace CrystalMind.MatchMancer
{
    public readonly struct BoardResolveStepResult
    {
        #region Variables

        // State
        private readonly IReadOnlyDictionary<TileType, int> destroyedTileTypeCounts;

        #endregion

        #region Properties

        public BoardResolveStepContext Context { get; }
        public int ClearedCount { get; }
        public IReadOnlyDictionary<TileType, int> DestroyedTileTypeCounts => destroyedTileTypeCounts;

        #endregion

        #region Public Methods

        public BoardResolveStepResult(BoardResolveStepContext context, int clearedCount, IReadOnlyDictionary<TileType, int> destroyedTileTypeCounts)
        {
            Context = context;
            ClearedCount = clearedCount;
            this.destroyedTileTypeCounts = destroyedTileTypeCounts ?? new Dictionary<TileType, int>();
        }

        #endregion
    }
}
