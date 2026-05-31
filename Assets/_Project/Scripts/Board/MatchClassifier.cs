using System.Collections.Generic;
using System.Linq;

namespace CrystalMind.MatchMancer
{
    public class MatchClassifier
    {
        #region Variables

        private enum SegmentIntersectionPosition
        {
            None,
            End,
            Middle
        }

        private readonly struct MatchSegment
        {
            public MatchSegment(bool isHorizontal, int fixedCoordinate, int start, int end)
            {
                IsHorizontal = isHorizontal;
                FixedCoordinate = fixedCoordinate;
                Start = start;
                End = end;
            }

            public bool IsHorizontal { get; }
            public int FixedCoordinate { get; }
            public int Start { get; }
            public int End { get; }
            public int Length => End - Start + 1;
        }

        #endregion

        #region Public Methods

        public MatchPatternType Classify(MatchGroup group)
        {
            if (group == null || group.Count < 3)
            {
                return MatchPatternType.None;
            }

            if (IsSquare2x2(group))
            {
                return MatchPatternType.Square2x2;
            }

            List<MatchSegment> horizontalSegments = GetSegments(group, true);
            List<MatchSegment> verticalSegments = GetSegments(group, false);

            MatchPatternType shapePattern = ClassifyIntersectingSegments(horizontalSegments, verticalSegments);

            if (shapePattern != MatchPatternType.None)
            {
                return shapePattern;
            }

            int longestLength = GetLongestSegmentLength(horizontalSegments, verticalSegments);

            if (longestLength >= 5)
            {
                return MatchPatternType.Straight5Plus;
            }

            if (longestLength == 4)
            {
                return MatchPatternType.Straight4;
            }

            if (longestLength == 3)
            {
                return MatchPatternType.Straight3;
            }

            return MatchPatternType.None;
        }

        #endregion

        #region Private Methods

        private bool IsSquare2x2(MatchGroup group)
        {
            if (group.Count != 4)
            {
                return false;
            }

            List<Tile> tiles = group.Tiles.Where(tile => tile != null).ToList();

            if (tiles.Count != 4)
            {
                return false;
            }

            List<int> rows = tiles.Select(tile => tile.Row).Distinct().OrderBy(row => row).ToList();
            List<int> cols = tiles.Select(tile => tile.Col).Distinct().OrderBy(col => col).ToList();

            if (rows.Count != 2 || cols.Count != 2)
            {
                return false;
            }

            if (rows[1] - rows[0] != 1 || cols[1] - cols[0] != 1)
            {
                return false;
            }

            TileType tileType = tiles[0].Type;

            return tiles.All(tile => tile.Type == tileType) &&
                HasTileAt(tiles, rows[0], cols[0]) &&
                HasTileAt(tiles, rows[0], cols[1]) &&
                HasTileAt(tiles, rows[1], cols[0]) &&
                HasTileAt(tiles, rows[1], cols[1]);
        }

        private bool HasTileAt(List<Tile> tiles, int row, int col)
        {
            return tiles.Any(tile => tile != null && tile.Row == row && tile.Col == col);
        }

        private List<MatchSegment> GetSegments(MatchGroup group, bool horizontal)
        {
            List<MatchSegment> segments = new List<MatchSegment>();
            IEnumerable<IGrouping<int, Tile>> groupedTiles = horizontal
                ? group.Tiles.Where(tile => tile != null).GroupBy(tile => tile.Row)
                : group.Tiles.Where(tile => tile != null).GroupBy(tile => tile.Col);

            foreach (IGrouping<int, Tile> tileGroup in groupedTiles)
            {
                List<int> movingCoordinates = horizontal
                    ? tileGroup.Select(tile => tile.Col).Distinct().OrderBy(col => col).ToList()
                    : tileGroup.Select(tile => tile.Row).Distinct().OrderBy(row => row).ToList();

                AddContiguousSegments(segments, horizontal, tileGroup.Key, movingCoordinates);
            }

            return segments;
        }

