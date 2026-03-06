using System;
using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Owns spell level data and orchestrates spell casting.
    /// Spell levels are persisted via SaveSystem.
    /// </summary>
    public class SpellSystem : MonoBehaviour
    {
        public static SpellSystem Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<ElementType, int> OnSpellLevelChanged;  // (element, newLevel)
        public event Action<ElementType>      OnSpellCast;

        private GameConfig Config => GameManager.Instance.Config;

        // Spell level bonus table: index = level (1-5), value = mana cost reduction
        private static readonly int[] CostReductions = { 0, 0, 2, 4, 6, 8 };

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public int GetLevel(ElementType e)           => SaveSystem.GetSpellLevel(e);
        public int GetMaxLevel()                      => 5;

        public int GetCurrentCost(ElementType e)
        {
            int level      = GetLevel(e);
            int baseCost   = Config.GetSpellBaseCost(e);
            int reduction  = CostReductions[Mathf.Clamp(level, 1, 5)];
            return Mathf.Max(1, baseCost - reduction);
        }

        public bool CanCastSpell(ElementType e)    => ManaSystem.Instance?.CanActivateSpell(e) ?? false;

        /// <summary>Attempt to cast spell for element. Returns true if cast succeeded.</summary>
        public bool TryCastSpell(ElementType e)
        {
            if (!CanCastSpell(e)) return false;
            if (!ManaSystem.Instance.TryConsumeMana(e)) return false;

            ExecuteSpell(e);
            OnSpellCast?.Invoke(e);
            AchievementSystem.Instance?.OnSpellCast(e);
            AudioManager.Instance?.PlaySpellSFX(e);
            return true;
        }

        /// <summary>
        /// Try to upgrade spell for element using crystals.
        /// Returns true if upgrade was successful.
        /// </summary>
        public bool TryUpgradeSpell(ElementType e)
        {
            int currentLevel = GetLevel(e);
            if (currentLevel >= 5)
            {
                Debug.Log($"[SpellSystem] {e} already max level.");
                return false;
            }

            int targetLevel = currentLevel + 1;
            int cost        = Config.spellUpgradeCosts[targetLevel];

            if (SaveSystem.GetCrystals(e) < cost)
            {
                Debug.Log($"[SpellSystem] Not enough {e} crystals. Need {cost}.");
                return false;
            }

            // Check spirit prerequisite
            if (!MeetsLevelRequirement(e, targetLevel))
            {
                Debug.Log($"[SpellSystem] Spirits prerequisite not met for level {targetLevel}.");
                return false;
            }

            SaveSystem.AddCrystals(e, -cost);
            SaveSystem.SetSpellLevel(e, targetLevel);
            SaveSystem.Save();

            OnSpellLevelChanged?.Invoke(e, targetLevel);
            AchievementSystem.Instance?.OnSpellUpgraded();
            return true;
        }

        // ── Spirit prerequisites per level ───────────────────────────────────

        private bool MeetsLevelRequirement(ElementType e, int targetLevel)
        {
            var save = SaveSystem.Current;
            return targetLevel switch
            {
                2 => save.spiritsFreed[(int)e],                                           // 1 spirit (own)
                3 => CountFreedSpirits() >= 2,
                4 => CountFreedSpirits() >= 4,
                5 => CountFreedSpirits() >= 5,  // all 5 elementals
                _ => true
            };
        }

        private int CountFreedSpirits()
        {
            int n = 0;
            foreach (var freed in SaveSystem.Current.spiritsFreed)
                if (freed) n++;
            return n;
        }

        // ── Spell execution ───────────────────────────────────────────────────

        private void ExecuteSpell(ElementType e)
        {
            switch (e)
            {
                case ElementType.Light:
                    ExecuteAurora();
                    break;
                default:
                    // Regular spells: prompt player to choose target (HUD handles targeting)
                    UIManager.Instance?.BeginSpellTargeting(e, OnTargetChosen);
                    break;
            }
        }

        private void OnTargetChosen(ElementType e, int col, int row)
        {
            switch (e)
            {
                case ElementType.Fire:  SpellFire(col, row);  break;
                case ElementType.Water: SpellWater(row);       break;
                case ElementType.Air:   SpellAir(col);         break;
                case ElementType.Earth: SpellEarth();          break;
            }
        }

        private void SpellFire(int centerCol, int centerRow)
        {
            int level  = GetLevel(ElementType.Fire);
            int radius = GetAoeRadius(level); // 1 = 3x3, can expand with level

            var config = GameManager.Instance.Config;
            int W = config.boardWidth, H = config.boardHeight;
            var grid = BoardManager.Instance;

            var affected = new System.Collections.Generic.List<BoardPiece>();
            var gridField = GetGrid();
            if (gridField == null) return;

            for (int dc = -radius; dc <= radius; dc++)
            for (int dr = -radius; dr <= radius; dr++)
            {
                int c = centerCol + dc, r = centerRow + dr;
                if (c >= 0 && c < W && r >= 0 && r < H && gridField[c, r] != null)
                    affected.Add(gridField[c, r]);
            }

            DestroyPieces(affected);
        }

        private void SpellWater(int row)
        {
            int W        = GameManager.Instance.Config.boardWidth;
            var gridField = GetGrid();
            if (gridField == null) return;

            var affected = new System.Collections.Generic.List<BoardPiece>();
            for (int c = 0; c < W; c++)
                if (gridField[c, row] != null) affected.Add(gridField[c, row]);

            DestroyPieces(affected);
        }

        private void SpellAir(int col)
        {
            int H        = GameManager.Instance.Config.boardHeight;
            var gridField = GetGrid();
            if (gridField == null) return;

            var affected = new System.Collections.Generic.List<BoardPiece>();
            for (int r = 0; r < H; r++)
                if (gridField[col, r] != null) affected.Add(gridField[col, r]);

            DestroyPieces(affected);
        }

        private void SpellEarth()
        {
            int level    = GetLevel(ElementType.Earth);
            int count    = 8 + (level - 1); // more pieces at higher levels
            var gridField = GetGrid();
            if (gridField == null) return;

            int W = GameManager.Instance.Config.boardWidth;
            int H = GameManager.Instance.Config.boardHeight;
            var all = new System.Collections.Generic.List<BoardPiece>();
            for (int c = 0; c < W; c++)
            for (int r = 0; r < H; r++)
                if (gridField[c, r] != null) all.Add(gridField[c, r]);

            for (int i = 0; i < Mathf.Min(count, all.Count); i++)
            {
                int j = UnityEngine.Random.Range(i, all.Count);
                (all[i], all[j]) = (all[j], all[i]);
            }
            DestroyPieces(all.GetRange(0, Mathf.Min(count, all.Count)));
        }

        private void ExecuteAurora()
        {
            // Destroy ALL light pieces + bonus mana
            var gridField = GetGrid();
            if (gridField == null) return;

            int W = GameManager.Instance.Config.boardWidth;
            int H = GameManager.Instance.Config.boardHeight;
            var affected = new System.Collections.Generic.List<BoardPiece>();

            for (int c = 0; c < W; c++)
            for (int r = 0; r < H; r++)
                if (gridField[c, r] != null && gridField[c, r].Element == ElementType.Light)
                    affected.Add(gridField[c, r]);

            DestroyPieces(affected);
            ManaSystem.Instance?.AddBonusMana(affected.Count); // bonus mana per light destroyed
        }

        private void DestroyPieces(System.Collections.Generic.List<BoardPiece> pieces)
        {
            CrystalSystem.Instance?.OnPiecesDestroyed(new System.Collections.Generic.HashSet<BoardPiece>(pieces), isCascade: false);
            foreach (var p in pieces) p.PlayDestroyAnim(null);
            BoardManager.Instance?.ClearPiecesExternal(pieces);
        }

        private int GetAoeRadius(int level) => level switch
        {
            1 => 1, 2 => 1, 3 => 1, 4 => 2, 5 => 2, _ => 1
        };

        // Reflection helper (same pattern as SpecialPieceHandler)
        private BoardPiece[,] GetGrid() =>
            (BoardPiece[,])typeof(BoardManager)
                .GetField("_grid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(BoardManager.Instance);
    }
}
