namespace CrystalMind.MatchMancer
{
    public class MatchClassifier
    {
        #region Public Methods

        public MatchPatternType Classify(MatchGroup group)
        {
            if (group == null || group.Count < 3)
            {
                return MatchPatternType.None;
            }

            if (group.HasHorizontalMatch && group.HasVerticalMatch)
            {
                return MatchPatternType.Cross;
            }

            if (group.Count >= 5)
            {
                return MatchPatternType.Straight5Plus;
            }

            if (group.Count == 4)
            {
                return MatchPatternType.Straight4;
            }

            return MatchPatternType.Straight3;
        }

        #endregion
    }
}
