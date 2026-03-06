// ── Partial extension for BoardManager ───────────────────────────────────────
// Add this method inside BoardManager.cs OR keep as separate partial class.
// Included separately to avoid the BoardManager file growing too large.

using System.Collections.Generic;

namespace Match3Wizard
{
    public partial class BoardManager
    {
        /// <summary>
        /// Called by SpecialPieceHandler after a spell destroys pieces externally.
        /// Clears those cells in the grid and triggers gravity + refill.
        /// </summary>
        public void ClearPiecesExternal(IEnumerable<BoardPiece> pieces)
        {
            foreach (var p in pieces)
            {
                if (_grid == null) return;
                if (p == null) continue;
                if (_grid[p.Col, p.Row] == p)
                    _grid[p.Col, p.Row] = null;
            }
            StartCoroutine(PostExternalClear());
        }

        private System.Collections.IEnumerator PostExternalClear()
        {
            yield return ApplyGravity();
            yield return FillEmpty();

            var cascades = MatchDetector.FindAllMatches(_grid, W, H);
            if (cascades.Count > 0)
                yield return ResolveMatches(cascades, isCascade: true);
        }
    }
}
