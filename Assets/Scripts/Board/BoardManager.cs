using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Match3Wizard
{
    /// <summary>
    /// Owns the 8x8 grid. Handles player input (swap), gravity, cascade resolution,
    /// and delegates match payouts to ManaSystem, CrystalSystem, and SpecialPieceHandler.
    /// </summary>
    public partial class BoardManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static BoardManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Piece Prefab")]
        [SerializeField] private BoardPiece piecePrefab;

        [Header("Element Sprites (index = ElementType int)")]
        [SerializeField] private Sprite[] normalSprites;     // 5 entries
        [SerializeField] private Sprite[] magicStoneSprites; // 5 entries

        [Header("Board Origin (bottom-left world position)")]
        [SerializeField] private Vector2 origin = Vector2.zero;
        [SerializeField] private float   cellSize = 1f;

        // ── Grid ──────────────────────────────────────────────────────────────
        private BoardPiece[,] _grid;
        private int W => GameManager.Instance.Config.boardWidth;
        private int H => GameManager.Instance.Config.boardHeight;

        // ── Input state ───────────────────────────────────────────────────────
        private BoardPiece _selected;
        private bool       _isProcessing;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<MatchResult, bool> OnMatchResolved; // (result, isCascade)
        public event Action                    OnBoardShuffled;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void InitBoard()
        {
            ClearBoard();
            _grid = new BoardPiece[W, H];

            for (int col = 0; col < W; col++)
            for (int row = 0; row < H; row++)
                SpawnPiece(col, row, GetSafeRandomElement(col, row));

            // Safety: ensure no pre-existing matches (re-roll offenders)
            RemoveStartingMatches();
        }

        /// <summary>Called by BoardPiece input handler or touch layer.</summary>
        public void OnPieceTapped(BoardPiece piece)
        {
            if (_isProcessing || !GameManager.Instance.IsTimerRunning) return;

            if (_selected == null)
            {
                _selected = piece;
                piece.SetHighlight(true);
                return;
            }

            if (_selected == piece)
            {
                _selected.SetHighlight(false);
                _selected = null;
                return;
            }

            if (AreAdjacent(_selected, piece))
            {
                _selected.SetHighlight(false);
                StartCoroutine(TrySwap(_selected, piece));
                _selected = null;
            }
            else
            {
                _selected.SetHighlight(false);
                _selected = piece;
                piece.SetHighlight(true);
            }
        }

        // ── Swap & Cascade ────────────────────────────────────────────────────

        private IEnumerator TrySwap(BoardPiece a, BoardPiece b)
        {
            _isProcessing = true;

            // Visual swap
            yield return AnimateSwap(a, b);
            ApplySwapInGrid(a, b);

            var matches = MatchDetector.FindAllMatches(_grid, W, H);

            if (matches.Count == 0)
            {
                // Revert
                yield return AnimateSwap(a, b);
                ApplySwapInGrid(a, b);
                _isProcessing = false;
                yield break;
            }

            yield return ResolveMatches(matches, isCascade: false);
            _isProcessing = false;
        }

        private IEnumerator ResolveMatches(List<MatchResult> matches, bool isCascade)
        {
            foreach (var match in matches)
            {
                OnMatchResolved?.Invoke(match, isCascade);

                // Payout mana
                ManaSystem.Instance?.OnMatchResolved(match, isCascade);

                // Spawn magic stones
                SpecialPieceHandler.Instance?.OnMatchResolved(match, _grid);

                // Remove pieces
                foreach (var piece in match.Pieces)
                {
                    if (piece == null || _grid[piece.Col, piece.Row] != piece) continue;
                    _grid[piece.Col, piece.Row] = null;
                    piece.PlayDestroyAnim(() => ReturnToPool(piece));
                }
            }

            yield return new WaitForSeconds(GameManager.Instance.Config.matchDelay);

            yield return ApplyGravity();

            yield return FillEmpty();

            yield return new WaitForSeconds(GameManager.Instance.Config.matchDelay);

            // Cascade
            var cascadeMatches = MatchDetector.FindAllMatches(_grid, W, H);
            if (cascadeMatches.Count > 0)
                yield return ResolveMatches(cascadeMatches, isCascade: true);

            // Dead-board check
            if (!MatchDetector.HasAnyValidMove(_grid, W, H))
            {
                ShuffleBoard();
                OnBoardShuffled?.Invoke();
            }
        }

        private IEnumerator ApplyGravity()
        {
            bool fell;
            do
            {
                fell = false;
                for (int col = 0; col < W; col++)
                for (int row = 0; row < H - 1; row++)
                {
                    if (_grid[col, row] != null || _grid[col, row + 1] == null) continue;
                    _grid[col, row]         = _grid[col, row + 1];
                    _grid[col, row + 1]     = null;
                    _grid[col, row].Row     = row;
                    AnimateDrop(_grid[col, row], col, row);
                    fell = true;
                }
                if (fell) yield return new WaitForSeconds(0.05f);
            } while (fell);
        }

        private IEnumerator FillEmpty()
        {
            for (int col = 0; col < W; col++)
            for (int row = 0; row < H; row++)
            {
                if (_grid[col, row] != null) continue;
                var piece = SpawnPiece(col, row, GameManager.Instance.Config.GetRandomElement());
                piece.transform.position = CellToWorld(col, H + 1);
                AnimateDrop(piece, col, row);
            }
            yield return new WaitForSeconds(0.15f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private BoardPiece SpawnPiece(int col, int row, ElementType element)
        {
            var piece = Instantiate(piecePrefab, CellToWorld(col, row), Quaternion.identity, transform);
            var ei    = (int)element;
            piece.NormalSprite     = normalSprites.Length     > ei ? normalSprites[ei]     : null;
            piece.MagicStoneSprite = magicStoneSprites.Length > ei ? magicStoneSprites[ei] : null;
            piece.Init(element, col, row);
            _grid[col, row] = piece;
            return piece;
        }

        private void ReturnToPool(BoardPiece piece) => Destroy(piece.gameObject);

        private void ClearBoard()
        {
            if (_grid == null) return;
            foreach (var p in _grid)
                if (p != null) Destroy(p.gameObject);
        }

        private void AnimateDrop(BoardPiece piece, int col, int row)
        {
            var target = CellToWorld(col, row);
            StartCoroutine(MovePieceTo(piece, target));
        }

        private IEnumerator MovePieceTo(BoardPiece piece, Vector3 target)
        {
            piece.IsMoving = true;
            float speed = GameManager.Instance.Config.dropSpeed;
            while (Vector3.Distance(piece.transform.position, target) > 0.01f)
            {
                piece.transform.position = Vector3.MoveTowards(
                    piece.transform.position, target, speed * Time.deltaTime);
                yield return null;
            }
            piece.transform.position = target;
            piece.IsMoving = false;
        }

        private IEnumerator AnimateSwap(BoardPiece a, BoardPiece b)
        {
            var posA = CellToWorld(a.Col, a.Row);
            var posB = CellToWorld(b.Col, b.Row);
            float t  = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.deltaTime * 10f, 1f);
                a.transform.position = Vector3.Lerp(posA, posB, t);
                b.transform.position = Vector3.Lerp(posB, posA, t);
                yield return null;
            }
        }

        private void ApplySwapInGrid(BoardPiece a, BoardPiece b)
        {
            (_grid[a.Col, a.Row], _grid[b.Col, b.Row]) = (_grid[b.Col, b.Row], _grid[a.Col, a.Row]);
            (a.Col, b.Col) = (b.Col, a.Col);
            (a.Row, b.Row) = (b.Row, a.Row);
        }

        private bool AreAdjacent(BoardPiece a, BoardPiece b) =>
            Mathf.Abs(a.Col - b.Col) + Mathf.Abs(a.Row - b.Row) == 1;

        private Vector3 CellToWorld(int col, int row) =>
            new Vector3(origin.x + col * cellSize, origin.y + row * cellSize, 0f);

        private ElementType GetSafeRandomElement(int col, int row)
        {
            // Avoid spawning an element that would already form a match
            var forbidden = new System.Collections.Generic.HashSet<ElementType>();
            if (col >= 2
                && _grid[col-1, row] != null && _grid[col-2, row] != null
                && _grid[col-1, row].Element == _grid[col-2, row].Element)
                forbidden.Add(_grid[col-1, row].Element);

            if (row >= 2
                && _grid[col, row-1] != null && _grid[col, row-2] != null
                && _grid[col, row-1].Element == _grid[col, row-2].Element)
                forbidden.Add(_grid[col, row-1].Element);

            ElementType el;
            int attempts = 0;
            do { el = GameManager.Instance.Config.GetRandomElement(); attempts++; }
            while (forbidden.Contains(el) && attempts < 20);
            return el;
        }

        private void RemoveStartingMatches()
        {
            bool changed;
            do
            {
                changed = false;
                var matches = MatchDetector.FindAllMatches(_grid, W, H);
                foreach (var m in matches)
                    foreach (var p in m.Pieces)
                    {
                        _grid[p.Col, p.Row] = null;
                        Destroy(p.gameObject);
                        SpawnPiece(p.Col, p.Row, GetSafeRandomElement(p.Col, p.Row));
                        changed = true;
                    }
            } while (changed);
        }

        private void ShuffleBoard()
        {
            var elements = new List<ElementType>();
            for (int col = 0; col < W; col++)
            for (int row = 0; row < H; row++)
                if (_grid[col, row] != null)
                    elements.Add(_grid[col, row].Element);

            // Fisher-Yates
            for (int i = elements.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (elements[i], elements[j]) = (elements[j], elements[i]);
            }

            int idx = 0;
            for (int col = 0; col < W; col++)
            for (int row = 0; row < H; row++)
            {
                if (_grid[col, row] == null) continue;
                Destroy(_grid[col, row].gameObject);
                SpawnPiece(col, row, elements[idx++]);
            }
        }
    }
}
