using System.Collections.Generic;
using System.Linq;

namespace CrystalMind.MatchMancer
{
    public class MatchGroup
    {
        #region Variables

        // Cache
        private readonly HashSet<Tile> tiles = new HashSet<Tile>();

        // State
        private int longestHorizontalLength;
        private int longestVerticalLength;

        #endregion

        #region Properties

        public IReadOnlyCollection<Tile> Tiles => tiles;
        public int Count => tiles.Count;
        public int LongestHorizontalLength => longestHorizontalLength;
        public int LongestVerticalLength => longestVerticalLength;
        public bool HasHorizontalMatch => longestHorizontalLength >= 3;
        public bool HasVerticalMatch => longestVerticalLength >= 3;
        public bool IsCornerOrCrossShape => HasHorizontalMatch && HasVerticalMatch;

        #endregion

        #region Public Methods

        public void AddTiles(IEnumerable<Tile> newTiles)
        {
            AddTilesInternal(newTiles);
        }

        public void AddTiles(IEnumerable<Tile> newTiles, bool isHorizontalLine)
        {
            int lineLength = AddTilesInternal(newTiles);

            if (isHorizontalLine)
            {
                longestHorizontalLength = System.Math.Max(longestHorizontalLength, lineLength);
            }
            else
            {
                longestVerticalLength = System.Math.Max(longestVerticalLength, lineLength);
            }
        }

        public bool Overlaps(MatchGroup otherGroup)
        {
            if (otherGroup == null)
            {
                return false;
            }

            return tiles.Overlaps(otherGroup.tiles);
        }

        public void Merge(MatchGroup otherGroup)
        {
            if (otherGroup == null)
            {
                return;
            }

            tiles.UnionWith(otherGroup.tiles);
            longestHorizontalLength = System.Math.Max(longestHorizontalLength, otherGroup.longestHorizontalLength);
            longestVerticalLength = System.Math.Max(longestVerticalLength, otherGroup.longestVerticalLength);
        }

        #endregion

        #region Private Methods

        private int AddTilesInternal(IEnumerable<Tile> newTiles)
        {
            if (newTiles == null)
            {
                return 0;
            }

            int addedCount = 0;

            foreach (Tile tile in newTiles)
            {
                if (tile == null)
                {
                    continue;
                }

                tiles.Add(tile);
                addedCount++;
            }

            return addedCount;
        }

        #endregion
    }

    public class MatchFinder
    {
        #region Variables

        // Cache
        private readonly BoardManager board;
        private readonly MatchClassifier matchClassifier = new MatchClassifier();

        #endregion

        #region Properties

        #endregion

        #region Public Methods

        public MatchFinder(BoardManager boardManager)
        {
            board = boardManager;
        }

        public List<Tile> FindAllMatches()
        {
            HashSet<Tile> uniqueMatches = new HashSet<Tile>();
            List<MatchGroup> groups = FindMatchGroups();

            foreach (MatchGroup group in groups)
            {
                uniqueMatches.UnionWith(group.Tiles);
            }

            return uniqueMatches.ToList();
        }

        public List<MatchGroup> FindMatchGroups()
        {
            List<MatchGroup> groups = new List<MatchGroup>();

            groups.AddRange(FindHorizontalGroups());
            groups.AddRange(FindVerticalGroups());

            return MergeOverlappingGroups(groups);
        }

