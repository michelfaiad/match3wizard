using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Manages Magic Stone creation (match 4+) and activation (player presses element button).
    /// Each element button fires ALL stones of that element simultaneously.
    /// </summary>
    public class SpecialPieceHandler : MonoBehaviour
    {
        public static SpecialPieceHandler Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Match 4+ creates a Magic Stone ────────────────────────────────────

        public void OnMatchResolved(MatchResult match, BoardPiece[,] grid)
        {
            var config    = GameManager.Instance.Config;
            int stoneCount = config.GetStonesForMatch(match.Count);
            if (stoneCount == 0) return;

            // Place stone(s) at last piece(s) in match
            for (int s = 0; s < stoneCount && s < match.Pieces.Count; s++)
            {
                var target = match.Pieces[match.Pieces.Count - 1 - s];
                if (grid[target.Col, target.Row] == null) continue;
                grid[target.Col, target.Row].PromoteToMagicStone();
            }
        }

        // ── Player activates all stones of one element ─────────────────────────

        public void ActivateElement(ElementType element)
        {
            if (BoardManager.Instance == null) return;

            var stones = FindStonesOfElement(element);
            if (stones.Count == 0) return;

            StartCoroutine(ActivateStones(stones, element));
        }

        private IEnumerator ActivateStones(List<BoardPiece> stones, ElementType element)
        {
            var destroyed = new HashSet<BoardPiece>();

            foreach (var stone in stones)
            {
                var affected = GetAffectedPieces(stone, element);
                foreach (var piece in affected)
                    destroyed.Add(piece);

                // Also destroy the stone itself
                destroyed.Add(stone);
            }

            // Notify crystal system (spell-type destruction)
            CrystalSystem.Instance?.OnPiecesDestroyed(destroyed, isCascade: false);

            // Remove from board
            foreach (var piece in destroyed)
                piece.PlayDestroyAnim(null);

            // Let board know cells are cleared (reflect in grid)
            BoardManager.Instance.ClearPiecesExternal(destroyed);

            yield return new WaitForSeconds(0.3f);
        }

        // ── Spell effect per element ──────────────────────────────────────────

        private List<BoardPiece> GetAffectedPieces(BoardPiece stone, ElementType element)
        {
            return element switch
            {
                ElementType.Fire  => GetSquare3x3(stone),
                ElementType.Water => GetRow(stone),
                ElementType.Air   => GetColumn(stone),
                ElementType.Earth => GetRandom8(),
                ElementType.Light => GetAllLight(),
                _                 => new List<BoardPiece>()
            };
        }

        private List<BoardPiece> GetSquare3x3(BoardPiece center)
        {
            var result = new List<BoardPiece>();
            var grid   = GetGrid();
            if (grid == null) return result;

            int W = GameManager.Instance.Config.boardWidth;
            int H = GameManager.Instance.Config.boardHeight;

            for (int dc = -1; dc <= 1; dc++)
            for (int dr = -1; dr <= 1; dr++)
            {
                int c = center.Col + dc, r = center.Row + dr;
                if (c >= 0 && c < W && r >= 0 && r < H && grid[c, r] != null)
                    result.Add(grid[c, r]);
            }
            return result;
        }

        private List<BoardPiece> GetRow(BoardPiece piece)
        {
            var result = new List<BoardPiece>();
            var grid   = GetGrid();
            if (grid == null) return result;

            int W = GameManager.Instance.Config.boardWidth;
            for (int c = 0; c < W; c++)
                if (grid[c, piece.Row] != null) result.Add(grid[c, piece.Row]);
            return result;
        }

        private List<BoardPiece> GetColumn(BoardPiece piece)
        {
            var result = new List<BoardPiece>();
            var grid   = GetGrid();
            if (grid == null) return result;

            int H = GameManager.Instance.Config.boardHeight;
            for (int r = 0; r < H; r++)
                if (grid[piece.Col, r] != null) result.Add(grid[piece.Col, r]);
            return result;
        }

        private List<BoardPiece> GetRandom8()
        {
            var result = new List<BoardPiece>();
            var grid   = GetGrid();
            if (grid == null) return result;

            int W = GameManager.Instance.Config.boardWidth;
            int H = GameManager.Instance.Config.boardHeight;

            var all = new List<BoardPiece>();
            for (int c = 0; c < W; c++)
            for (int r = 0; r < H; r++)
                if (grid[c, r] != null) all.Add(grid[c, r]);

            // Fisher-Yates partial
            for (int i = 0; i < Mathf.Min(8, all.Count); i++)
            {
                int j = Random.Range(i, all.Count);
                (all[i], all[j]) = (all[j], all[i]);
                result.Add(all[i]);
            }
            return result;
        }

        private List<BoardPiece> GetAllLight()
        {
            var result = new List<BoardPiece>();
            var grid   = GetGrid();
            if (grid == null) return result;

            int W = GameManager.Instance.Config.boardWidth;
            int H = GameManager.Instance.Config.boardHeight;

            for (int c = 0; c < W; c++)
            for (int r = 0; r < H; r++)
                if (grid[c, r] != null && grid[c, r].Element == ElementType.Light)
                    result.Add(grid[c, r]);
            return result;
        }

        private List<BoardPiece> FindStonesOfElement(ElementType element)
        {
            var result = new List<BoardPiece>();
            var grid   = GetGrid();
            if (grid == null) return result;

            int W = GameManager.Instance.Config.boardWidth;
            int H = GameManager.Instance.Config.boardHeight;

            for (int c = 0; c < W; c++)
            for (int r = 0; r < H; r++)
                if (grid[c, r] != null
                    && grid[c, r].Element   == element
                    && grid[c, r].PieceType == PieceType.MagicStone)
                    result.Add(grid[c, r]);

            return result;
        }

        private BoardPiece[,] GetGrid() =>
            (BoardPiece[,])typeof(BoardManager)
                .GetField("_grid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(BoardManager.Instance);
    }
}
