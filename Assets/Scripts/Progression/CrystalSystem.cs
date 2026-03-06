using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Tracks crystal earnings during a session.
    /// Only spells and magic-stone activations generate crystals (not normal matches).
    /// </summary>
    public class CrystalSystem : MonoBehaviour
    {
        public static CrystalSystem Instance { get; private set; }

        private readonly float[] _sessionCrystals = new float[5];

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<ElementType, int> OnSessionCrystalsChanged; // (element, newTotal)

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void ResetSession()
        {
            for (int i = 0; i < 5; i++) _sessionCrystals[i] = 0f;
        }

        public void FlushSessionToSave()
        {
            for (int i = 0; i < 5; i++)
            {
                int earned = Mathf.FloorToInt(_sessionCrystals[i]);
                if (earned > 0) SaveSystem.AddCrystals((ElementType)i, earned);
            }
        }

        public int GetSessionCrystals(ElementType e) =>
            Mathf.FloorToInt(_sessionCrystals[(int)e]);

        public int GetTotalCrystals(ElementType e) =>
            SaveSystem.GetCrystals(e) + GetSessionCrystals(e);

        /// <summary>
        /// Called by BoardManager / SpecialPieceHandler after pieces are destroyed by spells.
        /// isCascade = true means 0.5 crystals per piece instead of 1.
        /// </summary>
        public void OnPiecesDestroyed(HashSet<BoardPiece> pieces, bool isCascade)
        {
            var config   = GameManager.Instance.Config;
            float perPiece = isCascade
                ? config.crystalsPerPieceCascade
                : config.crystalsPerPieceSpell;

            foreach (var piece in pieces)
            {
                if (piece == null) continue;
                int idx = (int)piece.Element;
                _sessionCrystals[idx] += perPiece;
                OnSessionCrystalsChanged?.Invoke(piece.Element, GetSessionCrystals(piece.Element));
            }
        }

        /// <summary>Spend crystals (persistent pool) for gallery reveal or spell upgrade.</summary>
        public bool TrySpendCrystals(ElementType e, int amount)
        {
            int total = GetTotalCrystals(e);
            if (total < amount) return false;

            // Drain from saved first, then session
            int saved = SaveSystem.GetCrystals(e);
            if (saved >= amount)
            {
                SaveSystem.AddCrystals(e, -amount);
            }
            else
            {
                int remaining = amount - saved;
                SaveSystem.AddCrystals(e, -saved);
                _sessionCrystals[(int)e] -= remaining;
            }
            return true;
        }
    }
}
