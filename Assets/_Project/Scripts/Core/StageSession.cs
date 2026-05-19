namespace CrystalMind.MatchMancer
{
    public static class StageSession
    {
        #region Variables

        // State
        private static bool hasPendingStageClearVisual;
        private static int pendingClearedStageIndex = -1;
        private static int pendingPreviousStars;
        private static int pendingEarnedStars;
        private static int pendingUnlockedStageIndex = -1;
        private static bool pendingStageStarsImproved;
        private static bool pendingStageUnlockedNext;

        #endregion

        #region Properties

        public static StageDefinition SelectedStage { get; set; }
        public static int SelectedStageIndex { get; set; } = -1;
        public static bool HasPendingStageClearVisual => hasPendingStageClearVisual;
        public static int PendingClearedStageIndex => pendingClearedStageIndex;
        public static int PendingPreviousStars => pendingPreviousStars;
        public static int PendingEarnedStars => pendingEarnedStars;
        public static int PendingUnlockedStageIndex => pendingUnlockedStageIndex;
        public static bool PendingStageStarsImproved => pendingStageStarsImproved;
        public static bool PendingStageUnlockedNext => pendingStageUnlockedNext;

        #endregion

        #region Public Methods

        public static void ClearSelectedStage()
        {
            SelectedStage = null;
            SelectedStageIndex = -1;
        }

        public static void SetPendingStageClearVisual(int stageIndex, int previousStars, int earnedStars, bool starsImproved, bool unlockedNextStage)
        {
            hasPendingStageClearVisual = true;
            pendingClearedStageIndex = stageIndex;
            pendingPreviousStars = previousStars;
            pendingEarnedStars = earnedStars;
            pendingUnlockedStageIndex = stageIndex + 1;
            pendingStageStarsImproved = starsImproved;
            pendingStageUnlockedNext = unlockedNextStage;
        }

        public static void ClearPendingStageClearVisual()
        {
            hasPendingStageClearVisual = false;
            pendingClearedStageIndex = -1;
            pendingPreviousStars = 0;
            pendingEarnedStars = 0;
            pendingUnlockedStageIndex = -1;
            pendingStageStarsImproved = false;
            pendingStageUnlockedNext = false;
        }

        #endregion
    }
}
