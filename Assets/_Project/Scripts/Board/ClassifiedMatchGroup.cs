namespace CrystalMind.MatchMancer
{
    public readonly struct ClassifiedMatchGroup
    {
        #region Properties

        public MatchGroup Group { get; }
        public MatchPatternType PatternType { get; }

        #endregion

        #region Public Methods

        public ClassifiedMatchGroup(MatchGroup group, MatchPatternType patternType)
        {
            Group = group;
            PatternType = patternType;
        }

        public string GetDebugSummary()
        {
            int count = Group != null ? Group.Count : 0;
            int horizontalLength = Group != null ? Group.LongestHorizontalLength : 0;
            int verticalLength = Group != null ? Group.LongestVerticalLength : 0;

            return $"{PatternType} | Count: {count} | H: {horizontalLength} | V: {verticalLength}";
        }

        #endregion
    }
}
