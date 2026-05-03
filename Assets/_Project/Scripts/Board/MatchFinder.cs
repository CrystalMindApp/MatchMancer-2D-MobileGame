using System.Collections.Generic;
using System.Linq;

namespace CrystalMind.MatchMancer
{
    public class MatchFinder
    {
        #region Variables

        // Cache
        private readonly BoardManager board;

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

            AddMatches(uniqueMatches, FindHorizontalMatches());
            AddMatches(uniqueMatches, FindVerticalMatches());

            return uniqueMatches.ToList();
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Private Methods

        private void AddMatches(HashSet<Tile> target, List<Tile> matches)
        {
            if (matches == null)
            {
                return;
            }

            foreach (Tile tile in matches)
            {
                if (tile != null)
                {
                    target.Add(tile);
                }
            }
        }

        private List<Tile> FindHorizontalMatches()
        {
            List<Tile> matches = new List<Tile>();

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
                        AddHorizontalMatch(matches, row, col - 1, matchCount);
                        matchCount = 1;
                    }
                }

                AddHorizontalMatch(matches, row, board.Cols - 1, matchCount);
            }

            return matches;
        }

        private List<Tile> FindVerticalMatches()
        {
            List<Tile> matches = new List<Tile>();

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
                        AddVerticalMatch(matches, row - 1, col, matchCount);
                        matchCount = 1;
                    }
                }

                AddVerticalMatch(matches, board.Rows - 1, col, matchCount);
            }

            return matches;
        }

        private void AddHorizontalMatch(List<Tile> matches, int row, int endCol, int matchCount)
        {
            if (matchCount < 3)
            {
                return;
            }

            for (int i = 0; i < matchCount; i++)
            {
                Tile tile = board.GetTile(row, endCol - i);

                if (tile != null)
                {
                    matches.Add(tile);
                }
            }
        }

        private void AddVerticalMatch(List<Tile> matches, int endRow, int col, int matchCount)
        {
            if (matchCount < 3)
            {
                return;
            }

            for (int i = 0; i < matchCount; i++)
            {
                Tile tile = board.GetTile(endRow - i, col);

                if (tile != null)
                {
                    matches.Add(tile);
                }
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
