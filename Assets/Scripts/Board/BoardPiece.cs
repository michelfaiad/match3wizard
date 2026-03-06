using UnityEngine;

namespace Match3Wizard
{
    public enum PieceType { Normal, MagicStone }

    /// <summary>
    /// Represents one cell on the 8x8 grid.
    /// Attach to the piece prefab; BoardManager pools and repositions instances.
    /// </summary>
    public class BoardPiece : MonoBehaviour
    {
        // ── Data ──────────────────────────────────────────────────────────────
        public ElementType Element   { get; private set; }
        public PieceType   PieceType { get; private set; }
        public int         Col       { get; set; }
        public int         Row       { get; set; }

        public bool IsMoving { get; set; }
        public bool IsSelected { get; set; }

        // ── Visuals (assign in prefab) ────────────────────────────────────────
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator       animator;

        // Sprites assigned by BoardManager based on element
        public Sprite NormalSprite    { get; set; }
        public Sprite MagicStoneSprite { get; set; }

        // ── Init ──────────────────────────────────────────────────────────────
        public void Init(ElementType element, int col, int row, bool asMagicStone = false)
        {
            Element   = element;
            Col       = col;
            Row       = row;
            PieceType = asMagicStone ? PieceType.MagicStone : PieceType.Normal;
            RefreshVisual();
        }

        public void PromoteToMagicStone()
        {
            PieceType = PieceType.MagicStone;
            RefreshVisual();
        }

        // ── Visual ────────────────────────────────────────────────────────────
        private void RefreshVisual()
        {
            if (spriteRenderer == null) return;
            spriteRenderer.sprite = PieceType == PieceType.MagicStone
                ? MagicStoneSprite
                : NormalSprite;
        }

        public void PlayDestroyAnim(System.Action onComplete)
        {
            if (animator != null)
            {
                animator.SetTrigger("Destroy");
                StartCoroutine(WaitForAnim("Destroy", onComplete));
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private System.Collections.IEnumerator WaitForAnim(string state, System.Action cb)
        {
            yield return new WaitForSeconds(0.25f);
            cb?.Invoke();
        }

        public void SetHighlight(bool on)
        {
            if (spriteRenderer == null) return;
            spriteRenderer.color = on ? new Color(1f, 1f, 0.5f) : Color.white;
        }
    }
}
