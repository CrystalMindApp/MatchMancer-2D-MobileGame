using System.Collections.Generic;

namespace CrystalMind.MatchMancer
{
    public readonly struct BoardEffectResult
    {
        #region Variables

        // State
        private readonly IReadOnlyDictionary<TileType, int> colorCounts;
        private readonly IReadOnlyList<CurseEffectData> triggeredCurses;

        #endregion

        #region Properties

        public bool Succeeded { get; }
        public int AffectedCount { get; }
        public int ClearedCount { get; }
        public IReadOnlyDictionary<TileType, int> ColorCounts => colorCounts;
        public IReadOnlyList<CurseEffectData> TriggeredCurses => triggeredCurses;

        #endregion

        #region Public Methods

        public BoardEffectResult(
            bool succeeded,
            int affectedCount,
            int clearedCount,
            IReadOnlyDictionary<TileType, int> colorCounts = null,
            IReadOnlyList<CurseEffectData> triggeredCurses = null)
        {
            Succeeded = succeeded;
            AffectedCount = affectedCount;
            ClearedCount = clearedCount;
            this.colorCounts = colorCounts ?? new Dictionary<TileType, int>();
            this.triggeredCurses = triggeredCurses ?? new List<CurseEffectData>();
        }

        public static BoardEffectResult Empty()
        {
            return new BoardEffectResult(false, 0, 0);
        }

        public static BoardEffectResult Affected(int affectedCount)
        {
            return new BoardEffectResult(affectedCount > 0, affectedCount, 0);
        }

        public static BoardEffectResult Cleared(
            int clearedCount,
            IReadOnlyDictionary<TileType, int> colorCounts,
            IReadOnlyList<CurseEffectData> triggeredCurses = null)
        {
            return new BoardEffectResult(clearedCount > 0, clearedCount, clearedCount, colorCounts, triggeredCurses);
        }

        #endregion
    }
}
