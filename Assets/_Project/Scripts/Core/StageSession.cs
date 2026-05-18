namespace CrystalMind.MatchMancer
{
    public static class StageSession
    {
        #region Properties

        public static StageDefinition SelectedStage { get; set; }
        public static int SelectedStageIndex { get; set; } = -1;

        #endregion

        #region Public Methods

        public static void ClearSelectedStage()
        {
            SelectedStage = null;
            SelectedStageIndex = -1;
        }

        #endregion
    }
}
