using System.Collections.Generic;

namespace CrystalMind.MatchMancer
{
    public readonly struct BoardResolveStepResult
    {
        #region Variables

        // State
        private readonly IReadOnlyDictionary<TileType, int> destroyedTileTypeCounts;
        private readonly IReadOnlyList<CurseEffectData> triggeredCurses;

        #endregion

        #region Properties

        public BoardResolveStepContext Context { get; }
        public int ClearedCount { get; }
        public IReadOnlyDictionary<TileType, int> DestroyedTileTypeCounts => destroyedTileTypeCounts;
        public IReadOnlyList<CurseEffectData> TriggeredCurses => triggeredCurses;

        #endregion

        #region Public Methods

        public BoardResolveStepResult(
            BoardResolveStepContext context,
            int clearedCount,
            IReadOnlyDictionary<TileType, int> destroyedTileTypeCounts,
            IReadOnlyList<CurseEffectData> triggeredCurses = null)
        {
            Context = context;
            ClearedCount = clearedCount;
            this.destroyedTileTypeCounts = destroyedTileTypeCounts ?? new Dictionary<TileType, int>();
            this.triggeredCurses = triggeredCurses ?? new List<CurseEffectData>();
        }

        #endregion
    }
}