        private void AddContiguousSegments(List<MatchSegment> segments, bool horizontal, int fixedCoordinate, List<int> movingCoordinates)
        {
            if (movingCoordinates == null || movingCoordinates.Count == 0)
            {
                return;
            }

            int start = movingCoordinates[0];
            int previous = movingCoordinates[0];

            for (int index = 1; index < movingCoordinates.Count; index++)
            {
                int current = movingCoordinates[index];

                if (current == previous + 1)
                {
                    previous = current;
                    continue;
                }

                AddSegmentIfMatchLength(segments, horizontal, fixedCoordinate, start, previous);
                start = current;
                previous = current;
            }

            AddSegmentIfMatchLength(segments, horizontal, fixedCoordinate, start, previous);
        }

        private void AddSegmentIfMatchLength(List<MatchSegment> segments, bool horizontal, int fixedCoordinate, int start, int end)
        {
            if (end - start + 1 < 3)
            {
                return;
            }

            segments.Add(new MatchSegment(horizontal, fixedCoordinate, start, end));
        }

        private MatchPatternType ClassifyIntersectingSegments(List<MatchSegment> horizontalSegments, List<MatchSegment> verticalSegments)
        {
            MatchPatternType strongestPattern = MatchPatternType.None;

            foreach (MatchSegment horizontalSegment in horizontalSegments)
            {
                foreach (MatchSegment verticalSegment in verticalSegments)
                {
                    MatchPatternType pattern = ClassifyIntersection(horizontalSegment, verticalSegment);
                    strongestPattern = GetStrongerPattern(strongestPattern, pattern);

                    if (strongestPattern == MatchPatternType.Cross)
                    {
                        return strongestPattern;
                    }
                }
            }

            return strongestPattern;
        }

        private MatchPatternType ClassifyIntersection(MatchSegment horizontalSegment, MatchSegment verticalSegment)
        {
            int row = horizontalSegment.FixedCoordinate;
            int col = verticalSegment.FixedCoordinate;

            if (col < horizontalSegment.Start || col > horizontalSegment.End ||
                row < verticalSegment.Start || row > verticalSegment.End)
            {
                return MatchPatternType.None;
            }

            SegmentIntersectionPosition horizontalPosition = GetIntersectionPosition(horizontalSegment, col);
            SegmentIntersectionPosition verticalPosition = GetIntersectionPosition(verticalSegment, row);

            if (horizontalPosition == SegmentIntersectionPosition.Middle &&
                verticalPosition == SegmentIntersectionPosition.Middle)
            {
                return MatchPatternType.Cross;
            }

            if ((horizontalPosition == SegmentIntersectionPosition.Middle && verticalPosition == SegmentIntersectionPosition.End) ||
                (horizontalPosition == SegmentIntersectionPosition.End && verticalPosition == SegmentIntersectionPosition.Middle))
            {
                return MatchPatternType.TShape;
            }

            if (horizontalPosition == SegmentIntersectionPosition.End &&
                verticalPosition == SegmentIntersectionPosition.End)
            {
                return MatchPatternType.LShape;
            }

            return MatchPatternType.None;
        }

        private SegmentIntersectionPosition GetIntersectionPosition(MatchSegment segment, int coordinate)
        {
            if (coordinate == segment.Start || coordinate == segment.End)
            {
                return SegmentIntersectionPosition.End;
            }

            if (coordinate > segment.Start && coordinate < segment.End)
            {
                return SegmentIntersectionPosition.Middle;
            }

            return SegmentIntersectionPosition.None;
        }

        private MatchPatternType GetStrongerPattern(MatchPatternType currentPattern, MatchPatternType candidatePattern)
        {
            return GetPatternPriority(candidatePattern) > GetPatternPriority(currentPattern)
                ? candidatePattern
                : currentPattern;
        }

        private int GetPatternPriority(MatchPatternType patternType)
        {
            switch (patternType)
            {
                case MatchPatternType.Cross:
                    return 4;

                case MatchPatternType.TShape:
                    return 3;

                case MatchPatternType.LShape:
                    return 2;

                default:
                    return 0;
            }
        }

        private int GetLongestSegmentLength(List<MatchSegment> horizontalSegments, List<MatchSegment> verticalSegments)
        {
            int longestHorizontal = horizontalSegments.Count > 0 ? horizontalSegments.Max(segment => segment.Length) : 0;
            int longestVertical = verticalSegments.Count > 0 ? verticalSegments.Max(segment => segment.Length) : 0;

            return System.Math.Max(longestHorizontal, longestVertical);
        }

        #endregion
    }
}
