using System.Collections.Generic;
using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Pure-logic service: no MonoBehaviour, no state.
    /// Receives the current grid and returns MatchResult lists.
    /// </summary>
    public static class MatchDetector
    {
        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Scans the full grid for all valid horizontal and vertical matches (3+).
        /// Returns a list of MatchResult, each containing the matched pieces.
        /// </summary>
        public static List<MatchResult> FindAllMatches(BoardPiece[,] grid, int width, int height)
        {
            var results   = new List<MatchResult>();
            var processed = new HashSet<BoardPiece>();

            // Horizontal
            for (int row = 0; row < height; row++)
            {
                int col = 0;
                while (col < width)
                {
                    var piece = grid[col, row];
                    if (piece == null) { col++; continue; }

                    int run = 1;
                    while (col + run < width
                           && grid[col + run, row] != null
                           && grid[col + run, row].Element == piece.Element
                           && grid[col + run, row].PieceType != PieceType.MagicStone)
                        run++;

                    if (run >= 3)
                    {
                        var group = new List<BoardPiece>();
                        for (int k = 0; k < run; k++)
                            group.Add(grid[col + k, row]);
                        results.Add(new MatchResult(group, piece.Element));
                        foreach (var p in group) processed.Add(p);
                    }
                    col += run;
                }
            }

            // Vertical
            for (int col = 0; col < width; col++)
            {
                int row = 0;
                while (row < height)
                {
                    var piece = grid[col, row];
                    if (piece == null) { row++; continue; }

                    int run = 1;
                    while (row + run < height
                           && grid[col, row + run] != null
                           && grid[col, row + run].Element == piece.Element
                           && grid[col, row + run].PieceType != PieceType.MagicStone)
                        run++;

                    if (run >= 3)
                    {
                        var group = new List<BoardPiece>();
                        for (int k = 0; k < run; k++)
                            group.Add(grid[col, row + k]);

                        // Merge with an existing match if pieces overlap (L/T shapes)
                        bool merged = false;
                        foreach (var existing in results)
                        {
                            if (existing.Element != piece.Element) continue;
                            foreach (var gp in group)
                            {
                                if (existing.Pieces.Contains(gp))
                                {
                                    existing.Merge(group);
                                    merged = true;
                                    break;
                                }
                            }
                            if (merged) break;
                        }

                        if (!merged)
                            results.Add(new MatchResult(group, piece.Element));
                    }
                    row += run;
                }
            }

            return results;
        }

        /// <summary>
        /// Returns true if swapping (c1,r1) with (c2,r2) creates at least one match.
        /// Does NOT modify the grid.
        /// </summary>
        public static bool SwapCreatesMatch(BoardPiece[,] grid, int width, int height,
                                             int c1, int r1, int c2, int r2)
        {
            // Swap
            (grid[c1, r1], grid[c2, r2]) = (grid[c2, r2], grid[c1, r1]);
            if (grid[c1, r1] != null) { grid[c1, r1].Col = c1; grid[c1, r1].Row = r1; }
            if (grid[c2, r2] != null) { grid[c2, r2].Col = c2; grid[c2, r2].Row = r2; }

            bool hasMatch = FindAllMatches(grid, width, height).Count > 0;

            // Swap back
            (grid[c1, r1], grid[c2, r2]) = (grid[c2, r2], grid[c1, r1]);
            if (grid[c1, r1] != null) { grid[c1, r1].Col = c1; grid[c1, r1].Row = r1; }
            if (grid[c2, r2] != null) { grid[c2, r2].Col = c2; grid[c2, r2].Row = r2; }

            return hasMatch;
        }

        /// <summary>
        /// Checks if the board has at least one valid move remaining.
        /// Used to detect dead-board states (shuffle trigger).
        /// </summary>
        public static bool HasAnyValidMove(BoardPiece[,] grid, int width, int height)
        {
            for (int col = 0; col < width; col++)
            for (int row = 0; row < height; row++)
            {
                if (col + 1 < width  && SwapCreatesMatch(grid, width, height, col, row, col + 1, row)) return true;
                if (row + 1 < height && SwapCreatesMatch(grid, width, height, col, row, col, row + 1)) return true;
            }
            return false;
        }
    }

    // ── MatchResult ───────────────────────────────────────────────────────────

    public class MatchResult
    {
        public List<BoardPiece> Pieces  { get; }
        public ElementType      Element { get; }
        public int              Count   => Pieces.Count;

        public MatchResult(List<BoardPiece> pieces, ElementType element)
        {
            Pieces  = new List<BoardPiece>(pieces);
            Element = element;
        }

        public void Merge(List<BoardPiece> extra)
        {
            foreach (var p in extra)
                if (!Pieces.Contains(p)) Pieces.Add(p);
        }
    }
}
