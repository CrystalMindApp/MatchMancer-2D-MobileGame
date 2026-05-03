using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class MatchFinder
    {
        private BoardManager board;

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

                    if (current.Type == previous.Type)
                    {
                        matchCount++;
                    }
                    else
                    {
                        if (matchCount >= 3)
                        {
                            Debug.Log($"Horizontal match found at row {row}, count {matchCount}");

                            for (int i = 0; i < matchCount; i++)
                            {
                                matches.Add(board.GetTile(row, col - 1 - i));
                            }
                        }

                        matchCount = 1;
                    }
                }

                // check end row
                if (matchCount >= 3)
                {
                    for (int i = 0; i < matchCount; i++)
                    {
                        matches.Add(board.GetTile(row, board.Cols - 1 - i));
                    }
                }
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

                    if (current.Type == previous.Type)
                    {
                        matchCount++;
                    }
                    else
                    {
                        if (matchCount >= 3)
                        {
                            Debug.Log($"Vertical match found at col {col}, count {matchCount}");

                            for (int i = 0; i < matchCount; i++)
                            {
                                matches.Add(board.GetTile(row - 1 - i, col));
                            }
                        }

                        matchCount = 1;
                    }
                }

                // check end column
                if (matchCount >= 3)
                {
                    Debug.Log($"Vertical match found at col {col}, count {matchCount}");

                    for (int i = 0; i < matchCount; i++)
                    {
                        matches.Add(board.GetTile(board.Rows - 1 - i, col));
                    }
                }
            }

            return matches;
        }

        public MatchFinder(BoardManager board)
        {
            this.board = board;
        }

        public List<Tile> FindAllMatches()
        {
            HashSet<Tile> uniqueMatches = new HashSet<Tile>();

            foreach (Tile tile in FindHorizontalMatches())
            {
                uniqueMatches.Add(tile);
            }

            foreach (Tile tile in FindVerticalMatches())
            {
                uniqueMatches.Add(tile);
            }

            foreach (Tile tile in uniqueMatches)
            {
                Debug.Log($"Matched Tile: row {tile.Row}, col {tile.Col}, type {tile.Type}");
            }

            return uniqueMatches.ToList();
        }
    }
}