        public List<ClassifiedMatchGroup> FindClassifiedMatchGroups(bool includeSquare2x2Matches = true)
        {
            List<ClassifiedMatchGroup> classifiedGroups = new List<ClassifiedMatchGroup>();
            List<MatchGroup> groups = FindMatchGroups();

            if (includeSquare2x2Matches)
            {
                groups.AddRange(FindSquareGroups());
            }

            foreach (MatchGroup group in groups)
            {
                classifiedGroups.Add(new ClassifiedMatchGroup(group, matchClassifier.Classify(group)));
            }

            return classifiedGroups;
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Private Methods

        private List<MatchGroup> FindHorizontalGroups()
        {
            List<MatchGroup> groups = new List<MatchGroup>();

            for (int row = 0; row < board.Rows; row++)
            {
                int matchCount = 1;

                for (int col = 1; col < board.Cols; col++)
                {
                    Tile current = board.GetTile(row, col);
                    Tile previous = board.GetTile(row, col - 1);

                    if (IsSameType(current, previous))
                    {
                        matchCount++;
                    }
                    else
                    {
                        AddHorizontalGroup(groups, row, col - 1, matchCount);
                        matchCount = 1;
                    }
                }

                AddHorizontalGroup(groups, row, board.Cols - 1, matchCount);
            }

            return groups;
        }

        private List<MatchGroup> FindVerticalGroups()
        {
            List<MatchGroup> groups = new List<MatchGroup>();

            for (int col = 0; col < board.Cols; col++)
            {
                int matchCount = 1;

                for (int row = 1; row < board.Rows; row++)
                {
                    Tile current = board.GetTile(row, col);
                    Tile previous = board.GetTile(row - 1, col);

                    if (IsSameType(current, previous))
                    {
                        matchCount++;
                    }
                    else
                    {
                        AddVerticalGroup(groups, row - 1, col, matchCount);
                        matchCount = 1;
                    }
                }

                AddVerticalGroup(groups, board.Rows - 1, col, matchCount);
            }

            return groups;
        }

        private List<MatchGroup> FindSquareGroups()
        {
            List<MatchGroup> groups = new List<MatchGroup>();

            for (int row = 0; row < board.Rows - 1; row++)
            {
                for (int col = 0; col < board.Cols - 1; col++)
                {
                    AddSquareGroupIfValid(groups, row, col);
                }
            }

            return groups;
        }

        private void AddSquareGroupIfValid(List<MatchGroup> groups, int row, int col)
        {
            Tile topLeft = board.GetTile(row, col);
            Tile topRight = board.GetTile(row, col + 1);
            Tile bottomLeft = board.GetTile(row + 1, col);
            Tile bottomRight = board.GetTile(row + 1, col + 1);

            if (!IsSameType(topLeft, topRight) ||
                !IsSameType(topLeft, bottomLeft) ||
                !IsSameType(topLeft, bottomRight))
            {
                return;
            }

            MatchGroup group = new MatchGroup();
            group.AddTiles(new[] { topLeft, topRight, bottomLeft, bottomRight });
            groups.Add(group);
        }

        private void AddHorizontalGroup(List<MatchGroup> groups, int row, int endCol, int matchCount)
        {
            if (matchCount < 3)
            {
                return;
            }

            List<Tile> tiles = new List<Tile>();

            for (int i = 0; i < matchCount; i++)
            {
                Tile tile = board.GetTile(row, endCol - i);

                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }

            MatchGroup group = new MatchGroup();
            group.AddTiles(tiles, true);
            groups.Add(group);
        }

        private void AddVerticalGroup(List<MatchGroup> groups, int endRow, int col, int matchCount)
        {
            if (matchCount < 3)
            {
                return;
            }

            List<Tile> tiles = new List<Tile>();

            for (int i = 0; i < matchCount; i++)
            {
                Tile tile = board.GetTile(endRow - i, col);

                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }

            MatchGroup group = new MatchGroup();
            group.AddTiles(tiles, false);
            groups.Add(group);
        }

        private List<MatchGroup> MergeOverlappingGroups(List<MatchGroup> groups)
        {
            List<MatchGroup> mergedGroups = new List<MatchGroup>();

            foreach (MatchGroup group in groups)
            {
                MatchGroup targetGroup = mergedGroups.FirstOrDefault(existingGroup => existingGroup.Overlaps(group));

                if (targetGroup == null)
                {
                    mergedGroups.Add(group);
                    continue;
                }

                targetGroup.Merge(group);
                MergeRemainingOverlaps(mergedGroups, targetGroup);
            }

            return mergedGroups;
        }

        private void MergeRemainingOverlaps(List<MatchGroup> groups, MatchGroup targetGroup)
        {
            for (int index = groups.Count - 1; index >= 0; index--)
            {
                MatchGroup currentGroup = groups[index];

                if (currentGroup == targetGroup)
                {
                    continue;
                }

                if (!targetGroup.Overlaps(currentGroup))
                {
                    continue;
                }

                targetGroup.Merge(currentGroup);
                groups.RemoveAt(index);
            }
        }

        private bool IsSameType(Tile firstTile, Tile secondTile)
        {
            if (firstTile == null || secondTile == null)
            {
                return false;
            }

            return firstTile.Type == secondTile.Type;
        }

        #endregion
    }
}
